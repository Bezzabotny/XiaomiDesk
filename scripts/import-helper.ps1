param([Parameter(Mandatory=$true)][string]$Apk)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
# Reuse only the exact helper artifact verified on a real phone for0.4.
# This operation reads no key or password and never performs signing.
$expected='52428A93451107F2D15D6B1F47C084465A1298D5902A5D5A0B4FBF9605385082'
if((Get-FileHash -LiteralPath $Apk -Algorithm SHA256).Hash -ne $expected){throw 'APK is not the verified XiaomiDesk0.4 helper'}
$phone=Join-Path $root 'dist/phone'
New-Item -ItemType Directory -Force -Path $phone | Out-Null
Copy-Item -LiteralPath $Apk -Destination (Join-Path $phone 'XiaomiDesk.apk') -Force
@{signing='verified-existing-release';helperVersion='0.4';sha256=$expected.ToLowerInvariant();version=(Get-Content -Raw (Join-Path $root 'VERSION')).Trim()} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $phone 'build-info.json') -Encoding utf8
Write-Output 'Imported verified existing helper0.4; signing credentials were not accessed'
