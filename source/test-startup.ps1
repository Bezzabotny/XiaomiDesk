$ErrorActionPreference='Stop'
$compiler="$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$output=Join-Path $PSScriptRoot '../.build/StartupTests.exe'
$files=@('desktop\Core.cs','desktop\StartupRegistration.cs','StartupTests.cs') | ForEach-Object {Join-Path $PSScriptRoot $_}
& $compiler /nologo /target:exe /codepage:65001 /out:$output $files
if($LASTEXITCODE){throw 'Startup tests compile failed'}
& $output
if($LASTEXITCODE){throw 'Startup tests failed'}
