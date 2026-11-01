param([string]$Archive)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$cache=Join-Path $root '.build'
New-Item -ItemType Directory -Force -Path $cache | Out-Null
$zip=Join-Path $cache 'scrcpy-win64-v5.0.1.zip'
$expected='B12A2C4EE8BE317422451FC7DCF8EE20A71B5EA7EF9AD73DDD825A25316227A5'
if($Archive){$zip=(Resolve-Path -LiteralPath $Archive).Path}else{Invoke-WebRequest -Uri 'https://github.com/Genymobile/scrcpy/releases/download/v5.0.1/scrcpy-win64-v5.0.1.zip' -OutFile $zip}
if((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash -ne $expected){throw 'scrcpy checksum mismatch'}
$unpacked=Join-Path $cache ('tools-'+[Guid]::NewGuid().ToString('N'))
Expand-Archive -LiteralPath $zip -DestinationPath $unpacked
$tools=Join-Path $root 'tools'
New-Item -ItemType Directory -Force -Path $tools | Out-Null
Copy-Item -Path (Join-Path $unpacked 'scrcpy-win64-v5.0.1/*') -Destination $tools -Recurse -Force
Write-Output 'Verified scrcpy 5.0.1 dependency installed in tools/'
