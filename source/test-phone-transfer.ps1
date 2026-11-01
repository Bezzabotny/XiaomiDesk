param([Parameter(Mandatory=$true)][string]$Serial,[Parameter(Mandatory=$true)][string]$Identity)
$ErrorActionPreference='Stop'
$compiler="$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$output=Join-Path $PSScriptRoot '../.build/PhoneTransferTest.exe'
$data=Join-Path $PSScriptRoot ('../.build/phone-transfer-'+[Guid]::NewGuid().ToString('N'))
$files=@('desktop\Core.cs','desktop\Adb.cs','desktop\Transfers.cs','desktop\AdbTransfers.cs','PhoneTransferTest.cs') | ForEach-Object {Join-Path $PSScriptRoot $_}
& $compiler /nologo /target:exe /codepage:65001 /r:System.Web.Extensions.dll /out:$output $files
if($LASTEXITCODE){throw 'Phone transfer test compilation failed'}
& $output (Split-Path $PSScriptRoot) $Serial $Identity $data
if($LASTEXITCODE){throw 'Phone transfer test failed'}
