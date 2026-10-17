# serve.ps1 —— 零依赖本地预览服务器（无需 Node / Python）
# 用法：右键「使用 PowerShell 运行」，或命令行执行：
#   powershell -ExecutionPolicy Bypass -File .\serve.ps1
# 然后浏览器打开 http://localhost:8899/

param(
  [int]$Port = 8899,
  [string]$Root = $PSScriptRoot
)

if (-not $Root) { $Root = (Get-Location).Path }
$Root = (Resolve-Path $Root).Path

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://localhost:$Port/")
try { $listener.Start() }
catch { Write-Host "启动失败：端口 $Port 可能被占用。换端口： -Port 8900" -ForegroundColor Red; exit 1 }

$mime = @{
  ".html" = "text/html; charset=utf-8"
  ".css"  = "text/css; charset=utf-8"
  ".js"   = "application/javascript; charset=utf-8"
  ".json" = "application/json; charset=utf-8"
  ".svg"  = "image/svg+xml"
  ".png"  = "image/png"
  ".jpg"  = "image/jpeg"
  ".ico"  = "image/x-icon"
  ".woff2"= "font/woff2"
}

Write-Host ""
Write-Host "  FreeHub 本地预览已启动" -ForegroundColor Green
Write-Host "  目录: $Root"
Write-Host "  前台: http://localhost:$Port/"
Write-Host "  后台: http://localhost:$Port/admin.html"
Write-Host "  按 Ctrl + C 停止" -ForegroundColor DarkGray
Write-Host ""

while ($listener.IsListening) {
  try {
    $ctx = $listener.GetContext()
    $rel = [System.Uri]::UnescapeDataString($ctx.Request.Url.LocalPath).TrimStart("/")
    if ([string]::IsNullOrWhiteSpace($rel)) { $rel = "index.html" }
    $file = Join-Path $Root ($rel -replace "/", "\")

    if ((Test-Path $file -PathType Leaf) -and ($file.StartsWith($Root))) {
      $bytes = [System.IO.File]::ReadAllBytes($file)
      $ext = [System.IO.Path]::GetExtension($file).ToLower()
      $ctx.Response.ContentType = if ($mime.ContainsKey($ext)) { $mime[$ext] } else { "application/octet-stream" }
      $ctx.Response.ContentLength64 = $bytes.Length
      $ctx.Response.Headers.Add("Cache-Control", "no-store, no-cache, must-revalidate")
      $ctx.Response.Headers.Add("Pragma", "no-cache")
      $ctx.Response.OutputStream.Write($bytes, 0, $bytes.Length)
    } else {
      $ctx.Response.StatusCode = 404
      $msg = [System.Text.Encoding]::UTF8.GetBytes("404 Not Found")
      $ctx.Response.OutputStream.Write($msg, 0, $msg.Length)
    }
    $ctx.Response.OutputStream.Close()
  } catch {
    if ($listener.IsListening) { Write-Host "请求出错: $_" -ForegroundColor DarkYellow }
  }
}
