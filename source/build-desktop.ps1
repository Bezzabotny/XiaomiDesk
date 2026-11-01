$ErrorActionPreference = 'Stop'
$compiler = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$files = Get-ChildItem (Join-Path $PSScriptRoot 'desktop') -Filter '*.cs' | Where-Object { $_.Name -notin 'CoreTests.cs','UISmoke.cs','ProcessFixture.cs' } | ForEach-Object FullName
New-Item -ItemType Directory -Force -Path (Join-Path (Split-Path $PSScriptRoot) 'dist') | Out-Null
$output = Join-Path (Split-Path $PSScriptRoot) 'dist/XiaomiDesk.exe'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.Security.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /out:$output $files
if ($LASTEXITCODE -ne 0) { throw 'Desktop build failed' }
