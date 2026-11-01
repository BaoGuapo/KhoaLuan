[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

if (-not (Get-Command flutter -ErrorAction SilentlyContinue)) {
    throw 'Không tìm thấy Flutter SDK trong PATH. Hãy cài Flutter rồi mở terminal mới.'
}

$projectRoot = Split-Path -Parent $PSScriptRoot
$backupRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("qlsv-mobile-source-" + [Guid]::NewGuid())
$filesToPreserve = @('pubspec.yaml', 'analysis_options.yaml', 'lib', 'test', 'README.md', 'tool')

New-Item -ItemType Directory -Path $backupRoot | Out-Null

try {
    foreach ($item in $filesToPreserve) {
        $source = Join-Path $projectRoot $item
        if (Test-Path -LiteralPath $source) {
            Copy-Item -LiteralPath $source -Destination $backupRoot -Recurse -Force
        }
    }

    Push-Location $projectRoot
    try {
        $platforms = if ($IsMacOS) { 'android,ios' } else { 'android' }
        flutter create --platforms=$platforms --project-name quan_ly_sinh_vien_app .
    }
    finally {
        Pop-Location
    }

    foreach ($item in $filesToPreserve) {
        $source = Join-Path $backupRoot $item
        if (Test-Path -LiteralPath $source) {
            Copy-Item -LiteralPath $source -Destination $projectRoot -Recurse -Force
        }
    }

    $mainManifestPath = Join-Path $projectRoot 'android/app/src/main/AndroidManifest.xml'
    $mainManifest = Get-Content -LiteralPath $mainManifestPath -Raw
    if ($mainManifest -notmatch 'android.permission.INTERNET') {
        $mainManifest = $mainManifest -replace '<application', "<uses-permission android:name=`"android.permission.INTERNET`" />`r`n    <application"
        Set-Content -LiteralPath $mainManifestPath -Value $mainManifest -Encoding utf8
    }

    $debugManifestPath = Join-Path $projectRoot 'android/app/src/debug/AndroidManifest.xml'
    @'
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    <uses-permission android:name="android.permission.INTERNET" />
    <application android:usesCleartextTraffic="true" />
</manifest>
'@ | Set-Content -LiteralPath $debugManifestPath -Encoding utf8

    $gradleKtsPath = Join-Path $projectRoot 'android/app/build.gradle.kts'
    if (Test-Path -LiteralPath $gradleKtsPath) {
        $gradleKts = Get-Content -LiteralPath $gradleKtsPath -Raw
        $gradleKts = $gradleKts -replace 'minSdk\s*=\s*flutter\.minSdkVersion', 'minSdk = 23'
        Set-Content -LiteralPath $gradleKtsPath -Value $gradleKts -Encoding utf8
    }

    $gradlePath = Join-Path $projectRoot 'android/app/build.gradle'
    if (Test-Path -LiteralPath $gradlePath) {
        $gradle = Get-Content -LiteralPath $gradlePath -Raw
        $gradle = $gradle -replace 'minSdkVersion\s+flutter\.minSdkVersion', 'minSdkVersion 23'
        Set-Content -LiteralPath $gradlePath -Value $gradle -Encoding utf8
    }

    Push-Location $projectRoot
    try {
        flutter pub get
        flutter format lib test
    }
    finally {
        Pop-Location
    }

    Write-Host 'Đã tạo platform Flutter và khôi phục source mobile.' -ForegroundColor Green
}
finally {
    if (Test-Path -LiteralPath $backupRoot) {
        Remove-Item -LiteralPath $backupRoot -Recurse -Force
    }
}
