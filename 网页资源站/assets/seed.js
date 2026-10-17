/* ===========================================================
 * seed.js —— 默认种子数据
 * 首次打开站点时用它初始化；之后数据存在浏览器 localStorage。
 * 想批量改默认内容，改这里即可（清空浏览器数据后生效）。
 * =========================================================== */
window.SEED_DATA = {
  settings: {
    siteTitle: "FreeHub 免费资源导航",
    siteSubtitle: "一个专门收集免费链接与实用工具的导航站",
    icp: "",
    adminPass: "admin123"
  },
  categories: [
    {
      id: "cloud",
      name: "网盘工具",
      icon: "🗂️",
      desc: "存储、分享、直链下载",
      items: [
        { id: "c1", name: "百度网盘", url: "https://pan.baidu.com", desc: "国内用户基数最大的网盘", icon: "", tags: ["网盘"], hot: true },
        { id: "c2", name: "阿里云盘", url: "https://www.alipan.com", desc: "不限速、大容量", icon: "", tags: ["网盘", "不限速"], hot: true },
        { id: "c3", name: "夸克网盘", url: "https://pan.quark.cn", desc: "夸克浏览器配套网盘", icon: "", tags: ["网盘"] },
        { id: "c4", name: "115生活", url: "https://115.com", desc: "老牌大容量网盘", icon: "", tags: ["网盘"] },
        { id: "c5", name: "天翼云盘", url: "https://cloud.189.cn", desc: "电信出品，空间大", icon: "", tags: ["网盘"] },
        { id: "c6", name: "迅雷云盘", url: "https://pan.xunlei.com", desc: "配合迅雷下载", icon: "", tags: ["网盘"] }
      ]
    },
    {
      id: "download",
      name: "下载工具",
      icon: "⬇️",
      desc: "多线程、BT、磁力下载",
      items: [
        { id: "d1", name: "Motrix", url: "https://motrix.app", desc: "免费开源，aria2 内核，支持 HTTP/BT/磁力", icon: "", tags: ["开源", "多线程"], hot: true },
        { id: "d2", name: "aria2", url: "https://aria2.github.io", desc: "轻量命令行下载神器", icon: "", tags: ["开源", "命令行"] },
        { id: "d3", name: "qBittorrent", url: "https://www.qbittorrent.org", desc: "免费开源 BT 客户端", icon: "", tags: ["开源", "BT"] },
        { id: "d4", name: "IDM", url: "https://www.internetdownloadmanager.com", desc: "经典多线程下载器（收费/试用）", icon: "", tags: ["多线程"] },
        { id: "d5", name: "迅雷", url: "https://www.xunlei.com", desc: "老牌下载器", icon: "", tags: ["下载"] }
      ]
    },
    {
      id: "script",
      name: "浏览器脚本 / 扩展",
      icon: "🐒",
      desc: "油猴脚本与实用扩展",
      items: [
        { id: "s1", name: "Greasy Fork", url: "https://greasyfork.org", desc: "最大的中文用户脚本站", icon: "", tags: ["油猴"], hot: true },
        { id: "s2", name: "篡改猴 Tampermonkey", url: "https://www.tampermonkey.net", desc: "最流行的用户脚本管理器", icon: "", tags: ["扩展"], hot: true },
        { id: "s3", name: "OpenUserJS", url: "https://openuserjs.org", desc: "另一个脚本托管平台", icon: "", tags: ["油猴"] },
        { id: "s4", name: "uBlock Origin", url: "https://github.com/gorhill/uBlock", desc: "高效广告拦截扩展", icon: "", tags: ["开源", "去广告"] }
      ]
    },
    {
      id: "online",
      name: "在线工具",
      icon: "🧰",
      desc: "打开网页就能用",
      items: [
        { id: "o1", name: "TinyPNG", url: "https://tinypng.com", desc: "在线图片压缩", icon: "", tags: ["图片"] },
        { id: "o2", name: "Squoosh", url: "https://squoosh.app", desc: "谷歌出品的图片压缩", icon: "", tags: ["图片", "开源"] },
        { id: "o3", name: "Convertio", url: "https://convertio.co", desc: "在线格式转换", icon: "", tags: ["转换"] },
        { id: "o4", name: "regex101", url: "https://regex101.com", desc: "正则表达式在线测试", icon: "", tags: ["开发"] },
        { id: "o5", name: "JSON 中文网", url: "https://www.json.cn", desc: "JSON 格式化与校验", icon: "", tags: ["开发"] }
      ]
    },
    {
      id: "media",
      name: "影视音乐",
      icon: "🎬",
      desc: "免费影音平台",
      items: [
        { id: "m1", name: "哔哩哔哩", url: "https://www.bilibili.com", desc: "国内最大的二次元/学习视频社区", icon: "", tags: ["视频"], hot: true },
        { id: "m2", name: "YouTube", url: "https://www.youtube.com", desc: "全球视频平台", icon: "", tags: ["视频"] },
        { id: "m3", name: "网易云音乐", url: "https://music.163.com", desc: "音乐播放与歌单", icon: "", tags: ["音乐"] },
        { id: "m4", name: "Listen 1", url: "https://listen1.github.io/listen1/", desc: "聚合多平台音乐的开源播放器", icon: "", tags: ["开源", "音乐"] }
      ]
    },
    {
      id: "study",
      name: "学习资源",
      icon: "📚",
      desc: "网课、文档、代码",
      items: [
        { id: "t1", name: "中国大学 MOOC", url: "https://www.icourse163.org", desc: "名校免费公开课", icon: "", tags: ["网课"] },
        { id: "t2", name: "GitHub", url: "https://github.com", desc: "全球最大代码托管平台", icon: "", tags: ["代码"], hot: true },
        { id: "t3", name: "菜鸟教程", url: "https://www.runoob.com", desc: "编程入门教程", icon: "", tags: ["教程"] },
        { id: "t4", name: "MDN", url: "https://developer.mozilla.org/zh-CN/", desc: "Web 开发权威文档", icon: "", tags: ["文档"] }
      ]
    },
    {
      id: "ai",
      name: "AI 工具",
      icon: "🤖",
      desc: "免费可用的 AI 助手",
      items: [
        { id: "a1", name: "DeepSeek", url: "https://chat.deepseek.com", desc: "国产大模型，免费", icon: "", tags: ["对话"], hot: true },
        { id: "a2", name: "Kimi", url: "https://kimi.moonshot.cn", desc: "长文本处理见长", icon: "", tags: ["对话"] },
        { id: "a3", name: "通义千问", url: "https://tongyi.aliyun.com", desc: "阿里出品", icon: "", tags: ["对话"] },
        { id: "a4", name: "豆包", url: "https://www.doubao.com", desc: "字节出品", icon: "", tags: ["对话"] },
        { id: "a5", name: "ChatGPT", url: "https://chat.openai.com", desc: "OpenAI 对话助手", icon: "", tags: ["对话", "需网络环境"] }
      ]
    },
    {
      id: "software",
      name: "系统软件",
      icon: "💻",
      desc: "装机必备免费软件",
      items: [
        { id: "w1", name: "火绒安全", url: "https://www.huorong.cn", desc: "轻量无捆绑的安全软件", icon: "", tags: ["安全"], hot: true },
        { id: "w2", name: "7-Zip", url: "https://www.7-zip.org", desc: "免费开源压缩软件", icon: "", tags: ["开源", "压缩"] },
        { id: "w3", name: "Everything", url: "https://www.voidtools.com", desc: "秒搜本地文件", icon: "", tags: ["效率", "必备"] },
        { id: "w4", name: "PotPlayer", url: "https://potplayer.daum.net", desc: "全能本地播放器", icon: "", tags: ["播放器"] }
      ]
    },
    {
      id: "packages",
      name: "软件安装包",
      icon: "📦",
      desc: "示例数据 —— 把 url 换成你自己的 OSS 直链即可",
      items: [
        {
          id: "p1", kind: "download",
          name: "Motrix", url: "https://your-bucket.oss-cn-hangzhou.aliyuncs.com/packages/Motrix-Setup-v1.8.5.exe",
          desc: "免费开源的全能下载器，支持 HTTP / BT / 磁力", icon: "⬇️",
          version: "v1.8.5", size: "58 MB", platform: "Windows 10/11 · 64 位",
          tags: ["开源", "下载器"], hot: true,
          mirrors: [
            { label: "阿里云 OSS 直链", url: "https://your-bucket.oss-cn-hangzhou.aliyuncs.com/packages/Motrix-Setup-v1.8.5.exe" },
            { label: "GitHub Releases", url: "https://github.com/agalwood/Motrix/releases" }
          ]
        },
        {
          id: "p2", kind: "download",
          name: "7-Zip", url: "https://your-bucket.oss-cn-hangzhou.aliyuncs.com/packages/7z2408-x64.exe",
          desc: "免费开源压缩解压工具，无广告无捆绑", icon: "🗜️",
          version: "24.08", size: "1.6 MB", platform: "Windows · 64 位",
          tags: ["开源", "压缩"],
          mirrors: [
            { label: "官网下载", url: "https://www.7-zip.org/download.html" }
          ]
        },
        {
          id: "p3", kind: "download",
          name: "火绒安全软件", url: "https://your-bucket.oss-cn-hangzhou.aliyuncs.com/packages/sysdiag-all.exe",
          desc: "轻量、无弹窗、不劫持的国产安全软件", icon: "🛡️",
          version: "6.0", size: "42 MB", platform: "Windows 7 及以上",
          tags: ["安全", "必备"], hot: true,
          mirrors: []
        }
      ]
    }
  ]
};
