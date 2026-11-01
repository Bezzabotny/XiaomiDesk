$ErrorActionPreference='Stop'
$compiler="$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$output=Join-Path $PSScriptRoot '../.build/TransferTests.exe'
$data=Join-Path $PSScriptRoot ('../.build/t-'+[Guid]::NewGuid().ToString('N').Substring(0,8))
$files=@('desktop\Core.cs','desktop\Transfers.cs','TransferTests.cs') | ForEach-Object {Join-Path $PSScriptRoot $_}
& $compiler /nologo /target:exe /codepage:65001 /r:System.Web.Extensions.dll /out:$output $files
if($LASTEXITCODE){throw 'Transfer test compilation failed'}
& $output $data
if($LASTEXITCODE){throw 'Transfer tests failed'}
