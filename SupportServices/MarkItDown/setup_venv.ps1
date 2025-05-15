# PowerShell Script to Setup Python Virtual Environment and Install Dependencies

# 定义项目根目录下的虚拟环境名称
$venvName = ".venv"
# 定义需求文件名
$requirementsFile = "requirements.txt"

# 获取脚本所在的目录作为项目根目录
$projectRoot = $PSScriptRoot
if (-not $projectRoot) {
    $projectRoot = Get-Location
}

Write-Host "项目根目录: $projectRoot"
Write-Host "虚拟环境名称: $venvName"
Write-Host "需求文件: $requirementsFile"

# 检查 requirements.txt 是否存在
$requirementsPath = Join-Path -Path $projectRoot -ChildPath $requirementsFile
if (-not (Test-Path $requirementsPath)) {
    Write-Error "错误: 需求文件 '$requirementsFile' 在项目根目录 '$projectRoot' 未找到。"
    Write-Host "请确保 '$requirementsFile' 文件存在于脚本所在的目录中。"
    exit 1
}

# 检查 Python 是否安装 (尝试 python 和 python3)
$pythonExecutable = ""
if (Get-Command python -ErrorAction SilentlyContinue) {
    $pythonExecutable = "python"
} elseif (Get-Command python3 -ErrorAction SilentlyContinue) {
    $pythonExecutable = "python3"
}

if (-not $pythonExecutable) {
    Write-Error "错误: 未找到 Python。请确保 Python 已安装并已添加到 PATH 环境变量中。"
    Write-Host "您可以从 https://www.python.org/downloads/ 下载 Python。"
    exit 1
}
Write-Host "找到 Python 执行文件: $pythonExecutable"

# 虚拟环境的完整路径
$venvPath = Join-Path -Path $projectRoot -ChildPath $venvName

# 如果虚拟环境目录不存在，则创建它
if (-not (Test-Path $venvPath)) {
    Write-Host "正在创建虚拟环境 '$venvName'..."
    # 使用 Python 的 venv 模块创建虚拟环境
    & $pythonExecutable -m venv $venvPath
    if ($LASTEXITCODE -ne 0) {
        Write-Error "错误: 创建虚拟环境失败。"
        exit 1
    }
    Write-Host "虚拟环境创建成功: $venvPath"
} else {
    Write-Host "虚拟环境 '$venvName' 已存在。"
}

# 构建激活脚本的路径 (适用于 Windows PowerShell)
$activationScript = Join-Path -Path $venvPath -ChildPath "Scripts\Activate.ps1"

Write-Host "正在尝试激活虚拟环境并安装依赖..."

# 执行激活脚本并安装依赖
# 注意：直接在当前脚本中激活 venv 比较复杂，因为它会影响当前 Shell 会话。
# 通常，用户会手动激活，或者脚本会启动一个新的已激活的 Shell。
# 这里我们尝试在子作用域内调用 pip。

$pipPath = Join-Path -Path $venvPath -ChildPath "Scripts\pip.exe"

if (-not (Test-Path $pipPath)) {
    # 有时 pip 可能叫 pip3.exe
    $pipPath = Join-Path -Path $venvPath -ChildPath "Scripts\pip3.exe"
    if (-not (Test-Path $pipPath)) {
        Write-Error "错误: 在虚拟环境中未找到 pip ($pipPath)。虚拟环境可能已损坏或未正确创建。"
        exit 1
    }
}

Write-Host "使用位于 '$pipPath' 的 pip 安装依赖..."
& $pipPath install -r $requirementsPath

if ($LASTEXITCODE -ne 0) {
    Write-Error "错误: 依赖安装失败。请检查错误信息。"
    Write-Host "您可以尝试手动激活虚拟环境并运行 'pip install -r $requirementsFile'"
    Write-Host "手动激活命令: '$activationScript'"
    exit 1
}

Write-Host ""
Write-Host "---------------------------------------------------------------------"
Write-Host "Python 虚拟环境设置完成，并且依赖已成功安装！"
Write-Host ""
Write-Host "要在此 PowerShell 会话中手动激活虚拟环境，请运行:"
Write-Host "$activationScript"
Write-Host ""
Write-Host "激活后，您的提示符通常会显示 '($venvName)'。"
Write-Host "然后您就可以运行 'python main.py' 或 'uvicorn main:app --reload' 等命令了。"
Write-Host "---------------------------------------------------------------------"

# 脚本结束。虚拟环境不会在此脚本退出后保持激活状态。
# 用户需要按照上述说明手动激活它。 