#!/usr/bin/env bash
set -euo pipefail

/opt/mssql/bin/sqlservr &
SQL_PID=$!

echo "Waiting for SQL Server to accept connections..."
for i in {1..60}; do
  if /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -No -Q "SELECT 1" >/dev/null 2>&1; then
    break
  fi
  sleep 2
done

if ! /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -No -Q "SELECT 1" >/dev/null 2>&1; then
  echo "SQL Server did not become ready in time."
  kill "${SQL_PID}" || true
  exit 1
fi

echo "SQL Server is ready. Running init scripts..."
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -No -Q "IF DB_ID(N'${SQL_DATABASE}') IS NULL CREATE DATABASE [${SQL_DATABASE}];"
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -No -d "${SQL_DATABASE}" -v AppDbUser="${APP_DB_USER}" AppDbPassword="${APP_DB_PASSWORD}" -i /init/seed_leaktest_pressure01.sql

echo "Applying topic indirection v2 schema..."
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -No -d "${SQL_DATABASE}" -i /init/apply-topicscope-v2.sql

echo "Initialization complete."
wait "${SQL_PID}"
