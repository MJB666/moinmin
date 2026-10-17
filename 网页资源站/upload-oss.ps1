# =====================================================================
#  upload-oss.ps1  —— 把本地安装包一键上传到阿里云 OSS / 腾讯云 COS
#
#  纯 PowerShell 实现（自签名 REST 请求），不需要安装 ossutil / coscmd。
#
#  首次使用：复制 oss.conf.example.json 为 oss.conf.json，填好自己的
#  AccessKey 等信息，之后每次只要：
#
#      .\upload-oss.ps1 -Path .\installers
#
#  单个文件：
#      .\upload-oss.ps1 -Path .\Motrix-Setup.exe -Key packages/Motrix-Setup.exe
#
#  只看不改（不真正上传，只打印将要上传的地址）：
#      .\upload-oss.ps1 -Path .\installers -DryRun
#
#  上传完会在屏幕打印可直接粘贴到后台的「下载直链」。
#  ⚠️ oss.conf.json 含密钥，千万别提交到 Git / 公开仓库！
# =====================================================================

[CmdletBinding()]
param(
  # 本地文件或文件夹（文件夹会递归上传）
  [Parameter(Mandatory = $true)][string]$Path,

  # 上传到 OSS 的目录前缀，如 packages
  [string]$Prefix = "packages",

  # 单个文件时自定义对象名（不填则用文件名）
  [string]$Key = "",

  # 对象访问权限：public-read 才允许直接外链下载；private 则需签名 URL
  [string]$Acl = "public-read",

  # 覆盖配置文件里的参数（一般不用填）
  [string]$Bucket,
  [string]$Endpoint,
  [string]$AccessKeyId,
  [string]$AccessKeySecret,

  # 只演练，不真正上传
  [switch]$DryRun
)

$ErrorActionPreference = "Stop"
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

# ---------------------------------------------------------------------
# 1. 读取配置
# ---------------------------------------------------------------------
$confFile = Join-Path $PSScriptRoot "oss.conf.json"
$conf = @{}
if (Test-Path $confFile) {
  $conf = (Get-Content -Raw -Encoding UTF8 $confFile | ConvertFrom-Json)
}

function Pick($cliVal, $confVal, $envVal) {
  if ($cliVal) { return $cliVal }
  if ($confVal) { return $confVal }
  if ($envVal) { return [Environment]::GetEnvironmentVariable($envVal) }
  return $null
}

if (-not $Bucket)          { $Bucket          = Pick $Bucket          $conf.bucket          "OSS_BUCKET" }
if (-not $Endpoint)        { $Endpoint        = Pick $Endpoint        $conf.endpoint        "OSS_ENDPOINT" }
if (-not $AccessKeyId)     { $AccessKeyId     = Pick $AccessKeyId     $conf.accessKeyId     "OSS_ACCESS_KEY_ID" }
if (-not $AccessKeySecret) { $AccessKeySecret = Pick $AccessKeySecret $conf.accessKeySecret "OSS_ACCESS_KEY_SECRET" }

$missing = @()
if (-not $Bucket)          { $missing += "bucket / OSS_BUCKET" }
if (-not $Endpoint)        { $missing += "endpoint / OSS_ENDPOINT" }
if (-not $AccessKeyId)     { $missing += "accessKeyId / OSS_ACCESS_KEY_ID" }
if (-not $AccessKeySecret) { $missing += "accessKeySecret / OSS_ACCESS_KEY_SECRET" }
if ($missing.Count) {
  Write-Host "`n缺少必要配置：" -ForegroundColor Red
  $missing | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
  Write-Host "`n请把它们写进 $confFile（参考 oss.conf.example.json），或用命令行参数传入。`n" -ForegroundColor Yellow
  exit 1
}

# 终端节点去掉协议前缀，统一成 host
$Endpoint = $Endpoint -replace '^https?://', '' -replace '/$', ''

# ---------------------------------------------------------------------
# 2. 扩展名 -> Content-Type
# ---------------------------------------------------------------------
$MIME = @{
  ".exe" = "application/octet-stream"; ".msi" = "application/x-msi"; ".zip" = "application/zip"
  ".7z"  = "application/x-7z-compressed"; ".rar" = "application/vnd.rar"; ".apk" = "application/vnd.android.package-archive"
  ".dmg" = "application/octet-stream"; ".pkg" = "application/octet-stream"; ".deb" = "application/vnd.debian.binary-package"
  ".rpm" = "application/x-rpm"; ".ipa" = "application/octet-stream"; ".tar" = "application/x-tar"
  ".gz"  = "application/gzip"; ".iso" = "application/octet-stream"; ".txt" = "text/plain; charset=utf-8"
  ".json"= "application/json; charset=utf-8"; ".pdf" = "application/pdf"
}

# ---------------------------------------------------------------------
# 3. 签名所需函数
# ---------------------------------------------------------------------
function Get-ContentMd5Base64([byte[]]$bytes) {
  $md5 = [System.Security.Cryptography.MD5]::Create()
  try { return [Convert]::ToBase64String($md5.ComputeHash($bytes)) } finally { $md5.Dispose() }
}

function Get-OssAuthorization {
  param(
    [string]$Verb, [string]$ContentMd5, [string]$ContentType,
    [string]$Date, [string]$CanonicalizedOssHeaders, [string]$Resource, [string]$Secret
  )
  $stringToSign = "$Verb`n$ContentMd5`n$ContentType`n$Date`n$CanonicalizedOssHeaders$Resource"
  $hmac = New-Object System.Security.Cryptography.HMACSHA1
  $hmac.Key = [System.Text.Encoding]::UTF8.GetBytes($Secret)
  try {
    $sig = $hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($stringToSign))
    return "OSS " + $AccessKeyId + ":" + [Convert]::ToBase64String($sig)
  } finally { $hmac.Dispose() }
}

# 对象 key 里每一段单独做 URL 编码（保留 / 分隔）
function Encode-ObjectKey([string]$k) {
  return (($k -split '/') | ForEach-Object { [System.Uri]::EscapeDataString($_) }) -join '/'
}

Add-Type -AssemblyName System.Net.Http
$http = New-Object System.Net.Http.HttpClient

function Upload-One([string]$localFile, [string]$objectKey) {
  $bytes = [System.IO.File]::ReadAllBytes($localFile)
  $sizeMb = [math]::Round($bytes.Length / 1MB, 2)

  $ext = [System.IO.Path]::GetExtension($localFile).ToLower()
  $contentType = if ($MIME.ContainsKey($ext)) { $MIME[$ext] } else { "application/octet-stream" }
  $contentMd5 = Get-ContentMd5Base64 $bytes
  $date = [DateTime]::UtcNow.ToString("r")

  # x-oss-* 头需要参与签名：小写、按名字排序、每行 "name:value\n"
  $canonicalOssHeaders = "x-oss-object-acl:$Acl`n"

  $resource = "/" + $Bucket + "/" + $objectKey
  $auth = Get-OssAuthorization `
    -Verb "PUT" -ContentMd5 $contentMd5 -ContentType $contentType `
    -Date $date -CanonicalizedOssHeaders $canonicalOssHeaders `
    -Resource $resource -Secret $AccessKeySecret

  $encodedKey = Encode-ObjectKey $objectKey
  $uri = "https://$Bucket.$Endpoint/$encodedKey"
  $publicUrl = $uri

  Write-Host ("  → {0}  ({1} MB)" -f $objectKey, $sizeMb) -ForegroundColor Cyan

  if ($DryRun) {
    Write-Host "     [DryRun] 将上传到: $publicUrl" -ForegroundColor DarkGray
    return $publicUrl
  }

  $req = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::Put, $uri)
  $req.Headers.TryAddWithoutValidation("Date", $date) | Out-Null
  $req.Headers.TryAddWithoutValidation("Authorization", $auth) | Out-Null
  $req.Headers.TryAddWithoutValidation("x-oss-object-acl", $Acl) | Out-Null
  $req.Headers.TryAddWithoutValidation("Content-MD5", $contentMd5) | Out-Null

  $content = [System.Net.Http.ByteArrayContent]::new($bytes)
  $content.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse($contentType)
  $req.Content = $content

  try {
    $resp = $http.SendAsync($req).GetAwaiter().GetResult()
    if (-not $resp.IsSuccessStatusCode) {
      $body = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
      throw ("HTTP {0} {1}`n{2}" -f [int]$resp.StatusCode, $resp.ReasonPhrase, $body)
    }
    Write-Host ("     ✅ 上传成功") -ForegroundColor Green
    return $publicUrl
  } catch {
    Write-Host ("     ❌ 上传失败: {0}" -f $_.Exception.Message) -ForegroundColor Red
    return $null
  } finally {
    $req.Dispose()
  }
}

# ---------------------------------------------------------------------
# 4. 组织待上传文件列表
# ---------------------------------------------------------------------
$Prefix = $Prefix.Trim('/')

if (-not (Test-Path $Path)) {
  Write-Host "找不到路径: $Path" -ForegroundColor Red
  exit 1
}

$jobs = @()
$item = Get-Item $Path
if ($item.PSIsContainer) {
  $files = Get-ChildItem -Path $item.FullName -Recurse -File
  foreach ($f in $files) {
    $rel = $f.FullName.Substring($item.FullName.Length).TrimStart('\', '/') -replace '\\', '/'
    $objectKey = (@($Prefix, $rel) | Where-Object { $_ }) -join '/'
    $jobs += [pscustomobject]@{ Local = $f.FullName; Key = $objectKey }
  }
} else {
  $objectKey = if ($Key) { $Key.TrimStart('/') } elseif ($Prefix) { "$Prefix/$($item.Name)" } else { $item.Name }
  $jobs += [pscustomobject]@{ Local = $item.FullName; Key = $objectKey }
}

if (-not $jobs.Count) { Write-Host "没有找到要上传的文件。" -ForegroundColor Yellow; exit 0 }

Write-Host ""
Write-Host ("准备上传 {0} 个文件到 {1} ({2})" -f $jobs.Count, $Bucket, $Endpoint) -ForegroundColor White
if ($DryRun) { Write-Host "（DryRun 模式，不会真正上传）" -ForegroundColor Yellow }
Write-Host ""

# ---------------------------------------------------------------------
# 5. 执行上传并输出下载直链
# ---------------------------------------------------------------------
$urls = @()
foreach ($j in $jobs) {
  $u = Upload-One -localFile $j.Local -objectKey $j.Key
  if ($u) { $urls += $u }
}

Write-Host ""
if ($urls.Count) {
  Write-Host "===== 下载直链（复制到后台「链接地址」即可）=====" -ForegroundColor Green
  $urls | ForEach-Object { Write-Host "  $_" }
  Write-Host ""
  Write-Host "提示：把直链填进后台时，类型记得选「安装包 / 可下载文件」。" -ForegroundColor DarkGray
  Write-Host "      如果下载报 AccessDenied，说明对象不是公共读 —— 检查 -Acl public-read 或 Bucket 读写权限。" -ForegroundColor DarkGray
} else {
  Write-Host "没有成功上传任何文件。" -ForegroundColor Red
}
Write-Host ""
