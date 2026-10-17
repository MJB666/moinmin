# ============================================================
#  莫印泯 · 手机访问服务
#  在电脑上双击「一键开服.cmd」运行本脚本。
#  手机连上同一个 WiFi，用手机浏览器打开屏幕上显示的网址即可。
#  这样打开的好处：手机能正常保存数据，还能「添加到主屏幕」当 App 用。
# ============================================================

$ErrorActionPreference = "Continue"
$root = $PSScriptRoot
$port = 8898

if (-not (Test-Path (Join-Path $root "index.html"))) {
    Write-Host ""
    Write-Host "  找不到 index.html —— 请把本脚本放在「莫印泯手机版」文件夹里运行。" -ForegroundColor Red
    Write-Host ""
    exit 1
}

# ---------- 找到本机局域网地址 ----------
function Get-LanIPs {
    $out = @()
    try {
        Get-NetIPAddress -AddressFamily IPv4 -ErrorAction Stop | ForEach-Object {
            if ($_.IPAddress -eq "127.0.0.1") { return }
            if ($_.IPAddress -like "169.254.*") { return }
            $out += $_.IPAddress
        }
    } catch {
        try {
            [System.Net.Dns]::GetHostAddresses([System.Net.Dns]::GetHostName()) | ForEach-Object {
                if ($_.AddressFamily -eq "InterNetwork" -and $_.ToString() -ne "127.0.0.1") { $out += $_.ToString() }
            }
        } catch { }
    }
    return ($out | Select-Object -Unique)
}

# ---------- 绑定端口（优先通配，不行就逐个 IP 试）----------
$listener = New-Object System.Net.HttpListener
$prefixes = @("http://+:$port/")
$lanIPs = Get-LanIPs
foreach ($ip in $lanIPs) { $prefixes += "http://${ip}:$port/" }

$bound = $null
foreach ($p in $prefixes) {
    try {
        $t = New-Object System.Net.HttpListener
        $t.Prefixes.Add($p)
        $t.Start()
        $listener = $t
        $bound = $p
        break
    } catch {
        try { $t.Close() } catch { }
    }
}

if (-not $bound) {
    Write-Host ""
    Write-Host "  端口 $port 被占用了，或者没权限绑定。" -ForegroundColor Red
    Write-Host "  换个端口：在本文件第 10 行把 `$port 改成 8899 再试。" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

Write-Host ""
Write-Host "  ============================================================" -ForegroundColor Cyan
Write-Host "   莫印泯 · 手机访问服务已启动" -ForegroundColor Cyan
Write-Host "  ============================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "   用手机浏览器打开下面任意一个网址（手机要和这台电脑连同一个 WiFi）：" -ForegroundColor White
Write-Host ""

$printed = @()
foreach ($ip in $lanIPs) {
    $u = "http://${ip}:$port/"
    Write-Host ("      $u") -ForegroundColor Green
    $printed += $u
}
if ($printed.Count -eq 0) {
    Write-Host ("      http://127.0.0.1:$port/   （本机测试用）") -ForegroundColor Yellow
    Write-Host "      没检测到局域网地址，请确认电脑已连上 WiFi 或网线。" -ForegroundColor Yellow
}
Write-Host ""
Write-Host "   打开后建议：浏览器菜单 → 添加到主屏幕，以后点图标就能用。" -ForegroundColor White
Write-Host ""
Write-Host "   关掉这个黑窗口 = 停止服务。" -ForegroundColor DarkGray
Write-Host ""

# ---------- MIME ----------
$mime = @{
    ".html" = "text/html; charset=utf-8"
    ".htm"  = "text/html; charset=utf-8"
    ".css"  = "text/css; charset=utf-8"
    ".js"   = "application/javascript; charset=utf-8"
    ".json" = "application/json; charset=utf-8"
    ".png"  = "image/png"
    ".jpg"  = "image/jpeg"
    ".jpeg" = "image/jpeg"
    ".gif"  = "image/gif"
    ".webp" = "image/webp"
    ".svg"  = "image/svg+xml"
    ".ico"  = "image/x-icon"
    ".mp4"  = "video/mp4"
    ".webm" = "video/webm"
    ".mkv"  = "video/x-matroska"
    ".mov"  = "video/quicktime"
    ".mp3"  = "audio/mpeg"
    ".m4a"  = "audio/mp4"
    ".txt"  = "text/plain; charset=utf-8"
    ".md"   = "text/plain; charset=utf-8"
}

$rootFull = [System.IO.Path]::GetFullPath($root)

function Send-Bytes($res, $full, $mime, $method, $rangeHeader) {
    $fi = New-Object System.IO.FileInfo($full)
    $len = $fi.Length
    $ext = [System.IO.Path]::GetExtension($full).ToLower()
    $ct = if ($mime.ContainsKey($ext)) { $mime[$ext] } else { "application/octet-stream" }

    $start = 0; $end = $len - 1; $partial = $false
    if ($rangeHeader -and $rangeHeader -match 'bytes=(\d*)-(\d*)') {
        $partial = $true
        if ($matches[1] -ne "") { $start = [int64]$matches[1] }
        if ($matches[2] -ne "") { $end = [int64]$matches[2] }
        if ($end -ge $len) { $end = $len - 1 }
        if ($start -gt $end) { $start = 0 }
    }

    $res.ContentType = $ct
    $res.Headers["Accept-Ranges"] = "bytes"
    $res.Headers["Cache-Control"] = "public, max-age=300"

    if ($partial) {
        $res.StatusCode = 206
        $res.Headers["Content-Range"] = "bytes $start-$end/$len"
    }
    $count = $end - $start + 1
    $res.ContentLength64 = $count

    if ($method -eq "HEAD") { return }

    $fs = New-Object System.IO.FileStream($full, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
    try {
        $fs.Seek($start, [System.IO.SeekOrigin]::Begin) | Out-Null
        $buf = New-Object byte[] 524288
        $left = $count
        while ($left -gt 0) {
            $want = [Math]::Min($buf.Length, $left)
            $n = $fs.Read($buf, 0, $want)
            if ($n -le 0) { break }
            $res.OutputStream.Write($buf, 0, $n)
            $left -= $n
        }
    } finally {
        $fs.Close()
    }
}

Write-Host "   服务运行中…（按 Ctrl+C 也可以停）" -ForegroundColor DarkGray
Write-Host ""

while ($listener.IsListening) {
    $ctx = $null
    try { $ctx = $listener.GetContext() } catch { break }

    $req = $ctx.Request
    $res = $ctx.Response
    try {
        $rel = [System.Uri]::UnescapeDataString($req.Url.AbsolutePath).TrimStart('/')
        if ([string]::IsNullOrWhiteSpace($rel)) { $rel = "index.html" }
        $full = [System.IO.Path]::GetFullPath((Join-Path $rootFull ($rel -replace '/', '\')))

        if (-not $full.StartsWith($rootFull, [StringComparison]::OrdinalIgnoreCase)) {
            $res.StatusCode = 403
            $res.Close()
            continue
        }

        $isDir = $false
        try { $isDir = (Get-Item -LiteralPath $full -ErrorAction Stop).PSIsContainer } catch { }
        if ($isDir) { $full = Join-Path $full "index.html" }

        if (-not (Test-Path -LiteralPath $full -PathType Leaf)) {
            $res.StatusCode = 404
            $msg = [System.Text.Encoding]::UTF8.GetBytes("404 找不到这个文件")
            $res.ContentType = "text/plain; charset=utf-8"
            $res.OutputStream.Write($msg, 0, $msg.Length)
            $res.Close()
            continue
        }

        Send-Bytes $res $full $mime $req.HttpMethod $req.Headers["Range"]
    } catch { }
    try { $res.Close() } catch { }
}

try { $listener.Stop(); $listener.Close() } catch { }
Write-Host ""
Write-Host "  服务已停止。" -ForegroundColor DarkGray
