#!/bin/bash

# Kyiv Shelters Data Setup Script
# Run from: Kyiv.Data/ directory
# This script downloads and sets up data, then runs the application

set -e

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Configuration - data file in current directory
OUTPUT_FILE="kyiv_shelters.json"

# Function to print colored output
print_header() {
    echo ""
    echo -e "${BLUE}========================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}========================================${NC}"
    echo ""
}

print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[✓]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[!]${NC} $1"
}

print_error() {
    echo -e "${RED}[✗]${NC} $1"
}

# Step 1: Check dependencies
check_dependencies() {
    print_header "Step 1: Checking Dependencies"

    local missing=0

    if ! command -v curl &> /dev/null; then
        print_error "curl is not installed"
        missing=$((missing + 1))
    else
        print_success "curl found"
    fi

    if ! command -v python3 &> /dev/null; then
        print_warning "python3 not found (will try curl fallback)"
    else
        print_success "python3 found"
    fi

    if ! command -v jq &> /dev/null; then
        print_warning "jq not found (optional, for data verification)"
    else
        print_success "jq found"
    fi

    if ! command -v dotnet &> /dev/null; then
        print_error ".NET SDK not found"
        missing=$((missing + 1))
    else
        print_success "dotnet found"
    fi

    if [ $missing -gt 0 ]; then
        print_error "Missing required tools. Please install them and try again."
        exit 1
    fi
}

# Step 2: Try to download the data
download_data() {
    print_header "Step 2: Downloading Kyiv GIS Data"

    local api_url="https://gisserver.kyivcity.gov.ua/mayno/rest/services/KYIV_API/Київ_Цифровий/MapServer/0/query?where=1%3D1&outFields=*&returnGeometry=true&f=pjson"

    # Method 1: Try with Python requests (more reliable)
    if command -v python3 &> /dev/null; then
        print_status "Attempting download with Python (Method 1)..."

        python3 << 'PYTHON_EOF'
import requests
import json
import sys

url = "https://gisserver.kyivcity.gov.ua/mayno/rest/services/KYIV_API/Київ_Цифровий/MapServer/0/query"
params = {
    "where": "1=1",
    "outFields": "*",
    "returnGeometry": "true",
    "f": "pjson"
}

headers = {
    "User-Agent": "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36",
    "Referer": "https://gisserver.kyivcity.gov.ua/",
    "Accept": "application/json",
}

try:
    session = requests.Session()
    session.headers.update(headers)
    response = session.get(url, params=params, timeout=300, verify=True)

    if response.status_code == 200:
        data = response.json()

        with open("kyiv_shelters.json", "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)

        feature_count = len(data.get("features", []))
        print(f"✓ SUCCESS: Downloaded {feature_count} features")
        sys.exit(0)
    else:
        print(f"✗ HTTP {response.status_code}")
        sys.exit(1)
except Exception as e:
    print(f"✗ Error: {e}")
    sys.exit(1)
PYTHON_EOF

        if [ $? -eq 0 ]; then
            return 0
        fi
    fi

    # Method 2: Try with curl and headers
    print_status "Attempting download with curl (Method 2)..."

    if curl -f --progress-bar --max-time 300 \
        -A "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36" \
        -H "Referer: https://gisserver.kyivcity.gov.ua/" \
        -H "Accept: application/json" \
        -o "$OUTPUT_FILE" \
        "$api_url" 2>/dev/null; then
        print_success "Data downloaded with curl"
        return 0
    fi

    # If both methods fail
    print_warning "Automated download failed (server blocking requests)"
    return 1
}

# Step 3: Verify the downloaded data
verify_data() {
    print_header "Step 3: Verifying Data"

    if [ ! -f "$OUTPUT_FILE" ]; then
        print_error "File not found: $OUTPUT_FILE"
        return 1
    fi

    FILE_SIZE=$(du -h "$OUTPUT_FILE" | cut -f1)
    print_status "File size: $FILE_SIZE"

    # Check if it's valid JSON with features
    if command -v jq &> /dev/null; then
        if jq empty "$OUTPUT_FILE" 2>/dev/null; then
            local record_count=$(jq '.features | length' "$OUTPUT_FILE")
            print_success "Valid JSON with $record_count shelter records"
            return 0
        else
            print_error "File is not valid JSON"
            return 1
        fi
    else
        # Fallback check without jq
        if grep -q '"features"' "$OUTPUT_FILE"; then
            print_success "File appears to be valid GeoJSON"
            return 0
        else
            print_error "File does not contain features data"
            return 1
        fi
    fi
}

# Step 4: Handle download failure with alternatives
handle_download_failure() {
    print_header "Download Failed - Alternative Methods"

    print_warning "Automated download was blocked by the server"
    print_status ""
    print_status "Option 1: Browser Download (Most Reliable)"
    print_status "  1. Open this URL in your browser:"
    print_status "     https://gisserver.kyivcity.gov.ua/mayno/rest/services/KYIV_API/Київ_Цифровий/MapServer/0/query?where=1%3D1&outFields=*&returnGeometry=true&f=pjson"
    print_status "  2. Wait for JSON to load"
    print_status "  3. Right-click → Save As → kyiv_shelters.json"
    print_status "  4. Place file in this directory (Kyiv.Data/)"
    print_status "  5. Run this script again"
    print_status ""
    print_status "Option 2: Use Python Directly"
    print_status "  python3 << 'EOF'"
    print_status "  import requests, json"
    print_status "  url = 'https://gisserver.kyivcity.gov.ua/mayno/rest/services/KYIV_API/Київ_Цифровий/MapServer/0/query'"
    print_status "  params = {'where': '1=1', 'outFields': '*', 'returnGeometry': 'true', 'f': 'pjson'}"
    print_status "  response = requests.get(url, params=params)"
    print_status "  with open('kyiv_shelters.json', 'w') as f: json.dump(response.json(), f)"
    print_status "  EOF"
    print_status ""

    # Check if file was manually downloaded
    if [ -f "$OUTPUT_FILE" ]; then
        print_status "Found $OUTPUT_FILE in current directory"
        read -p "Should I use this file? (y/n) " -r response
        if [[ "$response" =~ ^[Yy]$ ]]; then
            if verify_data; then
                return 0
            fi
        fi
    fi

    return 1
}

# Step 5: Run the data acquisition app
run_data_acquisition() {
    print_header "Step 5: Running Data Acquisition App"

    print_status "Starting dotnet application..."
    print_status ""
    print_status "When prompted:"
    print_status "  1. 'Refresh data?' → Answer: f (use local file: $OUTPUT_FILE)"
    print_status "  2. 'Auth token?' → Paste your JWT token"
    print_status "  3. 'Kyiv API URL?' → Keep default or ask your admin"
    print_status "  4. 'Service API URL?' → Usually: http://localhost:5261/api/locations"
    print_status ""

    read -p "Press Enter to start the app..." -r

    if [ -f "$OUTPUT_FILE" ]; then
        print_status "$OUTPUT_FILE found in current directory"
        print_status "Running: dotnet run"
        print_status ""
        dotnet run
    else
        print_error "$OUTPUT_FILE not found"
        return 1
    fi
}

# Main execution
main() {
    print_header "Kyiv Shelters - Automated Setup & Run"

    print_status "This script will:"
    print_status "  1. Check dependencies"
    print_status "  2. Download Kyiv GIS shelter data"
    print_status "  3. Verify the downloaded data"
    print_status "  4. Run the data acquisition app"
    print_status ""
    print_status "Current directory: $(pwd)"
    print_status ""

    # Step 1: Check dependencies
    check_dependencies

    # Step 2: Download data
    if download_data; then
        print_success "Data download completed"
    else
        if ! handle_download_failure; then
            print_error "Setup failed. Please download data manually and try again."
            exit 1
        fi
    fi

    # Step 3: Verify data
    if ! verify_data; then
        print_error "Data verification failed"
        exit 1
    fi

    print_success "Setup completed successfully!"
    print_status ""

    # Step 4: Run app
    print_status "Ready to run the data acquisition app"
    read -p "Continue to app? (y/n) " -r response

    if [[ "$response" =~ ^[Yy]$ ]]; then
        run_data_acquisition
    else
        print_success "Setup complete!"
        print_status "To run the app later, execute:"
        print_status "  dotnet run"
    fi
}

# Run main function
main
