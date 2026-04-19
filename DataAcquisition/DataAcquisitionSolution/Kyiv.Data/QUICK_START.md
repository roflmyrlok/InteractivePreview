# Kyiv Data Acquisition - Quick Start Guide

## 🚀 One Command - Everything Automated

Navigate to this folder (`Kyiv.Data/`) and run:

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

## ✨ What Happens Automatically

```
setup_kyiv_data script
    ↓
✓ Checks: curl, Python, .NET
    ↓
✓ Downloads: Kyiv GIS shelter data
    ↓
✓ Verifies: JSON format (1247 shelters)
    ↓
✓ Runs: dotnet run
    ↓
Asks 4 questions → You answer → Data imports
```

## 📋 4 Questions You'll Answer

When the app starts, be ready with:

### Question 1: Refresh data?
```
Refresh data from Kyiv API? (t/f): 
```
**Answer:** `f` (use local kyiv_shelters.json)

### Question 2: Authentication token
```
Enter authentication token:
```
**Get it first:**
```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"user","password":"pass"}'
```
Copy the token from response and paste it.

### Question 3: Kyiv API URL
```
Enter Kyiv API URL:
```
**Press Enter** for default, or ask your admin

### Question 4: Location Service URL
```
Enter Location Service API URL:
```
**Default:** `http://localhost:5261/api/locations`

## ✅ Expected Result

After answering questions, you'll see:

```
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

## 📂 File Locations

Everything happens in this folder:

```
Kyiv.Data/
├── setup_kyiv_data.sh         ← The script you run
├── setup_kyiv_data.ps1        ← Windows version
├── kyiv_shelters.json         ← Created by script
├── Program.cs                 ← App logic
└── README.md                  ← Full documentation
```

## 🔧 If Download Fails (403 Error)

The server might block automated downloads. Try:

### Browser Download
1. Open: https://gisserver.kyivcity.gov.ua/mayno/rest/services/KYIV_API/Київ_Цифровий/MapServer/0/query?where=1%3D1&outFields=*&returnGeometry=true&f=pjson
2. Right-click → Save As → `kyiv_shelters.json`
3. Place in this folder
4. Run script again - it will detect the file

## 🎯 Folder Path

Make sure you're in the right place:

```
InteractiveMap/
└── DataAcquisition/
    └── DataAcquisitionSolution/
        └── Kyiv.Data/           ← You are here
            ├── setup_kyiv_data.sh
            ├── setup_kyiv_data.ps1
            └── README.md
```

## 💡 That's It!

One command does everything:
1. Download data
2. Verify data
3. Run the app
4. Import to backend

No complex setup needed!

---

**Questions?** See `README.md` for full documentation.
