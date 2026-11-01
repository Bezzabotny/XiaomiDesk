param([string]$Jdk=$env:JAVA_HOME)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
New-Item -ItemType Directory -Force -Path (Join-Path $root '.build') | Out-Null
foreach($test in @('test.ps1','test-connection.ps1','test-screen.ps1','test-transfers.ps1','test-startup.ps1','test-media.ps1','test-gallery.ps1','smoke-ui.ps1','smoke-gallery.ps1')){
 & (Join-Path $root "source/$test")
 if($LASTEXITCODE){throw "Failed: $test"}
}
if($Jdk){& (Join-Path $root 'source/test-android.ps1') -Jdk $Jdk;if($LASTEXITCODE){throw 'Android policy tests failed'}}
