# FreeHub · 免费资源导航站

一个零依赖的「免费链接 / 实用工具」收集站：前台可浏览搜索，后台可增删改查。
**纯静态 HTML/CSS/JS，双击即可运行，不需要安装 Node、Python 或任何环境。**

---

## 一、目录结构

```
默认项目/
├── index.html          前台首页（分类 + 搜索 + 卡片 + 下载项）
├── admin.html          后台管理（登录 → 增删改查 → 导入导出）
├── serve.ps1           本地预览服务器（可选，零依赖）
├── upload-oss.ps1      安装包一键上传到阿里云 OSS（零依赖）
├── oss.conf.example.json  OSS 配置模板（复制成 oss.conf.json 填密钥）
└── assets/
    ├── style.css       全站样式（含深色模式、响应式）
    ├── seed.js         默认种子数据（首次打开时的初始内容）
    ├── store.js        数据层（localStorage 读写 + 导入导出）
    ├── app.js          前台逻辑
    └── admin.js        后台逻辑
```

---

## 二、怎么运行

### 方式 1：直接双击（最简单）
双击 `index.html`，用 Edge / Chrome 打开即可。
> 后台管理入口在右上角「后台管理」，默认密码 **`admin123`**。

### 方式 2：本地服务器（推荐，行为更接近线上）
右键 `serve.ps1` → 「使用 PowerShell 运行」，或命令行执行：

```powershell
powershell -ExecutionPolicy Bypass -File .\serve.ps1
```

然后访问：
- 前台 http://localhost:8899/
- 后台 http://localhost:8899/admin.html

> 端口被占用时：`.\serve.ps1 -Port 8900`

---

## 三、后台怎么用

| 操作 | 位置 |
|---|---|
| 登录 | 默认密码 `admin123`（可在「站点设置」里改） |
| 新建 / 重命名 / 删除分类 | 左侧分类面板 + 顶部「＋ 新建分类」/ 分类标题栏的 ✏️ 🗑️ |
| 添加 / 编辑 / 删除链接 | 顶部「＋ 添加链接」，或每行的 ✏️ 🗑️ ↑（上移排序） |
| 添加安装包 | 「＋ 添加链接」→ **类型选「安装包 / 可下载文件」**，再填版本/大小/平台/镜像 |
| 改站名 / 副标题 / 备案号 / 密码 | 右下角「⚙️ 站点设置」 |
| 备份数据 | 「导出 JSON」→ 下载一个 json 文件 |
| 恢复/迁移 | 「导入 JSON」→ 选择之前导出的文件 |
| 清空重来 | 「恢复默认」（需点两次确认） |

**链接填写小技巧**
- 链接不写 `https://` 会自动补上
- 图标留空 → 用名称首字自动生成彩色图标；填 emoji 更好看
- 标签用逗号分隔，如 `开源, 多线程`

**两种条目类型**

| 类型 | 前台表现 | 用途 |
|---|---|---|
| 普通链接 | 整张卡片可点击，跳转网站 | 网站、在线工具、资源站 |
| 安装包 | 显示「安装包」徽章 + 版本/大小/平台 + 「⬇ 下载」按钮 + 「更多源」 | 可下载的 exe / apk / dmg / zip |

> 镜像（备用下载源）格式：每行一个，`名称 | 链接`，例如
> ```
> 阿里云 OSS 直链 | https://xxx.oss-cn-hangzhou.aliyuncs.com/pkgs/a.exe
> 百度网盘 | https://pan.baidu.com/s/xxxx
> ```

---

## 四、怎么添加安装包（重要）

安装包是**大文件**，不能塞进站点的 localStorage（上限 5 MB），也不能放进静态托管的仓库。
正确做法：**站内只存下载链接，文件本体放对象存储**。

```
站点（轻，只有文字）  ──点击下载──▶  阿里云 OSS（放 exe/apk/dmg）
```

### 步骤 1：开通 OSS 并建 Bucket
1. 登录阿里云 → 开通「对象存储 OSS」（按量付费，几毛钱/GB）
2. 新建 Bucket，**读写权限选「公共读」**（否则外链下载会报 AccessDenied）
3. 记下 **Bucket 名** 和 **地域 Endpoint**（如 `oss-cn-hangzhou.aliyuncs.com`）
4. 右上角头像 → AccessKey 管理 → 创建 AccessKey，记下 ID 和 Secret

### 步骤 2：配置上传脚本
复制 `oss.conf.example.json` 为 `oss.conf.json`，填入：

```json
{
  "bucket": "你的bucket名",
  "endpoint": "oss-cn-hangzhou.aliyuncs.com",
  "accessKeyId": "LTAI5tXXXX",
  "accessKeySecret": "XXXX"
}
```

> ⚠️ `oss.conf.json` 含密钥，**绝对不要提交到 Git 或公开仓库**。

### 步骤 3：一键上传
把安装包放进一个文件夹（如 `installers/`），然后：

```powershell
# 上传整个文件夹
.\upload-oss.ps1 -Path .\installers

# 上传单个文件
.\upload-oss.ps1 -Path .\Motrix-Setup.exe -Key packages/Motrix-Setup.exe

# 只想看会上传到哪，不真传
.\upload-oss.ps1 -Path .\installers -DryRun
```

脚本会打印出**可直接粘贴的下载直链**，例如：
```
https://your-bucket.oss-cn-hangzhou.aliyuncs.com/packages/Motrix-Setup.exe
```

### 步骤 4：填进后台
打开「＋ 添加链接」→ 类型选「安装包 / 可下载文件」→ 把直链粘到「链接地址」→
再填版本号、文件大小、适用平台、其他镜像源 → 保存，前台立刻出现下载卡片。

### 常见问题
| 现象 | 原因 |
|---|---|
| 下载报 `AccessDenied` | 对象/Bucket 不是公共读；或用了 private 权限 |
| 上传报 `SignatureDoesNotMatch` | AccessKey 填错，或系统时间不准 |
| 想隐藏直链、防盗链 | Bucket 设成私有读，改成后端签名 URL（需要服务端，超出当前纯静态方案） |
| 腾讯云 COS | 本脚本针对 OSS 签名；COS 建议用官方 `coscmd`，思路相同 |

---

## 五、数据存在哪？会不会丢？

当前版本数据存在**浏览器的 localStorage**里（这就是你的"轻量数据库"）：

- ✅ 增删改查即时生效，刷新不丢
- ✅ 可随时「导出 JSON」备份
- ⚠️ **换浏览器 / 换电脑 / 清理浏览器数据 → 数据不会跟过去**
- ⚠️ 无痕模式关闭后数据会清空

所以：**内容攒多了记得点「导出 JSON」备份**，或参考下面第五步上线。

---

## 六、想真正上线（对外访问）

### 1）部署静态站（免费）
把整个文件夹上传即可，无需构建：

| 平台 | 说明 |
|---|---|
| **GitHub Pages** | 推到仓库 → Settings → Pages → 选分支，几分钟出网址 |
| **Vercel / Netlify** | 拖拽文件夹上传，秒出网址（国内访问一般） |
| **国内（免翻墙）** | 阿里云 OSS / 腾讯云 COS 静态网站托管，或宝塔面板 |
| **国内域名** | 需 ICP 备案，填到「站点设置 → 备案号」 |

### 2）升级成"真·云数据库"（多设备同步）
现在数据存在浏览器里，换设备不同步。想真正多人/多端共用，推荐：

**方案 A：Supabase（免费额度够用）**
1. 注册 supabase.com，建一个 project
2. 建表 `categories` / `items`
3. 前端引入 `@supabase/supabase-js`（CDN 一行）
4. 只需改 `assets/store.js` 里的 `load()` / `save()`：把 localStorage 换成 supabase 查询
   —— 前台和后台代码**完全不用动**

**方案 B：腾讯云开发 CloudBase / LeanCloud（国内访问快）**
思路同上，把 `store.js` 的读写换成对应 SDK。

> 关键：所有数据操作都收敛在 `store.js`，这就是当初把它单独拆出来的原因。

---

## 七、想改默认内容

编辑 `assets/seed.js` 里的 `categories` 数组即可（批量加链接最方便）。
改完后浏览器里点一次后台的「恢复默认」就能看到效果。

---

## 八、注意

- 后台密码是**纯前端校验**，只适合自用/本地，**不能当作真正的安全防护**。要对外公开后台，必须加服务端鉴权。
- `oss.conf.json` 里有 OSS 密钥，**不要提交到 Git / 公开仓库**（已在 `.gitignore` 中排除）。
- 收集的链接请遵守各站点条款，不要收录侵权/盗版内容，避免法律风险。
