$ErrorActionPreference='Stop'
$compiler="$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$out=Join-Path $PSScriptRoot '../.build/MediaTests.exe'
$policy=Join-Path $PSScriptRoot 'desktop/MediaPolicy.cs'
& $compiler /nologo /out:$out $policy (Join-Path $PSScriptRoot 'MediaTests.cs')
if($LASTEXITCODE){throw 'Compile failed'}
& $out
if($LASTEXITCODE){throw 'Media tests failed'}
