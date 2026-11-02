# 构建脚本 —— 椰子味的魈宝键盘精灵 v2.0
# 用法：双击 build.bat，或执行 powershell -ExecutionPolicy Bypass -File build.ps1
# 产物：dist\椰子味的魈宝键盘精灵.exe（单文件，素材全内嵌）

$ErrorActionPreference = "Stop"
$dir  = $PSScriptRoot
$name = "椰子味的魈宝键盘精灵"
$csc  = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$distDir = Join-Path $dir "dist"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  $name  v2.0  -  构建" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $csc)) {
    Write-Host "未找到 C# 编译器：$csc" -ForegroundColor Red
    Write-Host "请确认已安装 .NET Framework 4.0 或更高版本。" -ForegroundColor Red
    exit 1
}

# 停止正在运行的实例（否则文件被占用）
Get-Process -Name $name -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 800

New-Item -ItemType Directory -Force -Path $distDir | Out-Null

Write-Host "[1/3] 打包资源..." -ForegroundColor Yellow
$resArgs = @(
    "/win32icon:$dir\assets\app.ico",
    "/resource:$dir\assets\panel_bg.png,panel_bg.png",
    "/resource:$dir\assets\app.ico,app.ico",
    "/resource:$dir\assets\changelog.txt,changelog.txt",
    "/resource:$dir\assets\food\food.png,pet_food.png"
)

# 二维码
foreach ($q in @("qr_wechat.jpg", "qr_douyin.jpg")) {
    $qp = "$dir\assets\$q"
    if (Test-Path $qp) { $resArgs += "/resource:$qp,$q" }
}

# 桌宠表情（重命名为 pet_XX.png）
$petDir = "$dir\assets\pet"
if (Test-Path $petDir) {
    Get-ChildItem $petDir -Filter "*.png" | Sort-Object Name | ForEach-Object {
        $resArgs += "/resource:$($_.FullName),pet_$($_.Name)"
    }
}

Write-Host "  面板、图标、日志、食物、二维码、8 张表情" -ForegroundColor Gray

Write-Host "[2/3] 编译中..." -ForegroundColor Yellow
& $csc /nologo /target:winexe /optimize+ /platform:anycpu `
    "/out:$distDir\$name.exe" `
    @resArgs `
    /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll `
    "$dir\src\KeyStateTray.cs"

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "构建失败" -ForegroundColor Red
    exit 1
}

Write-Host "[3/3] 校验..." -ForegroundColor Yellow
$exe = Get-Item "$distDir\$name.exe"
$vi = $exe.VersionInfo

# 脱敏检查
$bytes = [System.IO.File]::ReadAllBytes($exe.FullName)
$dirty = $false
foreach ($enc in @([System.Text.Encoding]::UTF8, [System.Text.Encoding]::Unicode)) {
    if ($enc.GetString($bytes).Contains([char]0x738B + [char]0x4E4B + [char]0x5F66)) { $dirty = $true }
}

Write-Host "  版本: $($vi.FileVersion)"
Write-Host "  公司: [$(if($vi.CompanyName){$vi.CompanyName}else{'空'})]"
Write-Host "  含作者姓名: $(if($dirty){'是'}else{'否'})"

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "  构建成功" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ("  输出：dist\{0}" -f $exe.Name)
Write-Host ("  大小：{0:N0} 字节" -f $exe.Length)
Write-Host "============================================" -ForegroundColor Green
