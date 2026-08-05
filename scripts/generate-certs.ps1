# Powershell script to generate self-signed SSL certificates for local production testing
$certDir = Join-Path (Get-Location) "docker\certs"
if (-not (Test-Path $certDir)) {
    New-Item -ItemType Directory -Path $certDir -Force | Out-Null
}

$certPath = Join-Path $certDir "fullchain.pem"
$keyPath = Join-Path $certDir "privkey.pem"

if ((Test-Path $certPath) -and (Test-Path $keyPath)) {
    Write-Host "Certificates already exist in $certDir"
    exit 0
}

Write-Host "Generating self-signed SSL certificates in $certDir..."
openssl req -x509 -nodes -days 365 -newkey rsa:2048 `
    -keyout $keyPath `
    -out $certPath `
    -subj "/CN=localhost/O=CodeCraftNet"

Write-Host "Certificates successfully generated at:"
Write-Host " - $certPath"
Write-Host " - $keyPath"
