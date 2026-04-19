#!/usr/bin/env bash
# ══════════════════════════════════════════════════════════════════
#  ci-local.sh — Local Docker CI pipeline for InteractiveMap
#
#  Works on macOS (Intel & Apple Silicon) and Linux.
#
#  Phases
#  ──────
#  1. unit      Build every service to the `test` Dockerfile stage.
#               dotnet test runs during the Docker build itself — no
#               volume mounts, no "read-only file system" errors.
#
#  2. swagger   Start the full stack (postgres + 3 services) in
#               Development mode, wait for each service's
#               /swagger/v1/swagger.json to return HTTP 200, print
#               clickable Swagger URLs, then tear everything down.
#
#  Usage
#  ─────
#  ./ci-local.sh                  # unit tests only
#  ./ci-local.sh --swagger        # unit tests + swagger integration
#  ./ci-local.sh --swagger-only   # swagger phase only (skip unit)
#  ./ci-local.sh --service locationservice   # one service unit test
#  ./ci-local.sh --build-only     # build images only, skip test run
#  ./ci-local.sh --clean          # remove all CI images & volumes
#  ./ci-local.sh --help
# ══════════════════════════════════════════════════════════════════
set -euo pipefail

# ── Resolve script directory (works on macOS + Linux) ─────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CI_COMPOSE="$SCRIPT_DIR/docker-compose.ci.yml"
SWAGGER_COMPOSE="$SCRIPT_DIR/docker-compose.swagger-test.yml"

ALL_SERVICES=("userservice" "locationservice" "reviewservice")

# Default port values (overridden if .env exists)
USERSERVICE_HTTP_PORT=5280
LOCATIONSERVICE_HTTP_PORT=5282
REVIEWSERVICE_HTTP_PORT=5284

# ── Detect `docker compose` v2 or fall back to `docker-compose` v1 ─
if docker compose version &>/dev/null 2>&1; then
    DC="docker compose"
elif command -v docker-compose &>/dev/null; then
    DC="docker-compose"
else
    echo "ERROR: Neither 'docker compose' nor 'docker-compose' found." >&2
    exit 1
fi

# ── Parse arguments ───────────────────────────────────────────────
RUN_UNIT=true
RUN_SWAGGER=false
BUILD_ONLY=false
CLEAN=false
TARGET_SERVICE=""

while [[ $# -gt 0 ]]; do
    case "$1" in
        --swagger)        RUN_SWAGGER=true; shift ;;
        --swagger-only)   RUN_UNIT=false; RUN_SWAGGER=true; shift ;;
        --build-only)     BUILD_ONLY=true; shift ;;
        --service)        TARGET_SERVICE="$2"; shift 2 ;;
        --clean)          CLEAN=true; shift ;;
        -h|--help)
            sed -n '/^#  ci-local/,/^# ══/p' "$0" | sed 's/^# \{0,2\}//'
            exit 0 ;;
        *)
            echo "Unknown option: $1  (try --help)" >&2; exit 1 ;;
    esac
done

# ── Colors (disabled if not a terminal) ───────────────────────────
if [[ -t 1 ]]; then
    RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'
    BLUE='\033[0;34m'; CYAN='\033[0;36m'; BOLD='\033[1m'; NC='\033[0m'
else
    RED=''; GREEN=''; YELLOW=''; BLUE=''; CYAN=''; BOLD=''; NC=''
fi

step()   { echo -e "\n${BLUE}▶  $*${NC}"; }
ok()     { echo -e "${GREEN}✓  $*${NC}"; }
warn()   { echo -e "${YELLOW}⚠  $*${NC}"; }
fail()   { echo -e "${RED}✗  $*${NC}"; }
info()   { echo -e "${CYAN}   $*${NC}"; }

# ── Load .env if present (for port overrides etc.) ────────────────
if [[ -f "$SCRIPT_DIR/.env" ]]; then
    # shellcheck disable=SC1091
    set -a; source "$SCRIPT_DIR/.env"; set +a
    info "Loaded $SCRIPT_DIR/.env"
fi

# ── Clean mode ────────────────────────────────────────────────────
if $CLEAN; then
    step "Cleaning CI artifacts..."
    $DC -f "$CI_COMPOSE"      down --rmi local --volumes --remove-orphans 2>/dev/null || true
    $DC -f "$SWAGGER_COMPOSE" down --rmi local --volumes --remove-orphans 2>/dev/null || true
    for svc in "${ALL_SERVICES[@]}"; do
        docker rmi "interactivemap/${svc}:test"    2>/dev/null || true
        docker rmi "interactivemap/${svc}:swagger" 2>/dev/null || true
    done
    ok "Clean complete"
    exit 0
fi

# ── Narrow to one service if --service was given ──────────────────
SERVICES=("${ALL_SERVICES[@]}")
if [[ -n "$TARGET_SERVICE" ]]; then
    SERVICES=("$TARGET_SERVICE")
fi

# ── Helper: wait for a docker compose service to be healthy ───────
_wait_healthy() {
    local svc="$1"
    local max_sec="$2"
    local compose_file="$3"
    local elapsed=0
    local interval=5

    while [[ $elapsed -lt $max_sec ]]; do
        local status
        status=$($DC -f "$compose_file" ps --format json "$svc" 2>/dev/null \
                 | grep -o '"Health":"[^"]*"' | cut -d'"' -f4 || true)

        [[ "$status" == "healthy" ]] && return 0

        sleep "$interval"
        elapsed=$((elapsed + interval))
    done

    fail "${svc} did not become healthy within ${max_sec}s"
    return 1
}

FAILED=()
START_TIME=$(date +%s)

# ═════════════════════════════════════════════════════════════════
#  PHASE 1 — Unit tests (via Dockerfile `test` stage)
# ═════════════════════════════════════════════════════════════════
if $RUN_UNIT; then
    step "Phase 1 — Unit tests"
    echo "  Each service is built to the 'test' Dockerfile stage."
    echo "  dotnet test runs during docker build — no mounts, no permission issues."

    for svc in "${SERVICES[@]}"; do
        step "Building + testing ${svc}..."

        BUILD_TARGET="${svc}-test"

        # Build the test stage; test failures cause non-zero exit → build fails
        if $DC -f "$CI_COMPOSE" build "$BUILD_TARGET"; then
            ok "${svc} unit tests passed"
        else
            fail "${svc} unit tests FAILED"
            FAILED+=("${svc}:unit")
            # Continue to next service rather than aborting the whole pipeline
        fi
    done
fi

# ═════════════════════════════════════════════════════════════════
#  PHASE 2 — Swagger integration test (full stack, Dev mode)
# ═════════════════════════════════════════════════════════════════
if $RUN_SWAGGER; then
    step "Phase 2 — Swagger integration test"

    SWAGGER_FAILED=false

    # Ensure stack is torn down on exit, even on error
    swagger_teardown() {
        step "Tearing down Swagger test stack..."
        $DC -f "$SWAGGER_COMPOSE" down --volumes --remove-orphans 2>/dev/null || true
        ok "Swagger stack stopped"
    }
    trap swagger_teardown EXIT

    # ── Build service images (final stage) ────────────────────────
    step "Building service images for Swagger test..."
    SWAGGER_BUILD_TARGETS=()
    for svc in "${SERVICES[@]}"; do
        SWAGGER_BUILD_TARGETS+=("$svc")
    done
    if ! $DC -f "$SWAGGER_COMPOSE" build "${SWAGGER_BUILD_TARGETS[@]}"; then
        fail "Swagger stack build failed"
        FAILED+=("swagger:build")
        SWAGGER_FAILED=true
    fi

    if ! $SWAGGER_FAILED; then
        # ── Start the full stack ──────────────────────────────────
        step "Starting Swagger test stack (postgres + services)..."
        $DC -f "$SWAGGER_COMPOSE" up -d postgres

        # Wait for postgres first
        info "Waiting for postgres to be healthy..."
        _wait_healthy "postgres" 60 "$SWAGGER_COMPOSE"

        # Start services
        $DC -f "$SWAGGER_COMPOSE" up -d "${SWAGGER_BUILD_TARGETS[@]}"

        # ── Wait helper (no `timeout` command — works on macOS) ───
        # Polls each service's Swagger JSON endpoint until 200 or timeout.

        declare -A SVC_PORTS=(
            [userservice]="${USERSERVICE_HTTP_PORT:-5280}"
            [locationservice]="${LOCATIONSERVICE_HTTP_PORT:-5282}"
            [reviewservice]="${REVIEWSERVICE_HTTP_PORT:-5284}"
        )

        for svc in "${SERVICES[@]}"; do
            port="${SVC_PORTS[$svc]}"
            url="http://localhost:${port}/swagger/v1/swagger.json"

            step "Waiting for ${svc} Swagger at ${url} ..."

            max_attempts=36   # 36 × 5s = 3 min max
            attempt=0
            success=false

            while [[ $attempt -lt $max_attempts ]]; do
                attempt=$((attempt + 1))

                http_code=""
                if command -v curl &>/dev/null; then
                    http_code=$(curl -s -o /dev/null -w "%{http_code}" \
                                     --connect-timeout 3 "$url" 2>/dev/null || true)
                elif command -v wget &>/dev/null; then
                    http_code=$(wget -q --spider --server-response "$url" 2>&1 \
                                 | awk '/HTTP\//{print $2}' | tail -1 || true)
                else
                    fail "Neither curl nor wget found — cannot test Swagger endpoint"
                    break
                fi

                if [[ "$http_code" == "200" ]]; then
                    success=true
                    break
                fi

                echo "   attempt ${attempt}/${max_attempts} → HTTP ${http_code:-no response}"
                sleep 5
            done

            if $success; then
                ok "${svc} Swagger is UP  →  ${BOLD}${url}${NC}"
                ok "${svc} Swagger UI     →  ${BOLD}http://localhost:${port}/swagger/index.html${NC}"
            else
                fail "${svc} Swagger did not become available after ${max_attempts} attempts"
                FAILED+=("${svc}:swagger")
                SWAGGER_FAILED=true

                # Dump logs to help diagnose startup failures
                warn "--- ${svc} container logs ---"
                $DC -f "$SWAGGER_COMPOSE" logs --tail=40 "$svc" 2>/dev/null || true
                warn "--- end ${svc} logs ---"
            fi
        done

        if ! $SWAGGER_FAILED; then
            echo ""
            info "All Swagger endpoints are reachable."
            info "Press Ctrl+C to stop the stack, or wait for teardown."
            # Give the developer a moment to inspect before auto-teardown
            sleep 3
        fi
    fi

    # Teardown runs via the EXIT trap above
fi

# ═════════════════════════════════════════════════════════════════
#  Summary
# ═════════════════════════════════════════════════════════════════
END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))

echo ""
echo -e "${BOLD}═══════════════════════════════════════════════════${NC}"
echo -e "${BOLD}  CI Pipeline Summary  (${DURATION}s)${NC}"
echo -e "${BOLD}═══════════════════════════════════════════════════${NC}"

if [[ ${#FAILED[@]} -eq 0 ]]; then
    ok "All checks passed"
    exit 0
else
    for item in "${FAILED[@]}"; do
        fail "$item"
    done
    echo ""
    fail "Pipeline FAILED"
    exit 1
fi
