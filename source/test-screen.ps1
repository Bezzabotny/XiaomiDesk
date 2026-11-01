$ErrorActionPreference='Stop'
$compiler="$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$output=Join-Path $PSScriptRoot '../.build/ScreenTests.exe'
$files=@('desktop\Core.cs','desktop\ScreenOptions.cs','ScreenTests.cs') | ForEach-Object {Join-Path $PSScriptRoot $_}
& $compiler /nologo /target:exe /codepage:65001 /out:$output $files
if($LASTEXITCODE){throw 'Screen tests compile failed'}
& $output
if($LASTEXITCODE){throw 'Screen tests failed'}
