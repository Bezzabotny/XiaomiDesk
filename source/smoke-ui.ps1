$ErrorActionPreference='Stop'
$compiler="$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$sources=Get-ChildItem (Join-Path $PSScriptRoot 'desktop') -Filter '*.cs' | Where-Object {$_.Name -notin 'Program.cs','CoreTests.cs','ProcessFixture.cs'} | ForEach-Object FullName
$output=Join-Path $PSScriptRoot '..\.build\UISmoke.exe'
$images=Join-Path $PSScriptRoot '..\.build\ui'
$fixture=Join-Path $PSScriptRoot '..\.build\adb-fixture'
New-Item -ItemType Directory -Force -Path (Join-Path $fixture 'tools') | Out-Null
$fixtureExe=Join-Path $fixture 'tools\adb.exe'
& $compiler /nologo /target:exe /out:$fixtureExe (Join-Path $PSScriptRoot 'desktop\ProcessFixture.cs')
if($LASTEXITCODE){throw 'Fixture compilation failed'}
& $compiler /nologo /target:exe /main:UISmoke /codepage:65001 /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.Security.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /out:$output $sources
if($LASTEXITCODE){throw 'UI smoke compilation failed'}
& $output $images $fixture
if($LASTEXITCODE){throw 'UI smoke failed'}
