param([string]$Jdk=$env:JAVA_HOME,[string]$BuildTools='', [string]$AndroidJar='', [string]$SigningKey='', [string]$KeyAlias='xiaomidesk', [switch]$Release)
$ErrorActionPreference='Stop'
if(!$Jdk -or !$BuildTools -or !$AndroidJar){throw 'Pass JDK17+, BuildTools and AndroidJar (API35) paths.'}
if($Release -and (!$SigningKey -or !$env:XIAOMIDESK_SIGNING_PASSWORD)){throw 'Release requires -SigningKey and XIAOMIDESK_SIGNING_PASSWORD environment variable.'}
$build=Join-Path $PSScriptRoot ('../.build/android-build/'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $build,(Join-Path $build 'classes'),(Join-Path $build 'dex') | Out-Null
$build=(Resolve-Path $build).Path
$jar=(Resolve-Path $AndroidJar).Path
$bt=(Resolve-Path $BuildTools).Path
$java=Join-Path $Jdk 'bin\java.exe'
$classes=Join-Path $build 'classes'
$sources=Get-ChildItem (Join-Path $PSScriptRoot 'android') -Filter '*.java' | ForEach-Object FullName
& (Join-Path $Jdk 'bin\javac.exe') -encoding UTF-8 --release 8 -classpath $jar -d $classes $sources
if($LASTEXITCODE){throw 'javac failed'}
$inputJar=Join-Path $build 'classes.jar'
Add-Type -AssemblyName System.IO.Compression.FileSystem
if(Test-Path $inputJar){Remove-Item -LiteralPath $inputJar}
[IO.Compression.ZipFile]::CreateFromDirectory($classes,$inputJar)
& $java -cp (Join-Path $bt 'lib\d8.jar') com.android.tools.r8.D8 --lib $jar --min-api 26 --output (Join-Path $build 'dex') $inputJar
if($LASTEXITCODE){throw 'd8 failed'}
$unsigned=Join-Path $build 'unsigned.apk'
& (Join-Path $bt 'aapt2.exe') link -o $unsigned --manifest (Join-Path $PSScriptRoot 'android\AndroidManifest.xml') -I $jar --min-sdk-version 26 --target-sdk-version 35
if($LASTEXITCODE){throw 'aapt2 failed'}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=[IO.Compression.ZipFile]::Open($unsigned,[IO.Compression.ZipArchiveMode]::Update)
try{[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,(Join-Path $build 'dex\classes.dex'),'classes.dex') | Out-Null}finally{$zip.Dispose()}
$aligned=Join-Path $build 'aligned.apk'
& (Join-Path $bt 'zipalign.exe') -f -p 4 $unsigned $aligned
if($LASTEXITCODE){throw 'zipalign failed'}
$key=$SigningKey
$passwordEnv='XIAOMIDESK_SIGNING_PASSWORD'
if(!$key){
 $key=Join-Path $PSScriptRoot '../.build/development-only.p12'
 $passwordEnv='XIAOMIDESK_DEV_PASSWORD'
 $env:XIAOMIDESK_DEV_PASSWORD='android'
 if(!(Test-Path -LiteralPath $key)){& (Join-Path $Jdk 'bin/keytool.exe') -genkeypair -keystore $key -storepass:env XIAOMIDESK_DEV_PASSWORD -keypass:env XIAOMIDESK_DEV_PASSWORD -alias $KeyAlias -dname 'CN=XiaomiDesk Development Only' -keyalg RSA -keysize 3072 -validity 3650 -storetype PKCS12;if($LASTEXITCODE){throw 'Development key generation failed'}}
 Write-Warning 'Development APK: do not publish as a release or use to update a release installation.'
}elseif(!$env:XIAOMIDESK_SIGNING_PASSWORD){throw 'Set XIAOMIDESK_SIGNING_PASSWORD for the supplied key'}
$phone=Join-Path (Split-Path $PSScriptRoot) 'dist/phone'
New-Item -ItemType Directory -Force -Path $phone | Out-Null
$apk=Join-Path $phone 'XiaomiDesk.apk'
& $java -jar (Join-Path $bt 'lib/apksigner.jar') sign --ks $key --ks-key-alias $KeyAlias --ks-pass ('env:'+$passwordEnv) --key-pass ('env:'+$passwordEnv) --out $apk $aligned
if($LASTEXITCODE){throw 'Signing failed'}
& $java -jar (Join-Path $bt 'lib/apksigner.jar') verify --verbose $apk
if($LASTEXITCODE){throw 'Signature verification failed'}
$signingMode=if($Release){'release'}else{'development'}
@{ signing=$signingMode; version=(Get-Content -Raw (Join-Path (Split-Path $PSScriptRoot) 'VERSION')).Trim() } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $phone 'build-info.json') -Encoding utf8