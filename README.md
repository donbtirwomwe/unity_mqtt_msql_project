# unity_mqtt_msql_project

Unity demo project for industrial asset monitoring using:
- SQL Server for asset, datapoint, channel, and file metadata
- MQTT for live telemetry updates
- Runtime UI for asset loading, datapoint selection, and document link opening

## Stack
- Unity Editor: 6000.3.10f1
- C# scripts in `Assets`
- SQL Server via `System.Data.SqlClient`
- MQTT via M2Mqtt / M2MqttUnity

## Current Project Scope
- Load assets from SQL (`ASSETS`, `DATAPOINTS`, `DATACHANNELS`, `DATAFILES`)
- Auto-populate asset dropdown and details panel
- Subscribe/unsubscribe to datapoint MQTT topics
- Show live telemetry in UI panels
- Open linked documents from datapoint file references (web/UNC/local)
- Scene demo content and tutorials included

## Project Structure
- `Assets/` Unity scenes, scripts, prefabs, media
- `Packages/` Unity package manifest and locks
- `ProjectSettings/` Unity project settings
- `Tools/` helper scripts
- `DummyDocs/` sample docs used by file-link features

## Setup
1. Open the project folder in Unity Hub:
   `<path-to-cloned-repo>`
2. Use Unity Editor version `6000.3.10f1`.
3. Open scene: `Assets/Scenes/Industrial_Demo.unity` (or `AssetViewer.unity`).
4. Configure DB/MQTT values in `Assets/Resources/DBConfig.asset`.
5. Press Play and use the UI to load and monitor assets.

## Local Containers (SQL + MQTT)
Use this when teammates do not have access to your Nexus runtime.

1. Install Docker Desktop.
2. From the repository root, run:
   `pwsh ./containers/up.ps1`
   This generates broker auth files from `containers/.env` before startup.
3. Verify dependencies are reachable before opening Unity:
   `pwsh ./containers/check-connectivity.ps1`
4. This starts:
   - SQL Server on `localhost:1433`
   - MQTT broker on `localhost:1884`
5. Set Unity DB config values to local:
   - `serverIp = 127.0.0.1`
   - `port = 1433`
   - `database = IndustrialAssets`
   - `userId = app_reader`
   - `password = <your-app-db-password>`
   - `mqttServerIp = 127.0.0.1`
   - `mqttPort = 1884`
   - `mqttUserName = <your-mqtt-app-username>`
   - `mqttPassword = <your-mqtt-app-password>`

If those four credential fields are left blank in `Assets/Resources/DBConfig.asset`, Unity will fall back to `containers/.env` for local-editor runs.

The Unity client now reads data through stored procedures instead of direct table queries, and the seeded MQTT routes use opaque aliases under `mx/...` instead of descriptive process paths.

The SQL container auto-creates the `IndustrialAssets` database and seeds demo data from:
- `containers/mssql/init/seed_leaktest_pressure01.sql`

To import your own data into the database, use the provided Excel template:
- `Tools/DB_Import_Template.xlsx`

Open it in Excel, fill in the **ASSETS**, **DATAPOINTS**, **DATACHANNELS**, and **DATAFILES** sheets following the column notes in row 2.
The **TOPICMAPPINGS** and **ASSETACCESS** sheets are reference-only — those are generated automatically by the DB bootstrap scripts, not manually populated.

Stop containers:
- `pwsh ./containers/down.ps1`

## Integrating Into An Existing Unity Project
Use this if a teammate already has a Unity project and scene, but needs this repo's SQL Server + MQTT backend and the Unity-side runtime integration added to that existing scene.

There are two parts to integrate:
- the backend stack: SQL Server + MQTT broker + init scripts under `containers/`
- the Unity scene/runtime pieces: scripts, config asset, MQTT client libraries, and optional demo UI assets under `Assets/`

### Copy These Into The Target Unity Project
- `Assets/Config/DBConfig.cs`
- `Assets/Config/DBConfigHolder.cs` (optional helper)
- `Assets/AssetController.cs`
- `Assets/Scripts/AssetLoaderDemo.cs`
- `Assets/Scripts/Models/`
- `Assets/UnityMainThreadDispatcher.cs`
- `Assets/M2Mqtt/`
- `Assets/M2MqttUnity/`
- `Assets/Plugins/` if the target project does not already contain the required MQTT/.NET assemblies

Keep the same `Assets/...` relative paths where possible. The runtime code loads `Resources/DBConfig` by name, so `DBConfig.asset` must end up at `Assets/Resources/DBConfig.asset`.

### Backend Files To Bring Along
If they do not already have SQL Server and MQTT infrastructure, they also need these non-Unity files from this repo:
- `containers/up.ps1`
- `containers/down.ps1`
- `containers/check-connectivity.ps1`
- `containers/docker-compose.yml`
- `containers/.env.example`
- `containers/mqtt/`
- `containers/mssql/`

These files do not go inside the Unity `Assets/` tree. They should live alongside the Unity project repo, or in a sibling repo/folder the teammate will run locally.

### Optional Demo UI Assets
Copy these only if they want the same demo scene/prefab flow:
- `Assets/AssetUI_Prefab.prefab`
- `Assets/Prefabs/`
- `Assets/UI/`
- any directly referenced art/material assets used by the prefab or scene

### Do Not Copy
- `Library/`
- `Temp/`
- `Logs/`
- `UserSettings/`
- `Assets/_Recovery/`
- `.vscode/`
- `ProjectSettings/` as a full replacement
- `Packages/manifest.json` as a full replacement

### Target Project Local Setup
Each teammate should create or update these local-only files in the target Unity project and backend folder:

1. `Assets/Resources/DBConfig.asset`
   - Create via Unity menu: `Assets > Create > Config > DBConfig`
   - Place it at `Assets/Resources/DBConfig.asset`
   - Set at minimum:
     - `serverIp`
     - `port`
     - `database`
     - `mqttServerIp`
     - `mqttPort`
     - optionally `userId`, `password`, `mqttUserName`, `mqttPassword`

2. Local backend config
   - Copy `containers/.env.example` to `containers/.env`
   - Fill in local secrets such as:
     - `SA_PASSWORD`
     - `APP_DB_USER`
     - `APP_DB_PASSWORD`
     - `MQTT_APP_USERNAME`
     - `MQTT_APP_PASSWORD`

If the credential fields in `Assets/Resources/DBConfig.asset` are left blank, the Unity code can fall back to values in `containers/.env` for local editor runs.

### Required Manual Wiring In The Target Scene
1. Add the imported runtime script to the target scene:
   - `AssetController` for the simpler controller flow, or
   - `AssetLoaderDemo` for the demo asset-loading flow
2. Assign the `DBConfig` asset to any scene objects that expose it.
3. Make sure the target project has the required Unity packages and assembly compatibility settings for the imported plugins.
4. Start the local SQL + MQTT backend before Play mode:
   - `pwsh ./containers/up.ps1`
   - `pwsh ./containers/check-connectivity.ps1`

### What The Teammate Is Actually Adding
If their existing scene currently has no SQL or MQTT support, they are adding:
- a local SQL Server container seeded with the required schema and demo data
- a local MQTT broker container with auth/ACL setup
- Unity scripts that query SQL and subscribe to MQTT topics
- a `DBConfig` asset that tells the scene how to connect to both services

### Package Merge Guidance
Do not overwrite the target project's `Packages/manifest.json`.
Instead, compare this repo's package manifest and merge only the missing package entries the imported assets/scripts require.

## Important Security Note
This project currently includes default database and broker settings intended for local/demo use. Before production or wider sharing:
- Replace demo credentials
- Move secrets out of source-controlled assets
- Use environment-specific secure configuration

## Repository
GitHub: https://github.com/donbtirwomwe/unity_mqtt_msql_project
