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

## Important Security Note
This project currently includes default database and broker settings intended for local/demo use. Before production or wider sharing:
- Replace demo credentials
- Move secrets out of source-controlled assets
- Use environment-specific secure configuration

## Repository
GitHub: https://github.com/donbtirwomwe/unity_mqtt_msql_project
