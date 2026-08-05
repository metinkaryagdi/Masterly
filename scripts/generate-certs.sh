#!/usr/bin/env bash
set -e

CERT_DIR="docker/certs"
mkdir -p "$CERT_DIR"

CERT_PATH="$CERT_DIR/fullchain.pem"
KEY_PATH="$CERT_DIR/privkey.pem"

if [ -f "$CERT_PATH" ] && [ -f "$KEY_PATH" ]; then
    echo "Certificates already exist in $CERT_DIR"
    exit 0
fi

echo "Generating self-signed SSL certificates in $CERT_DIR..."
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout "$KEY_PATH" \
    -out "$CERT_PATH" \
    -subj "/CN=localhost/O=CodeCraftNet"

echo "Certificates successfully generated at:"
echo " - $CERT_PATH"
echo " - $KEY_PATH"
