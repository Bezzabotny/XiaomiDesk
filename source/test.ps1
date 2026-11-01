$ErrorActionPreference = 'Stop'
$compiler = "$env:WINDIR/Microsoft.NET/Framework64/v4.0.30319/csc.exe"
$target = Join-Path $PSScriptRoot '../.build/CoreTests.exe'
$core = Join-Path $PSScriptRoot 'desktop\Core.cs'
$tests = Join-Path $PSScriptRoot 'desktop\CoreTests.cs'
$bridge = Join-Path $PSScriptRoot 'desktop\Bridge.cs'
$adb = Join-Path $PSScriptRoot 'desktop\Adb.cs'
$fixture = Join-Path $PSScriptRoot '..\.build\adb-fixture'
New-Item -ItemType Directory -Force -Path (Join-Path $fixture 'tools') | Out-Null
$fixtureExe=Join-Path $fixture 'tools\adb.exe'
& $compiler /nologo /target:exe /out:$fixtureExe (Join-Path $PSScriptRoot 'desktop\ProcessFixture.cs')
if ($LASTEXITCODE -ne 0) { throw 'Fixture compilation failed' }
& $compiler /nologo /target:exe /r:System.Web.Extensions.dll /out:$target $core $bridge $adb $tests
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
& $target $fixture
if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
