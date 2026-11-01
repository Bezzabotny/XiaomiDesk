$ErrorActionPreference='Stop'
$compiler="$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$out=Join-Path $PSScriptRoot '../.build/ArchiveTests.exe'
& $compiler /nologo /codepage:65001 /r:System.Web.Extensions.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /out:$out (Join-Path $PSScriptRoot 'desktop/GalleryArchive.cs') (Join-Path $PSScriptRoot 'ArchiveTests.cs')
if($LASTEXITCODE){throw 'Compile failed'}
& $out
if($LASTEXITCODE){throw 'Gallery tests failed'}
