#!/usr/bin/env bash
set -euo pipefail

BASE_DIR="${1:-$HOME/mqtt-project}"
ROOT="$BASE_DIR/docs"

echo "Target root: $ROOT"
mkdir -p "$ROOT/leaktest/templates"
mkdir -p "$ROOT/leaktest/calcs"
mkdir -p "$ROOT/leaktest/reports"
mkdir -p "$ROOT/sensors/pressure01"

cat > "$ROOT/leaktest/hold_procedure.pdf" <<'TXT'
Dummy PDF placeholder for Hold Procedure.
TXT

cat > "$ROOT/leaktest/templates/hold_trend_template.csv" <<'TXT'
time_s,pressure_psi,decay_rate_psi_s
0,120.0,0.00
1,119.9,0.10
TXT

cat > "$ROOT/leaktest/calcs/leak_formula.xlsx" <<'TXT'
Dummy XLSX placeholder for leak formula workbook.
TXT

cat > "$ROOT/leaktest/reports/result_report.pdf" <<'TXT'
Dummy PDF placeholder for Result Report.
TXT

cat > "$ROOT/sensors/pressure01/datasheet.pdf" <<'TXT'
Dummy PDF placeholder for Pressure Sensor Datasheet.
TXT

cat > "$ROOT/sensors/pressure01/calibration_cert.pdf" <<'TXT'
Dummy PDF placeholder for Calibration Certificate.
TXT

echo "Dummy docs created under: $ROOT"
