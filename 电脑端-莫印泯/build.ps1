# =====================================================================
#  build.ps1 —— 一键构建「莫印泯」桌面软件
#
#  用法：
#     powershell -ExecutionPolicy Bypass -File .\build.ps1
#     （或者直接双击同目录下的「一键构建.cmd」）
#
#  它会自动完成：
#     1. 在本文件夹里找一张图片当软件图标
#        （优先用 icon.png / 图标.png，否则找名字带 icon 的图，再不行取最新图片）
#     2. 把它转成多尺寸 app.ico（16/24/32/48/64/128/256）
#     3. 用 .NET 自带的 C# 编译器编译出「莫印泯.exe」（WPF 界面：视频背景 + 玻璃反光卡片）
#
#  背景视频：
#     把视频命名成 bg.mp4（或 bg.webm / 背景.mp4）放进本文件夹，程序会自动找到。
#     没放视频时用动态渐变背景，一样好看。
#
#  可调参数：
#     -Zoom 0.7       裁剪缩放。1.0 = 整张图居中裁方；0.7 = 放大到 70% 再裁（默认，主体更清楚）
#     -Round $false   去掉圆角（默认带圆角）
#
#  例：
#     .\build.ps1                      → 放大 + 圆角（默认）
#     .\build.ps1 -Zoom 1.0 -Round $false   → 整张图、方角
#
#  想换图标：把新图片命名成 icon.png 放进本文件夹，重跑本脚本即可。
# =====================================================================

param(
  # 裁剪缩放：1.0 = 整张图居中裁成正方形；0.7 = 放大到 70% 再裁（主体更大更清楚）
  [double]$Zoom = 0.7,
  # 圆角：默认加圆角，看起来像常见 App 图标
  [bool]$Round = $true
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
if (-not $root) { $root = (Get-Location).Path }
Set-Location $root

Add-Type -AssemblyName System.Drawing

$CSC = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $CSC)) { $CSC = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe" }
if (-not (Test-Path $CSC)) { Write-Host "找不到 C# 编译器 csc.exe" -ForegroundColor Red; exit 1 }

# 界面用的是 WPF（才能做到视频背景 + 玻璃反光），需要这几个程序集
$fwDir  = Split-Path $CSC -Parent
$wpfDir = Join-Path $fwDir "WPF"
foreach ($ref in @("$wpfDir\PresentationCore.dll", "$wpfDir\PresentationFramework.dll", "$wpfDir\WindowsBase.dll", "$fwDir\System.Xaml.dll")) {
  if (-not (Test-Path $ref)) {
    Write-Host "缺少 WPF 运行库：$ref" -ForegroundColor Red
    Write-Host "这台电脑需要 .NET Framework 4.6 以上（Win10/11 自带）。" -ForegroundColor Red
    exit 1
  }
}

Write-Host ""
Write-Host "===== 构建 莫印泯 =====" -ForegroundColor Cyan

# ---------------------------------------------------------------------
# 1. 找图片
# ---------------------------------------------------------------------
$imgFile = $null
$imgExt = @(".png", ".jpg", ".jpeg", ".bmp", ".gif")

# 第一优先：标准命名
$preferred = @("icon.png", "icon.jpg", "icon.jpeg", "图标.png", "图标.jpg", "logo.png", "莫印泯.png")
foreach ($p in $preferred) {
  $full = Join-Path $root $p
  if (Test-Path $full) { $imgFile = $full; break }
}

# 第二优先：名字里带 icon / 图标 / logo 的图片
if (-not $imgFile) {
  $imgFile = Get-ChildItem $root -File -ErrorAction SilentlyContinue |
    Where-Object {
      $imgExt -contains $_.Extension.ToLower() -and
      ($_.Name -like "*icon*" -or $_.Name -like "*图标*" -or $_.Name -like "*logo*" -or $_.Name -like "*莫印泯*")
    } | Sort-Object LastWriteTime -Descending |
    Select-Object -First 1 | ForEach-Object { $_.FullName }
}

# 第三优先：文件夹里最新的图片
if (-not $imgFile) {
  $imgFile = Get-ChildItem $root -File -ErrorAction SilentlyContinue |
    Where-Object { $imgExt -contains $_.Extension.ToLower() } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1 | ForEach-Object { $_.FullName }
}

# ---------------------------------------------------------------------
# 2. ICO 生成工具
# ---------------------------------------------------------------------
function Get-ResizedBitmap($src, [int]$size, [double]$zoomLevel, [bool]$useRound) {
  # 1) 先把源图居中裁成正方形并缩放到目标尺寸
  $tmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $g = [System.Drawing.Graphics]::FromImage($tmp)
  $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
  $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
  $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
  $g.Clear([System.Drawing.Color]::Transparent)

  $z = 1.0
  if ($zoomLevel -gt 0 -and $zoomLevel -le 1.0) { $z = $zoomLevel }
  $side = [int]([math]::Min($src.Width, $src.Height) * $z)
  if ($side -lt 1) { $side = 1 }
  $sx = [int](($src.Width - $side) / 2)
  $sy = [int](($src.Height - $side) / 2)
  $dest = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
  $g.DrawImage($src, $dest, $sx, $sy, $side, $side, [System.Drawing.GraphicsUnit]::Pixel)
  $g.Dispose()

  if (-not $useRound) { return , $tmp }

  # 2) 圆角：用 TextureBrush 填充圆角路径，边缘抗锯齿
  $out = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $g2 = [System.Drawing.Graphics]::FromImage($out)
  $g2.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $g2.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  $g2.Clear([System.Drawing.Color]::Transparent)

  $r = [int]($size * 0.22)
  if ($r -lt 1) { $r = 1 }
  $d = $r * 2
  $path = New-Object System.Drawing.Drawing2D.GraphicsPath
  $path.AddArc(0, 0, $d, $d, 180, 90)
  $path.AddArc(($size - $d), 0, $d, $d, 270, 90)
  $path.AddArc(($size - $d), ($size - $d), $d, $d, 0, 90)
  $path.AddArc(0, ($size - $d), $d, $d, 90, 90)
  $path.CloseFigure()

  $tb = New-Object System.Drawing.TextureBrush($tmp, [System.Drawing.Drawing2D.WrapMode]::Clamp)
  $g2.FillPath($tb, $path)
  $tb.Dispose(); $path.Dispose(); $g2.Dispose(); $tmp.Dispose()
  return , $out
}

function Get-DibBytes($bmp) {
  $size = $bmp.Width
  $rect = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
  $data = $bmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $stride = $data.Stride
  $raw = New-Object byte[] ($stride * $size)
  [System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $raw, 0, $raw.Length)
  $bmp.UnlockBits($data)

  $ms = New-Object System.IO.MemoryStream
  $bw = New-Object System.IO.BinaryWriter($ms)
  $bw.Write([UInt32]40)                 # biSize
  $bw.Write([Int32]$size)               # biWidth
  $bw.Write([Int32]($size * 2))         # biHeight (XOR + AND)
  $bw.Write([UInt16]1)                  # biPlanes
  $bw.Write([UInt16]32)                 # biBitCount
  $bw.Write([UInt32]0)                  # BI_RGB
  $bw.Write([UInt32]($size * $size * 4))
  $bw.Write([Int32]0); $bw.Write([Int32]0)
  $bw.Write([UInt32]0); $bw.Write([UInt32]0)
  for ($y = $size - 1; $y -ge 0; $y--) { $bw.Write($raw, $y * $stride, $size * 4) }
  $maskStride = [int]([math]::Ceiling($size / 32.0) * 4)
  $mask = New-Object byte[] ($maskStride * $size)
  $bw.Write($mask, 0, $mask.Length)
  $bw.Flush()
  $arr = $ms.ToArray()
  $bw.Close()
  return , $arr
}

function New-IcoFile($srcImage, [string]$outPath, [double]$zoomLevel, [bool]$useRound) {
  $sizes = @(16, 24, 32, 48, 64, 128, 256)
  $bmps = @()
  foreach ($s in $sizes) { $bmps += , (Get-ResizedBitmap $srcImage $s $zoomLevel $useRound) }

  $dibs = @()
  foreach ($b in $bmps) { $dibs += , (Get-DibBytes $b) }

  $ms = New-Object System.IO.MemoryStream
  $bw = New-Object System.IO.BinaryWriter($ms)
  $bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$bmps.Count)
  $offset = 6 + 16 * $bmps.Count
  for ($i = 0; $i -lt $bmps.Count; $i++) {
    $b = $bmps[$i]; $d = $dibs[$i]
    $w = if ($b.Width -ge 256) { 0 } else { [byte]$b.Width }
    $h = if ($b.Height -ge 256) { 0 } else { [byte]$b.Height }
    $bw.Write([byte]$w); $bw.Write([byte]$h)
    $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([UInt16]1); $bw.Write([UInt16]32)
    $bw.Write([UInt32]$d.Length)
    $bw.Write([UInt32]$offset)
    $offset += $d.Length
  }
  foreach ($d in $dibs) { $bw.Write($d) }
  $bw.Flush()
  [System.IO.File]::WriteAllBytes($outPath, $ms.ToArray())
  $bw.Close()
  foreach ($b in $bmps) { $b.Dispose() }
}

# 没有图片时，生成一个默认图标
function New-DefaultIcon([string]$outPath) {
  $s = 512
  $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias
  $g.Clear([System.Drawing.Color]::Transparent)

  $rect = New-Object System.Drawing.Rectangle(16, 16, ($s - 32), ($s - 32))
  $path = New-Object System.Drawing.Drawing2D.GraphicsPath
  $d = 110
  $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
  $path.AddArc(($rect.Right - $d), $rect.Y, $d, $d, 270, 90)
  $path.AddArc(($rect.Right - $d), ($rect.Bottom - $d), $d, $d, 0, 90)
  $path.AddArc($rect.X, ($rect.Bottom - $d), $d, $d, 90, 90)
  $path.CloseFigure()

  $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $rect,
    [System.Drawing.Color]::FromArgb(79, 110, 247),
    [System.Drawing.Color]::FromArgb(123, 92, 240),
    [System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal)
  $g.FillPath($brush, $path)
  $brush.Dispose()

  $font = New-Object System.Drawing.Font("Microsoft YaHei", 250, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
  $sf = New-Object System.Drawing.StringFormat
  $sf.Alignment = [System.Drawing.StringAlignment]::Center
  $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
  $g.DrawString("莫", $font, [System.Drawing.Brushes]::White, [System.Drawing.RectangleF]$rect, $sf)
  $font.Dispose(); $sf.Dispose(); $path.Dispose(); $g.Dispose()

  New-IcoFile $bmp $outPath 1.0 $false
  $bmp.Dispose()
}

# ---------------------------------------------------------------------
# 3. 生成 app.ico
# ---------------------------------------------------------------------
$icoPath = Join-Path $root "app.ico"
if ($imgFile) {
  Write-Host ("[1/3] 使用图片作为图标: {0}" -f (Split-Path $imgFile -Leaf)) -ForegroundColor Green
  try {
    $src = [System.Drawing.Image]::FromFile($imgFile)
    New-IcoFile $src $icoPath $Zoom $Round
    $src.Dispose()
  } catch {
    Write-Host ("      图片读取失败（{0}），改用默认图标" -f $_.Exception.Message) -ForegroundColor Yellow
    New-DefaultIcon $icoPath
  }
} else {
  Write-Host "[1/3] 没找到图片，使用默认图标（把图片存成 icon.png 放进本文件夹即可替换）" -ForegroundColor Yellow
  New-DefaultIcon $icoPath
}

# ---------------------------------------------------------------------
# 4. 写清单（高 DPI + Win10/11 兼容）
# ---------------------------------------------------------------------
Write-Host "[2/3] 生成应用清单 app.manifest" -ForegroundColor Green
$manifest = @'
<?xml version="1.0" encoding="utf-8"?>
<assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
  <assemblyIdentity version="1.0.0.0" name="MoYinMin.app" type="win32"/>
  <application xmlns="urn:schemas-microsoft-com:asm.v3">
    <windowsSettings>
      <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true</dpiAware>
    </windowsSettings>
  </application>
  <compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1">
    <application>
      <supportedOS Id="{e2011457-1546-43c5-a5fe-008deee3d3f0}"/>
      <supportedOS Id="{35138b9a-5d96-4fbd-8e2d-a2440225f93a}"/>
      <supportedOS Id="{4a2f28e3-53b9-4441-ba9c-d69d4a4a6e38}"/>
      <supportedOS Id="{1f676c76-80e1-4239-95bb-83d0f6d0da78}"/>
      <supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}"/>
    </application>
  </compatibility>
</assembly>
'@
[System.IO.File]::WriteAllText((Join-Path $root "app.manifest"), $manifest, (New-Object System.Text.UTF8Encoding($false)))

# ---------------------------------------------------------------------
# 5. 编译
# ---------------------------------------------------------------------
Write-Host "[3/3] 编译 莫印泯.exe ..." -ForegroundColor Green
$srcFile = Join-Path $root "莫印泯.cs"
$outExe = Join-Path $root "莫印泯.exe"
$manifestPath = Join-Path $root "app.manifest"

# 先删掉旧 exe，这样编译失败不会被误判成成功
if (Test-Path $outExe) { Remove-Item $outExe -Force }

$argsList = @(
  "/nologo"
  "/target:winexe"
  "/codepage:65001"
  "/optimize+"
  "/out:$outExe"
  "/win32icon:$icoPath"
  "/win32manifest:$manifestPath"
  "/r:$wpfDir\PresentationCore.dll"
  "/r:$wpfDir\PresentationFramework.dll"
  "/r:$wpfDir\WindowsBase.dll"
  "/r:$fwDir\System.Xaml.dll"
  "/r:System.Drawing.dll"
  "/r:System.Web.Extensions.dll"
  $srcFile
)

$output = & $CSC $argsList 2>&1
$output | ForEach-Object { Write-Host "  $_" }

if (Test-Path $outExe) {
  $f = Get-Item $outExe
  # 把图标也复制到数据目录，这样程序不管从哪儿启动都能找到自己的图标
  try {
    $dataDir = Join-Path $env:APPDATA "莫印泯"
    if (-not (Test-Path $dataDir)) { New-Item -ItemType Directory -Path $dataDir -Force | Out-Null }
    Copy-Item $icoPath (Join-Path $dataDir "app.ico") -Force
    Write-Host ("  图标副本: {0}" -f (Join-Path $dataDir "app.ico")) -ForegroundColor DarkGray
  } catch { }

  Write-Host ""
  Write-Host "构建成功！" -ForegroundColor Green
  Write-Host ("  程序: {0}" -f $f.FullName)
  Write-Host ("  大小: {0} KB" -f [math]::Round($f.Length / 1KB, 1))
  Write-Host ""
  Write-Host "  双击「莫印泯.exe」即可运行。" -ForegroundColor Cyan
  Write-Host "  换图标：把图片存成 icon.png 放进本文件夹，再跑一次 build.ps1。" -ForegroundColor DarkGray
  Write-Host "  换背景视频：把视频命名成 bg.mp4 放进本文件夹（或放进数据目录），点界面上的「视频背景」即可。" -ForegroundColor DarkGray
} else {
  Write-Host ""
  Write-Host "编译失败，请把上面的错误信息发给我。" -ForegroundColor Red
  exit 1
}
Write-Host ""
