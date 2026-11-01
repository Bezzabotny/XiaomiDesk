$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$dist=Join-Path $root 'dist'
$fakeLog=Join-Path $dist 'startup-error.txt'
$fakeKey=Join-Path $dist 'test-fixture.p12'
if((Test-Path -LiteralPath $fakeLog) -or (Test-Path -LiteralPath $fakeKey)){throw 'Test fixture paths already exist'}
try{
 'FAKE TEST PATH, NO USER DATA' | Set-Content -LiteralPath $fakeLog
 'FAKE TEST DATA, NOT A KEY' | Set-Content -LiteralPath $fakeKey
 $output=& (Join-Path $PSScriptRoot 'package.ps1') -Development
 Add-Type -AssemblyName System.IO.Compression.FileSystem
 $zip=[IO.Compression.ZipFile]::OpenRead([string]($output | Select-Object -Last 1))
 try{if($zip.Entries.FullName | Where-Object {$_ -match '(startup-error|\.p12$)'}){throw 'Private/stale dist file leaked into archive'};if(!($zip.Entries.FullName | Where-Object {$_ -match 'XiaomiDesk.exe$'})){throw 'Missing executable'}}finally{$zip.Dispose()}
 Write-Output 'PASS packaging excludes stale logs and signing key fixtures'
}finally{foreach($file in @($fakeLog,$fakeKey)){if(Test-Path -LiteralPath $file){Remove-Item -LiteralPath $file}}}
