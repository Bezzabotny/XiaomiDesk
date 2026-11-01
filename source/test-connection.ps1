$ErrorActionPreference='Stop'
$compiler="$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$output=Join-Path $PSScriptRoot '../.build/ConnectionTests.exe'
$state=Join-Path $PSScriptRoot ('../.build/connection-tests-'+[Guid]::NewGuid().ToString('N'))
$files=@('desktop\Core.cs','desktop\Bridge.cs','desktop\ConnectionSettings.cs','desktop\WirelessConnection.cs','ConnectionTests.cs') | ForEach-Object {Join-Path $PSScriptRoot $_}
& $compiler /nologo /target:exe /codepage:65001 /r:System.Web.Extensions.dll /r:System.Security.dll /out:$output $files
if($LASTEXITCODE){throw 'Connection test compilation failed'}
& $output $state
if($LASTEXITCODE){throw 'Connection tests failed'}
