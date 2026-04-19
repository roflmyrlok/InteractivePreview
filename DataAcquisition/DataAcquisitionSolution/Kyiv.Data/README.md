# Kyiv Shelters Data Acquisition Application

This is the main application folder for downloading and importing Kyiv shelter data.

## 📁 Folder Structure

```
Kyiv.Data/
├── setup_kyiv_data.sh           ← Run this (macOS/Linux)
├── setup_kyiv_data.ps1          ← Run this (Windows)
├── README.md                    ← You are here
├── Program.cs                   ← Main application entry point
├── Kyiv.Data.csproj             ← .NET project file
├── Models/                      ← Data models
│   ├── CreateLocationCommand.cs
│   ├── GisResponse.cs
│   └── LocationDetailDto.cs
├── kyiv_shelters.json           ← Data file (created by setup script)
└── obj/                         ← Build artifacts (auto-generated)
```

## 🚀 Quick Start (One Command)

### macOS/Linux
```bash
chmod +x setup_kyiv_data.sh
./setup_kyiv_data.sh
```

### Windows (PowerShell)
```powershell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process
.\setup_kyiv_data.ps1
```

## ✨ What the Setup Script Does

1. ✓ Checks dependencies (curl, Python, .NET)
2. ✓ Downloads Kyiv GIS shelter data
3. ✓ Verifies the downloaded JSON data
4. ✓ Runs the data acquisition application
5. ✓ Guides you through entering required information

## 📋 What You'll Be Prompted For

When the application starts, answer these prompts:

### Prompt 1: Refresh data?
```
Refresh data from Kyiv API? (t/f): f
```
- `f` = Use the local file (`kyiv_shelters.json`) - **Choose this**
- `t` = Fetch fresh data from Kyiv API (slower, usually blocked by 403 error)

### Prompt 2: Authentication Token
```
Enter authentication token: 
```
Paste your JWT token from the InteractiveMap backend API.

**How to get a token:**
```bash
curl -X POST http://your-backend/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"user","password":"password"}'
```
Copy the `token` value from the response.

### Prompt 3: Kyiv API URL
```
Enter Kyiv API URL: 
```
Usually you can press Enter for the default, or ask your admin.

### Prompt 4: Location Service API URL
```
Enter Location Service API URL: 
```
Default: `http://localhost:5261/api/locations`

Or if running on a remote server:
```
http://your-server-ip:5261/api/locations
```

## 🔄 Data Flow

```
setup script
    ↓
Check dependencies
    ↓
Download kyiv_shelters.json
    ↓
Verify JSON format
    ↓
Run: dotnet run
    ↓
Program.cs
    ↓
- Read kyiv_shelters.json
- Parse GIS features
- Create location objects
- POST to Location Service API
    ↓
Data imported to backend
```

## 📝 Data File

**Location:** `kyiv_shelters.json`
- **Size:** ~15 MB
- **Format:** GeoJSON with ArcGIS REST API structure
- **Contents:** Features for each shelter with:
  - Address
  - Coordinates (latitude/longitude)
  - Disabled access info
  - Phone, district, owner
  - Shelter type & category
  - Building type
  - Operating hours
  - Description

## 🛠 Manual Setup (If Script Fails)

### Step 1: Download Data Manually

Open this URL in your browser:
```
https://gisserver.kyivcity.gov.ua/mayno/rest/services/KYIV_API/Київ_Цифровий/MapServer/0/query?where=1%3D1&outFields=*&returnGeometry=true&f=pjson
```

Right-click → **Save As** → `kyiv_shelters.json`

Place the file in this directory (`Kyiv.Data/`)

### Step 2: Run the Application

```bash
dotnet run
```

Then answer the 4 prompts above.

## 🔧 Run Without Setup Script

If you already have `kyiv_shelters.json` in this folder:

```bash
dotnet run
# Answer: f, [token], [api urls]
```

## 📊 Expected Output

```
=== Kyiv Shelters Data Acquisition ===

Refresh data from Kyiv API? (t/f): f
Loading data from local file...

Enter authentication token: eyJhbGci...
Enter Kyiv API URL: https://api.kyiv...
Enter Location Service API URL: http://localhost:5261/api/locations

Processing shelter data...
Found 1247 shelters to process
Creating locations in service...

[1/1247] ✓ Created: вул. Героїв Небесної Сотні, 1
[2/1247] ✓ Created: вул. Шевченка, 47
...
[1247/1247] ✓ Created: вул. Лесі Українки, 26

Results: 1245 successful, 2 failed

Data processing completed successfully!
```

## ❌ Troubleshooting

### "curl is not installed"
```bash
# macOS
brew install curl

# Ubuntu/Debian
sudo apt-get install curl

# Windows - usually pre-installed, use PowerShell script
```

### "python3 not found"
```bash
# macOS
brew install python3

# Ubuntu/Debian
sudo apt-get install python3

# Windows - download from python.org
```

### ".NET SDK not found"
```bash
# macOS
brew install dotnet

# Ubuntu/Debian
curl https://dot.net/v1/dotnet-install.sh | bash

# Windows
# Download from https://dotnet.microsoft.com/download/dotnet/8.0
```

### Download fails with 403 error
The server is blocking automated requests. Use the browser method:
1. Open the URL in your browser (see Manual Setup above)
2. Save the JSON file
3. Run `dotnet run`

### "401 Unauthorized" when creating locations
Your JWT token is invalid or expired:
1. Get a fresh token from the auth API
2. Run the app again

### "Connection refused" errors
The Location Service is not running:
1. Start the backend service first
2. Verify it's on the correct port (default: 5261)
3. Run the app again

## 📚 Full Documentation

For detailed information, see the parent folder documentation:
- Parent: `DataAcquisition/`
- Grandparent: `InteractiveMap/`

Files include:
- `KYIV_GIS_DOWNLOAD_GUIDE.md`
- `DATA_ACQUISITION_RUN_GUIDE.md`
- `403_ERROR_SOLUTIONS.md`
- `SETUP_SCRIPT_USAGE.md`

## 🎯 Next Steps After Setup

1. Run the setup script
2. Answer the 4 prompts
3. Monitor the import progress
4. Verify data in the InteractiveMap app
5. Check backend logs for any import errors

## 💡 Tips

- **Keep it simple:** Use local file (`f` option) on first run
- **Save the token:** You'll need it each time you run
- **Check the backend:** Make sure Location Service is running before importing
- **Monitor progress:** The app shows real-time status for each location

## 📄 Files Reference

| File | Purpose |
|------|---------|
| `setup_kyiv_data.sh` | Automated setup + run (Linux/macOS) |
| `setup_kyiv_data.ps1` | Automated setup + run (Windows) |
| `Program.cs` | Main application logic |
| `Kyiv.Data.csproj` | .NET project configuration |
| `Models/CreateLocationCommand.cs` | Location data model |
| `Models/GisResponse.cs` | GIS API response structure |
| `Models/LocationDetailDto.cs` | Location detail model |

## ✅ Requirements

- .NET 8.0 SDK
- curl or Python 3 (for downloading data)
- Valid JWT token from backend
- Location Service API running
- Internet connection (for initial data download)

## 🚀 Ready to Go!

Run the setup script and follow the prompts:

```bash
./setup_kyiv_data.sh    # macOS/Linux
# or
.\setup_kyiv_data.ps1   # Windows
```

That's all you need!

