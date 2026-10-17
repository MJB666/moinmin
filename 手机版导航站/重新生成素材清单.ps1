# ============================================================
#  重新生成素材清单 media.js
#  往「图片」或「视频」文件夹里加了新东西之后，双击「重新生成素材清单.cmd」跑一下。
# ============================================================

$ErrorActionPreference = "Continue"
$root = $PSScriptRoot
$out  = Join-Path $root "media.js"

$imgDir = Join-Path $root "图片"
$vidDir = Join-Path $root "视频"

$imgExt = @(".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp")
$vidExt = @(".mp4", ".webm", ".mov", ".m4v", ".mkv")

function Get-Files($dir, $exts) {
    if (-not (Test-Path -LiteralPath $dir)) { return @() }
    return (Get-ChildItem -LiteralPath $dir -File | Where-Object { $exts -contains $_.Extension.ToLower() } | Sort-Object Name)
}

function Clean-Title($name) {
    $t = $name -replace '^\d+\s*[-_\.]?\s*', ''
    if ([string]::IsNullOrWhiteSpace($t)) { $t = $name }
    return $t
}

# JS 字符串转义
function Js-Str($s) {
    $s = [string]$s
    $s = $s.Replace('\', '\\').Replace('"', '\"').Replace("`r", ' ').Replace("`n", ' ')
    return $s
}

$imgs = Get-Files $imgDir $imgExt
$vids = Get-Files $vidDir $vidExt

$L = @()
$L += "/* 素材清单 —— 由「重新生成素材清单.cmd」自动生成 */"
$L += "/* 往「图片」「视频」文件夹里加东西后，重新跑一次就会出现在选择列表里 */"
$L += "window.BUNDLED = {"
$L += "  images: ["

for ($i = 0; $i -lt $imgs.Count; $i++) {
    $f = $imgs[$i]
    $rel = "图片/" + $f.Name
    $title = Clean-Title $f.Name
    $comma = if ($i -lt $imgs.Count - 1) { "," } else { "" }
    $L += ('    { src: "' + (Js-Str $rel) + '", title: "' + (Js-Str $title) + '" }' + $comma)
}

$L += "  ],"
$L += "  videos: ["

for ($i = 0; $i -lt $vids.Count; $i++) {
    $f = $vids[$i]
    $rel = "视频/" + $f.Name
    $title = Clean-Title $f.Name
    $size = "" + [math]::Round($f.Length / 1MB, 0) + " MB"
    $comma = if ($i -lt $vids.Count - 1) { "," } else { "" }
    $L += ('    { src: "' + (Js-Str $rel) + '", title: "' + (Js-Str $title) + '", size: "' + $size + '" }' + $comma)
}

$L += "  ]"
$L += "};"
$L += ""

$text = ($L -join "`r`n")
[System.IO.File]::WriteAllText($out, $text, (New-Object System.Text.UTF8Encoding($false)))

Write-Host ""
Write-Host "  已重新生成 media.js" -ForegroundColor Green
Write-Host ("    图片 " + $imgs.Count + " 个") -ForegroundColor Gray
foreach ($f in $imgs) { Write-Host ("      " + $f.Name) -ForegroundColor DarkGray }
Write-Host ("    视频 " + $vids.Count + " 个") -ForegroundColor Gray
foreach ($f in $vids) { Write-Host ("      " + $f.Name + "   " + [math]::Round($f.Length / 1MB, 0) + " MB") -ForegroundColor DarkGray }
Write-Host ""
Write-Host "  回到手机页面刷新一下就能看到新素材。" -ForegroundColor White
Write-Host ""
