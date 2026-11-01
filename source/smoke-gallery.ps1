$ErrorActionPreference='Stop'
$compiler="$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$sources=Get-ChildItem (Join-Path $PSScriptRoot 'desktop') -Filter '*.cs' | Where-Object {$_.Name -notin 'Program.cs','CoreTests.cs','ProcessFixture.cs','UISmoke.cs'} | ForEach-Object FullName
$out=Join-Path $PSScriptRoot '../.build/GallerySmoke.exe'
& $compiler /nologo /codepage:65001 /main:GallerySmoke /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Security.dll /r:System.Web.Extensions.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /out:$out $sources (Join-Path $PSScriptRoot 'GallerySmoke.cs')
if($LASTEXITCODE){throw 'Compilation failed'}
& $out (Join-Path $PSScriptRoot '../.build/gallery-ui-test')
if($LASTEXITCODE){throw 'Gallery UI failed'}
