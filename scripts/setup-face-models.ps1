$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$modelDirectory = Join-Path $repositoryRoot 'backend/QuanLySinhVien.Api/Models/Face'
New-Item -ItemType Directory -Path $modelDirectory -Force | Out-Null
$models = @(
    @{ Name = 'det_2.5g.onnx'; Url = 'https://github.com/yakhyo/face-reidentification/releases/download/v0.0.1/det_2.5g.onnx'; Hash = '041f73f47371333d1d17a6fee6c8ab4e6aecabefe398ff32cca4e2d5eaee0af9' },
    @{ Name = 'mobilenetv2_mcp.onnx'; Url = 'https://github.com/yakhyo/face-recognition/releases/download/v0.0.1/mobilenetv2_mcp.onnx'; Hash = '38b148284dd48cc898d5d4453104252fbdcbacc105fe3f0b80e78954d9d20d89' },
    @{ Name = 'MiniFASNetV2.onnx'; Url = 'https://github.com/yakhyo/face-anti-spoofing/releases/download/weights/MiniFASNetV2.onnx'; Hash = 'b32929adc2d9c34b9486f8c4c7bc97c1b69bc0ea9befefc380e4faae4e463907' }
)
foreach ($model in $models) {
    $target = Join-Path $modelDirectory $model.Name
    if (-not (Test-Path -LiteralPath $target)) { Invoke-WebRequest -Uri $model.Url -OutFile $target }
    $actualHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
    if ($actualHash -ne $model.Hash) { throw "Model checksum mismatch: $($model.Name). Review the file before replacing it." }
    Write-Output "Verified $($model.Name)"
}
