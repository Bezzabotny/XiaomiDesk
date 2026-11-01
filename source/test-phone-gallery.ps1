param([Parameter(Mandatory=$true)][string]$Serial,[string]$SettingsFile=(Join-Path $env:LOCALAPPDATA 'XiaomiDesk/connection.json'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Security
$settings=Get-Content -Raw -LiteralPath $SettingsFile | ConvertFrom-Json
$adb=Join-Path (Split-Path $PSScriptRoot) 'tools/adb.exe'
$identity=(& $adb -s $Serial shell getprop ro.serialno).Trim()
if($settings.Identity -ne ('serial:'+$identity)){throw 'Phone differs from saved identity'}
$credential=[Text.Encoding]::UTF8.GetString([Security.Cryptography.ProtectedData]::Unprotect([Convert]::FromBase64String($settings.ProtectedToken),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser))
$id=[Guid]::NewGuid().ToString('N')
$workRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../.build'))
$directory=Join-Path $workRoot ('phone-gallery-'+$id)
New-Item -ItemType Directory -Path $directory | Out-Null
$remote='/sdcard/Android/data/app.xiaomidesk/cache/gallery/'+$id+'.zip'
try{
 $opened=& $adb -s $Serial shell am start -n app.xiaomidesk/.MainActivity --es token $credential --ei port $settings.BridgePort --es action ('gallery:0:'+$id)
 $credential=$null
 if($LASTEXITCODE){throw 'Helper did not open'}
 $ready=$false
 for($i=0;$i -lt 90;$i++){$state=& $adb -s $Serial shell ('test -f '+$remote+' && echo ready || true');if($state -match '^ready'){$ready=$true;break};Start-Sleep -Seconds 1}
 if(!$ready){throw 'No gallery export: confirm photo/video permission on phone'}
 $archive=Join-Path $directory 'page.zip'
 $pulled=& $adb -s $Serial pull $remote $archive
 if($LASTEXITCODE){throw 'Gallery download failed'}
 $compiler="$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
 $out=Join-Path $workRoot 'PhoneGalleryTest.exe'
 & $compiler /nologo /codepage:65001 /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /out:$out (Join-Path $PSScriptRoot 'desktop/GalleryArchive.cs') (Join-Path $PSScriptRoot 'desktop/MediaPolicy.cs') (Join-Path $PSScriptRoot 'PhoneGalleryTest.cs')
 if($LASTEXITCODE){throw 'Compilation failed'}
 & $out $archive $directory
 if($LASTEXITCODE){throw 'Gallery verification failed'}
}finally{
 $removed=& $adb -s $Serial shell ('rm -f -- '+$remote)
 Get-ChildItem -LiteralPath $directory -File | ForEach-Object {Remove-Item -LiteralPath $_.FullName}
 Remove-Item -LiteralPath $directory
}
