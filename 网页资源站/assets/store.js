/* ===========================================================
 * store.js —— 数据层（本地 "数据库"）
 * - 用 localStorage 保存数据，双击打开也能用
 * - 提供导入 / 导出 JSON，方便备份与迁移
 * - 以后想换真数据库，只改这个文件即可
 * =========================================================== */
(function () {
  const KEY = "freehub.data.v1";

  function clone(o) {
    return JSON.parse(JSON.stringify(o));
  }

  const Store = {
    /** 默认数据（来自 seed.js） */
    defaultData() {
      return clone(window.SEED_DATA || { settings: {}, categories: [] });
    },

    /** 读取全部数据 */
    load() {
      try {
        const raw = localStorage.getItem(KEY);
        if (raw) {
          const d = JSON.parse(raw);
          if (d && Array.isArray(d.categories)) return d;
        }
      } catch (e) {
        console.warn("[Store] 读取失败，使用默认数据", e);
      }
      return this.defaultData();
    },

    /** 保存全部数据 */
    save(data) {
      data.settings = data.settings || {};
      data.settings.updatedAt = new Date().toISOString();
      localStorage.setItem(KEY, JSON.stringify(data));
      return data;
    },

    /** 恢复为默认数据 */
    reset() {
      localStorage.removeItem(KEY);
      return this.defaultData();
    },

    /** 导出为 JSON 文件 */
    exportFile(data) {
      const name = "freehub-data-" + new Date().toISOString().slice(0, 10) + ".json";
      const blob = new Blob([JSON.stringify(data, null, 2)], { type: "application/json" });
      const a = document.createElement("a");
      a.href = URL.createObjectURL(blob);
      a.download = name;
      document.body.appendChild(a);
      a.click();
      a.remove();
      setTimeout(() => URL.revokeObjectURL(a.href), 1000);
    },

    /** 从文件导入 JSON，返回 Promise<data> */
    importFile(file) {
      return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => {
          try {
            const d = JSON.parse(reader.result);
            if (!d || !Array.isArray(d.categories)) {
              reject(new Error("文件格式不正确：缺少 categories 数组"));
              return;
            }
            resolve(d);
          } catch (e) {
            reject(e);
          }
        };
        reader.onerror = () => reject(reader.error);
        reader.readAsText(file);
      });
    },

    /** 生成唯一 id */
    uid(prefix) {
      return (prefix || "id") + "_" + Math.random().toString(36).slice(2, 8) + Date.now().toString(36).slice(-3);
    }
  };

  /* ---------- 通用小工具 ---------- */
  const U = {
    /** 取域名后的主机名，用于展示 */
    hostname(url) {
      try {
        return new URL(url).hostname.replace(/^www\./, "");
      } catch (e) {
        return "";
      }
    },
    /** 根据字符串生成稳定的颜色 */
    colorFor(str) {
      let h = 0;
      for (let i = 0; i < str.length; i++) h = (h * 31 + str.charCodeAt(i)) % 360;
      return "hsl(" + h + ", 65%, 52%)";
    },
    /** 取首个字符（兼容 emoji 与中文） */
    initial(str) {
      if (!str) return "?";
      return Array.from(str.trim())[0].toUpperCase();
    },
    /** 转义 HTML，防止注入 */
    esc(s) {
      return String(s == null ? "" : s)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;");
    }
  };

  window.Store = Store;
  window.U = U;
})();
