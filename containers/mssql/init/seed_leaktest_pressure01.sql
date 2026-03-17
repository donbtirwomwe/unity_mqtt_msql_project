-- Repo copy used by Docker compose init.
-- Source of truth is Assets/SQL/seed_leaktest_pressure01.sql.

IF DB_NAME() = 'master'
BEGIN
	RAISERROR('Do not run seed_leaktest_pressure01.sql against master. Select your application database first.', 16, 1);
	RETURN;
END;

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.ASSETS', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.ASSETS
	(
		ID NVARCHAR(64) NOT NULL,
		Name NVARCHAR(256) NULL,
		Description NVARCHAR(1024) NULL,
		MessagingRole NVARCHAR(32) NULL,
		Status INT NOT NULL CONSTRAINT DF_ASSETS_Status DEFAULT(0),
		CONSTRAINT PK_ASSETS PRIMARY KEY (ID)
	);
END;

IF COL_LENGTH('dbo.ASSETS', 'MessagingRole') IS NULL
BEGIN
	ALTER TABLE dbo.ASSETS ADD MessagingRole NVARCHAR(32) NULL;
END;

IF COL_LENGTH('dbo.ASSETS', 'MessagingRoleCode') IS NULL
BEGIN
	ALTER TABLE dbo.ASSETS ADD MessagingRoleCode INT NULL;
END;

IF OBJECT_ID(N'dbo.DATAPOINTS', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.DATAPOINTS
	(
		ID NVARCHAR(64) NOT NULL,
		ASSET_ID NVARCHAR(64) NOT NULL,
		Name NVARCHAR(256) NULL,
		Description NVARCHAR(1024) NULL,
		Status INT NOT NULL CONSTRAINT DF_DATAPOINTS_Status DEFAULT(0),
		CONSTRAINT PK_DATAPOINTS PRIMARY KEY (ID)
	);
END;

IF OBJECT_ID(N'dbo.DATACHANNELS', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.DATACHANNELS
	(
		ID NVARCHAR(64) NOT NULL,
		ASSET_ID NVARCHAR(64) NOT NULL,
		DATAPOINT_ID NVARCHAR(64) NOT NULL,
		Name NVARCHAR(256) NULL,
		Description NVARCHAR(1024) NULL,
		Target NVARCHAR(1024) NULL,
		CONSTRAINT PK_DATACHANNELS PRIMARY KEY (ID)
	);
END;

IF OBJECT_ID(N'dbo.DATAFILES', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.DATAFILES
	(
		ID NVARCHAR(64) NOT NULL,
		ASSET_ID NVARCHAR(64) NOT NULL,
		DATAPOINT_ID NVARCHAR(64) NOT NULL,
		Name NVARCHAR(256) NULL,
		Description NVARCHAR(1024) NULL,
		Type NVARCHAR(64) NULL,
		Link NVARCHAR(1024) NULL,
		CONSTRAINT PK_DATAFILES PRIMARY KEY (ID)
	);
END;

DELETE FROM DATAFILES WHERE ASSET_ID IN ('b101', 'b102');
DELETE FROM DATACHANNELS WHERE ASSET_ID IN ('b101', 'b102');
DELETE FROM DATAPOINTS WHERE ASSET_ID IN ('b101', 'b102');
DELETE FROM ASSETS WHERE ID IN ('b101', 'b102');

INSERT INTO ASSETS (ID, Name, Description, Status) VALUES
('b101', 'Test Station', 'leaktest - Performs pressure decay and leak-rate qualification.', 1),
('b102', 'Pressure Sensor', 'pressure01 - Primary pressure transducer feeding leak test telemetry.', 1);

IF COL_LENGTH('dbo.ASSETS', 'MessagingRole') IS NOT NULL
BEGIN
	EXEC('UPDATE ASSETS SET MessagingRole = ''subscriber'' WHERE ID = ''b101'';');
	EXEC('UPDATE ASSETS SET MessagingRole = ''both'' WHERE ID = ''b102'';');
END;

IF COL_LENGTH('dbo.ASSETS', 'MessagingRoleCode') IS NOT NULL
BEGIN
	EXEC('UPDATE ASSETS SET MessagingRoleCode = 0 WHERE ID = ''b101'';');
	EXEC('UPDATE ASSETS SET MessagingRoleCode = 2 WHERE ID = ''b102'';');
END;

INSERT INTO DATAPOINTS (ID, ASSET_ID, Name, Description, Status) VALUES
('c101', 'b101', 'Pressure Hold', 'Validates pressure hold during timed window.', 1),
('c102', 'b101', 'Fill and Charge', 'Tracks fill flow and ramp-up pressure profile.', 1),
('c103', 'b101', 'Temperature Compensation', 'Compensates pressure against ambient/part temperature.', 1),
('c104', 'b101', 'Gross Leak Detection', 'Immediate alarm for rapid pressure drop events.', 1),
('c105', 'b101', 'Fine Leak Quantification', 'Computes leak rate versus acceptance limit.', 1),
('c106', 'b101', 'Test Verdict', 'Pass/fail decision and reason code.', 1),
('d101', 'b102', 'Sensor Telemetry', 'Live transducer readings and health metrics.', 1),
('d102', 'b102', 'Sensor Diagnostics', 'Calibration and drift indicators.', 1);

INSERT INTO DATACHANNELS (ID, ASSET_ID, DATAPOINT_ID, Name, Description, Target) VALUES
('e101', 'b101', 'c101', 'Hold Pressure PSI', 'Current hold pressure.', 'plant/leaktest/hold/pressure_psi'),
('e102', 'b101', 'c101', 'Hold Timer Sec', 'Elapsed hold timer.', 'plant/leaktest/hold/timer_s'),
('e103', 'b101', 'c101', 'Pressure Decay', 'Pressure decay slope during hold.', 'plant/leaktest/hold/decay_rate_psi_s'),
('e104', 'b101', 'c102', 'Fill Flow SCCM', 'Fill flow command/actual.', 'plant/leaktest/fill/flow_sccm'),
('e105', 'b101', 'c102', 'Charge Pressure PSI', 'Pressure during charge ramp.', 'plant/leaktest/fill/charge_pressure_psi'),
('e106', 'b101', 'c103', 'Ambient Temp C', 'Ambient temperature.', 'plant/leaktest/temp/ambient_c'),
('e107', 'b101', 'c103', 'Part Temp C', 'Part body temperature.', 'plant/leaktest/temp/part_c'),
('e108', 'b101', 'c103', 'Comp Pressure PSI', 'Temperature compensated pressure.', 'plant/leaktest/temp/comp_pressure_psi'),
('e109', 'b101', 'c104', 'Gross Leak Alarm', 'Boolean alarm for gross leak.', 'plant/leaktest/gross/alarm'),
('e10a', 'b101', 'c104', 'Rapid Drop Flag', 'Rapid drop detection flag.', 'plant/leaktest/gross/rapid_drop'),
('e10b', 'b101', 'c105', 'Leak Rate SCCM', 'Computed leak rate.', 'plant/leaktest/fine/leak_rate_sccm'),
('e10c', 'b101', 'c105', 'Leak Limit SCCM', 'Configured leak acceptance limit.', 'plant/leaktest/fine/leak_limit_sccm'),
('e10d', 'b101', 'c106', 'Pass Fail', 'Final pass/fail bit.', 'plant/leaktest/result/pass_fail'),
('e10e', 'b101', 'c106', 'Result Code', 'Failure mode / result code.', 'plant/leaktest/result/code'),
('e201', 'b102', 'd101', 'Pressure PSI', 'Raw sensor pressure reading.', 'plant/pressure01/telemetry/pressure_psi'),
('e202', 'b102', 'd101', 'Pressure Filtered', 'Filtered pressure value.', 'plant/pressure01/telemetry/pressure_filtered_psi'),
('e203', 'b102', 'd101', 'Sensor Health', 'Health/quality indicator.', 'plant/pressure01/telemetry/health'),
('e204', 'b102', 'd102', 'Calibration Due', 'Calibration due flag.', 'plant/pressure01/diag/calibration_due'),
('e205', 'b102', 'd102', 'Drift PPM', 'Estimated sensor drift.', 'plant/pressure01/diag/drift_ppm');

INSERT INTO DATAFILES (ID, ASSET_ID, DATAPOINT_ID, Name, Description, Type, Link) VALUES
('f101', 'b101', 'c101', 'Hold Procedure', 'Work instruction for pressure hold.', 'pdf', 'docs/leaktest/hold_procedure.pdf'),
('f102', 'b101', 'c101', 'Hold Trend Template', 'CSV template for hold trend export.', 'csv', 'docs/leaktest/templates/hold_trend_template.csv'),
('f103', 'b101', 'c105', 'Leak Formula', 'Leak-rate calculation worksheet.', 'xlsx', 'docs/leaktest/calcs/leak_formula.xlsx'),
('f104', 'b101', 'c106', 'Result Report', 'Final pass/fail signed report.', 'pdf', 'docs/leaktest/reports/result_report.pdf'),
('f201', 'b102', 'd101', 'Sensor Datasheet', 'Pressure sensor technical datasheet.', 'pdf', 'docs/sensors/pressure01/datasheet.pdf'),
('f202', 'b102', 'd102', 'Calibration Cert', 'Latest calibration certificate.', 'pdf', 'docs/sensors/pressure01/calibration_cert.pdf');

COMMIT TRANSACTION;
