param([string]$Jdk=$env:JAVA_HOME)
$ErrorActionPreference='Stop'
$classes=Join-Path $PSScriptRoot '..\.build\policy-tests'
New-Item -ItemType Directory -Force -Path $classes | Out-Null
& (Join-Path $Jdk 'bin\javac.exe') -encoding UTF-8 -d $classes (Join-Path $PSScriptRoot 'android\SessionPolicy.java') (Join-Path $PSScriptRoot 'android-tests\SessionPolicyTest.java')
if($LASTEXITCODE){throw 'Java tests compilation failed'}
& (Join-Path $Jdk 'bin\java.exe') -cp $classes app.xiaomidesk.SessionPolicyTest
if($LASTEXITCODE){throw 'Java tests failed'}
