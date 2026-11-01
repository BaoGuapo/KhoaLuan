[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot

function Require-Command([string]$commandName, [string]$installHint) {
    if (-not (Get-Command $commandName -ErrorAction SilentlyContinue)) {
        throw "$commandName was not found. $installHint"
    }
}

Require-Command 'dotnet' 'Install the .NET SDK, then open a new terminal.'
Require-Command 'flutter' 'Install the Flutter SDK, add it to PATH, then open a new terminal.'

Push-Location $repositoryRoot
try {
    if (-not (Test-Path 'KhoaLuan.sln')) {
        dotnet new sln -n KhoaLuan
    }

    if (-not (Test-Path 'backend/QuanLySinhVien.Api/QuanLySinhVien.Api.csproj')) {
        dotnet new webapi --use-controllers -n QuanLySinhVien.Api -o backend/QuanLySinhVien.Api
        dotnet sln KhoaLuan.sln add backend/QuanLySinhVien.Api/QuanLySinhVien.Api.csproj
    }

    if (-not (Test-Path 'web/QuanLySinhVien.Web/QuanLySinhVien.Web.csproj')) {
        dotnet new mvc -n QuanLySinhVien.Web -o web/QuanLySinhVien.Web
        dotnet sln KhoaLuan.sln add web/QuanLySinhVien.Web/QuanLySinhVien.Web.csproj
    }

    if (-not (Test-Path 'mobile/quan_ly_sinh_vien_app/pubspec.yaml')) {
        flutter create mobile/quan_ly_sinh_vien_app
    }

    $exampleSettingsSource = Join-Path $repositoryRoot 'backend/appsettings.example.json'
    $exampleSettingsDestination = Join-Path $repositoryRoot 'backend/QuanLySinhVien.Api/appsettings.example.json'
    if (-not (Test-Path $exampleSettingsDestination)) {
        Copy-Item -LiteralPath $exampleSettingsSource -Destination $exampleSettingsDestination
    }

    Write-Host 'Project structure created successfully.' -ForegroundColor Green
}
finally {
    Pop-Location
}
