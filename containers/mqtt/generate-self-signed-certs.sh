#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CERT_DIR="${SCRIPT_DIR}/certs"
CN="${MQTT_CERT_CN:-192.168.1.50}"
DAYS="${MQTT_CERT_DAYS:-825}"

mkdir -p "${CERT_DIR}"

openssl req \
  -x509 \
  -nodes \
  -newkey rsa:4096 \
  -sha256 \
  -days "${DAYS}" \
  -subj "/CN=${CN}" \
  -keyout "${CERT_DIR}/server.key" \
  -out "${CERT_DIR}/server.crt"

cp "${CERT_DIR}/server.crt" "${CERT_DIR}/ca.crt"

chmod 600 "${CERT_DIR}/server.key"
chmod 644 "${CERT_DIR}/server.crt" "${CERT_DIR}/ca.crt"

echo "Generated certs in ${CERT_DIR}"
echo "  ca.crt"
echo "  server.crt"
echo "  server.key"
