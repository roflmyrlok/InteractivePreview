#!/bin/bash

# Kyiv Shelters Data Setup Script
# Run from: Kyiv.Data/ directory
# This script downloads and sets up data, then runs the application

set -e

# Configuration - data file in current directory
OUTPUT_FILE="kyiv_shelters.json"

# Function to print output
print_header() {
    echo ""
    echo "========================================"
    echo "$1"
    echo "========================================"
    echo ""
}

print_status() {
    echo "[INFO] $1"
}

print_success() {
    echo "[SUCCESS] $1"
}

print_warning() {
    echo "[WARNING] $1"
}

print_error() {
    echo "[ERROR] $1"
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

# Step 2: Download data if not present
download_data() {
    print_header "Step 2: Checking Data File"

    # If file exists, verify and skip download
    if [ -f "$OUTPUT_FILE" ]; then
        print_status "Found existing file: $OUTPUT_FILE"

        if command -v jq &> /dev/null; then
            if jq empty "$OUTPUT_FILE" 2>/dev/null; then
                local record_count=$(jq '.features | length' "$OUTPUT_FILE")
                print_success "File is valid with $record_count records. Skipping download."
                return 0
            else
                print_error "Existing file is not valid JSON"
                return 1
            fi
        else
            print_success "File exists. Skipping download."
            return 0
        fi
    fi

    # File doesn't exist, download it
    print_status "File not found. Downloading data..."

    local api_url="https://gisserver.kyivcity.gov.ua/mayno/rest/services/KYIV_API/Київ_Цифровий/MapServer/0/query?where=1%3D1&outFields=*&returnGeometry=true&f=pjson"

    if curl -f --progress-bar --max-time 300 \
        -H "User-Agent: Mozilla/5.0" \
        -H "Accept: application/json" \
        -o "$OUTPUT_FILE" \
        "$api_url" 2>/dev/null; then
        print_success "Data downloaded successfully"
        return 0
    else
        print_error "Download failed"
        return 1
    fi
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


# Step 3: Run the data acquisition app
run_data_acquisition() {
    print_header "Step 3: Running Data Acquisition App"

    print_status "Starting dotnet application..."
    print_status ""
    print_status "When prompted:"
    print_status "  1. 'Auth token?' → Paste your JWT token"
    print_status "  2. 'Service API URL?' → Example: http://host/api/locations"
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
    print_header "Data Acquisition - Setup & Run"

    print_status "This script will:"
    print_status "  1. Check dependencies"
    print_status "  2. Download data if not present"
    print_status "  3. Run the data acquisition app"
    print_status ""
    print_status "Current directory: $(pwd)"
    print_status ""

    # Step 1: Check dependencies
    check_dependencies

    # Step 2: Download data if not present
    if ! download_data; then
        print_error "Setup failed. Could not obtain data file."
        exit 1
    fi

    print_success "Setup completed successfully!"
    print_status ""

    # Step 3: Run app
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
