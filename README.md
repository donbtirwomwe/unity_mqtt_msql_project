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
   `C:/Users/donbt/projects/unity_mqtt_msql_project`
2. Use Unity Editor version `6000.3.10f1`.
3. Open scene: `Assets/Scenes/Industrial_Demo.unity` (or `AssetViewer.unity`).
4. Configure DB/MQTT values in `Assets/Resources/DBConfig.asset`.
5. Press Play and use the UI to load and monitor assets.

## Local Containers (SQL + MQTT)
Use this when teammates do not have access to your Nexus runtime.

1. Install Docker Desktop.
2. From the repository root, run:
   `pwsh ./containers/up.ps1`
3. Verify dependencies are reachable before opening Unity:
   `pwsh ./containers/check-connectivity.ps1`
4. This starts:
   - SQL Server on `localhost:1433`
   - MQTT broker on `localhost:1884`
5. Set Unity DB config values to local:
   - `serverIp = 127.0.0.1`
   - `port = 1433`
   - `database = IndustrialAssets`
   - `userId = sa`
   - `password = Industrial@Demo2026!`
   - `mqttServerIp = 127.0.0.1`
   - `mqttPort = 1884`

The SQL container auto-creates the `IndustrialAssets` database and seeds demo data from:
- `containers/mssql/init/seed_leaktest_pressure01.sql`

Stop containers:
- `pwsh ./containers/down.ps1`

## Important Security Note
This project currently includes default database and broker settings intended for local/demo use. Before production or wider sharing:
- Replace demo credentials
- Move secrets out of source-controlled assets
- Use environment-specific secure configuration

## Repository
GitHub: https://github.com/donbtirwomwe/unity_mqtt_msql_project
