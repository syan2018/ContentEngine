# PowerShell Script to Run the FastAPI Service using the local .venv

$venvName = ".venv"
$projectRoot = $PSScriptRoot
if (-not $projectRoot) {
    $projectRoot = Get-Location
}

$venvPath = Join-Path -Path $projectRoot -ChildPath $venvName
$uvicornPath = Join-Path -Path $venvPath -ChildPath "Scripts\uvicorn.exe"

Write-Host "项目根目录: $projectRoot"
Write-Host "尝试使用虚拟环境中的 Uvicorn: $uvicornPath"

if (-not (Test-Path $uvicornPath)) {
    Write-Warning "警告: 在 $uvicornPath 未找到 Uvicorn。"
    Write-Warning "请确保您已经使用 setup_venv.ps1 (或其他相应脚本) 正确设置了虚拟环境并安装了依赖。"
    Write-Warning "如果虚拟环境已激活，您也可以直接尝试运行: uvicorn main:app --reload"
    # 尝试直接调用 uvicorn，以防它在 PATH 中（例如，如果 venv 已被激活）
    $uvicornPath = "uvicorn" 
    Write-Host "尝试使用系统 PATH 中的 uvicorn..."
}

Write-Host "正在启动 FastAPI 服务 (uvicorn main:app --reload)..."
Write-Host "服务将在 http://127.0.0.1:8000 运行。按 Ctrl+C 停止服务。"

# 执行 Uvicorn
& $uvicornPath main:app --reload

if ($LASTEXITCODE -ne 0) {
    Write-Error "错误: 启动服务失败。"
    Write-Host "请确保虚拟环境已激活，或者 '$uvicornPath' 是正确的路径并且可以执行。"
    Write-Host "您可以尝试手动激活虚拟环境: .\$venvName\Scripts\Activate.ps1"
    Write-Host "然后运行命令: uvicorn main:app --reload"
} 