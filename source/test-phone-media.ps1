param([Parameter(Mandatory=$true)][string]$Serial)
$ErrorActionPreference='Stop'
$compiler="$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$out=Join-Path $PSScriptRoot '../.build/RealMediaProbe.exe'
$files=@('StreamHost.cs','ScreenOptions.cs','Adb.cs','Core.cs') | ForEach-Object {Join-Path $PSScriptRoot "desktop/$_"}
& $compiler /nologo /codepage:65001 /r:System.Windows.Forms.dll /r:System.Drawing.dll /out:$out $files (Join-Path $PSScriptRoot 'RealMediaProbe.cs')
if($LASTEXITCODE){throw 'Compilation failed'}
$recording=Join-Path $PSScriptRoot ('../.build/media-probe-'+[Guid]::NewGuid().ToString('N')+'.mkv')
try{& $out (Split-Path $PSScriptRoot) $Serial $recording;if($LASTEXITCODE){throw 'Phone media check failed'}}finally{if(Test-Path -LiteralPath $recording){Remove-Item -LiteralPath $recording}}
