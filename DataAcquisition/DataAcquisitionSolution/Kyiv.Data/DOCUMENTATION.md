# Documentation Index

This document points to all available documentation for the Kyiv Shelters Data Acquisition application.

## 📍 You Are Here

```
DataAcquisition/DataAcquisitionSolution/Kyiv.Data/
```

## 📚 Documentation Files (In This Folder)

### Quick References
1. **[QUICK_START.md](QUICK_START.md)** — Ultra-quick one-command setup
   - How to run the script
   - The 4 prompts you'll answer
   - What to do if download fails

2. **[README.md](README.md)** — Complete application documentation
   - Full folder structure
   - Setup instructions
   - What the script does
   - All 4 prompts explained in detail
   - Troubleshooting guide

### Setup Scripts (In This Folder)
- **setup_kyiv_data.sh** — Run on Linux/macOS
- **setup_kyiv_data.ps1** — Run on Windows

## 📚 Documentation Files (Parent Folders)

These files are in the parent `InteractiveMap/` folder for reference:

### Data Acquisition
- **DATA_ACQUISITION_RUN_GUIDE.md** — Detailed run guide (legacy)
- **DATA_ACQUISITION_QUICK_START.md** — Quick start (legacy)

### GIS Download
- **KYIV_GIS_DOWNLOAD_GUIDE.md** — How to download GIS data
- **DOWNLOAD_QUICK_REFERENCE.md** — Quick reference for downloads
- **403_ERROR_SOLUTIONS.md** — Troubleshooting 403 errors
- **FIX_403_FORBIDDEN.md** — Fixing 403 Forbidden errors
- **SETUP_SCRIPT_USAGE.md** — Legacy setup script documentation

### Implementation
- **IMPLEMENTATION_GUIDE.md** — iOS app backend integration guide
- **API-Documentation.md** — Backend API documentation
- **BACKEND_MIGRATION_ANALYSIS.md** — Backend analysis

## 🚀 Quick Navigation

### "I just want to run it!"
→ Read: **[QUICK_START.md](QUICK_START.md)**
→ Run: `./setup_kyiv_data.sh`

### "I want to understand what it does"
→ Read: **[README.md](README.md)**

### "The download is failing (403 error)"
→ Read: Parent folder `FIX_403_FORBIDDEN.md`

### "I want to understand the GIS data format"
→ Read: Parent folder `KYIV_GIS_DOWNLOAD_GUIDE.md`

### "I want to understand the API"
→ Read: Parent folder `API-Documentation.md`

## 📂 Full Project Structure

```
InteractiveMap/
├── DOCUMENTATION files (parent folder)
├── DataAcquisition/
│   ├── DataAcquisitionSolution/
│   │   ├── Kyiv.Data/              ← YOU ARE HERE
│   │   │   ├── setup_kyiv_data.sh
│   │   │   ├── setup_kyiv_data.ps1
│   │   │   ├── QUICK_START.md      ← START HERE
│   │   │   ├── README.md           ← FULL GUIDE
│   │   │   ├── DOCUMENTATION.md    ← THIS FILE
│   │   │   ├── Program.cs
│   │   │   ├── Kyiv.Data.csproj
│   │   │   ├── Models/
│   │   │   └── kyiv_shelters.json
│   │   └── DataAcquisitionSolution.sln
│   ├── .gitignore
│   └── Dockerfile
└── [other project files]
```

## ⚡ One-Command Summary

```bash
cd DataAcquisition/DataAcquisitionSolution/Kyiv.Data
./setup_kyiv_data.sh
```

Or on Windows:
```powershell
cd DataAcquisition\DataAcquisitionSolution\Kyiv.Data
.\setup_kyiv_data.ps1
```

## 📖 Reading Order

### First Time Users
1. **QUICK_START.md** (2 min) — Get the overview
2. **Run the script** (5 min) — Execute
3. **README.md** (5 min) — Deep dive if needed

### Troubleshooting
1. Check **README.md** "Troubleshooting" section
2. If 403 error → See parent folder **FIX_403_FORBIDDEN.md**
3. If other issues → Check relevant documentation above

## 🎯 Key Concepts

### The Script
- **Location:** `Kyiv.Data/setup_kyiv_data.sh` (or .ps1 on Windows)
- **Purpose:** Automate download, verify, and run the app
- **Usage:** Just run it and answer 4 prompts

### The Data
- **File:** `Kyiv.Data/kyiv_shelters.json`
- **Size:** ~15 MB
- **Format:** GeoJSON with ArcGIS features
- **Contents:** 1247 shelter locations

### The Application
- **Folder:** `Kyiv.Data/`
- **Entry Point:** `Program.cs`
- **Language:** C# (.NET 8.0)
- **Purpose:** Read JSON, create locations, import to backend

## 💡 Tips

- **Start with QUICK_START.md** — It's literally 2 minutes
- **Scripts are self-documenting** — They show colored status messages
- **All documentation is local** — No internet needed (except for downloads)
- **Scripts handle failures** — They suggest alternatives if download fails

## ✅ Checklist

Before running:
- [ ] You're in: `DataAcquisition/DataAcquisitionSolution/Kyiv.Data/`
- [ ] You've read: QUICK_START.md
- [ ] You have: .NET SDK installed
- [ ] You have: curl or Python installed
- [ ] You have: JWT token ready (or know how to get one)

## 🚀 Ready?

```bash
./setup_kyiv_data.sh
```

That's all you need to do!

---

**Questions?** Check the relevant documentation file above.

