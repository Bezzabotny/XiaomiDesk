param([switch]$Development)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$version=(Get-Content -Raw (Join-Path $root 'VERSION')).Trim()
if($version -notmatch '^\d+\.\d+\.\d+(-[a-z0-9.]+)?$'){throw 'Invalid version'}
$dist=Join-Path $root 'dist'
$info=Join-Path $dist 'phone/build-info.json'
if(!(Test-Path -LiteralPath $info)){throw 'Build Android helper first (missing signing metadata)'}
$buildInfo=Get-Content -Raw -LiteralPath $info | ConvertFrom-Json
if(!$Development -and $buildInfo.signing -notin @('release','verified-existing-release')){throw 'Development APK cannot be packaged as a release; build with -Release, import a verified existing helper, or pass -Development'}
if($buildInfo.signing -eq 'verified-existing-release' -and (Get-FileHash -LiteralPath (Join-Path $dist 'phone/XiaomiDesk.apk') -Algorithm SHA256).Hash.ToLowerInvariant() -ne $buildInfo.sha256){throw 'Imported helper checksum differs from verified metadata'}
$suffix=if($Development){'-development'}else{''}
$staging=Join-Path $root ('.build/package-'+[Guid]::NewGuid().ToString('N')+'/XiaomiDesk')
New-Item -ItemType Directory -Force -Path $staging,(Join-Path $staging 'tools'),(Join-Path $staging 'phone'),(Join-Path $staging 'docs') | Out-Null
foreach($name in @('XiaomiDesk.exe','phone/XiaomiDesk.apk','phone/build-info.json')){Copy-Item -LiteralPath (Join-Path $dist $name) -Destination (Join-Path $staging $name)}
$requiredTools=@('adb.exe','AdbWinApi.dll','AdbWinUsbApi.dll','scrcpy.exe','scrcpy-server','SDL3.dll','avcodec-63.dll','avformat-63.dll','avutil-61.dll','swresample-7.dll','libusb-1.0.dll','LICENSE.txt')
foreach($name in $requiredTools){$file=Join-Path $root "tools/$name";if(!(Test-Path -LiteralPath $file)){throw "Missing dependency $name. Run scripts/fetch-tools.ps1"};Copy-Item -LiteralPath $file -Destination (Join-Path $staging 'tools')}
foreach($name in @('README.md','LICENSE','THIRD-PARTY.md','CHANGELOG.md','XiaomiDesk.exe.config')){Copy-Item -LiteralPath (Join-Path $root $name) -Destination $staging}
foreach($name in @('WIFI.md','TROUBLESHOOTING.md')){Copy-Item -LiteralPath (Join-Path $root "docs/$name") -Destination (Join-Path $staging 'docs')}
$licenseTarget=Join-Path $staging 'third-party-licenses'
New-Item -ItemType Directory -Force -Path $licenseTarget | Out-Null
foreach($name in @('FFmpeg-LGPL-2.1.txt','libusb-LGPL-2.1.txt','SDL-zlib.txt')){Copy-Item -LiteralPath (Join-Path $root "third-party-licenses/$name") -Destination $licenseTarget}
if($Development){'DEVELOPMENT BUILD: APK uses a development key; do not publish or install over an existing release helper.' | Set-Content -LiteralPath (Join-Path $staging 'DEVELOPMENT-BUILD.txt')}
$archive=Join-Path $root ".build/XiaomiDesk-$version$suffix-Windows11-x64.zip"
Compress-Archive -LiteralPath $staging -DestinationPath $archive -CompressionLevel Optimal -Force
$hash=(Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $([IO.Path]::GetFileName($archive))" | Set-Content -LiteralPath ($archive+'.sha256') -Encoding ascii
Write-Output $archive
