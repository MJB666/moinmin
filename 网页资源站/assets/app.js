/* ===========================================================
 * app.js —— 前台导航逻辑
 * =========================================================== */
(function () {
  const { U } = window;

  let data = Store.load();
  let activeCat = "all";
  let keyword = "";

  const $ = (id) => document.getElementById(id);
  const el = {
    siteTitle: $("siteTitle"),
    catList: $("catList"),
    grid: $("grid"),
    empty: $("empty"),
    catTitle: $("catTitle"),
    catDesc: $("catDesc"),
    count: $("count"),
    search: $("searchInput"),
    themeBtn: $("themeBtn"),
    footer: $("footerText")
  };

  /* ---------------- 主题 ---------------- */
  const THEME_KEY = "freehub.theme";
  function applyTheme(t) {
    document.documentElement.setAttribute("data-theme", t);
    el.themeBtn.textContent = t === "dark" ? "☀️" : "🌙";
  }
  applyTheme(localStorage.getItem(THEME_KEY) || "light");
  el.themeBtn.addEventListener("click", () => {
    const next = document.documentElement.getAttribute("data-theme") === "dark" ? "light" : "dark";
    localStorage.setItem(THEME_KEY, next);
    applyTheme(next);
  });

  /* ---------------- 站点信息 ---------------- */
  function renderSite() {
    const s = data.settings || {};
    if (s.siteTitle) {
      el.siteTitle.textContent = s.siteTitle;
      document.title = s.siteTitle + " · 免费资源导航";
    }
    el.footer.innerHTML =
      (s.siteSubtitle ? U.esc(s.siteSubtitle) + " · " : "") +
      "链接来自互联网，本站仅作收集整理 · <a href=\"admin.html\">后台管理</a>" +
      (s.icp ? " · " + U.esc(s.icp) : "");
  }

  /* ---------------- 分类列表 ---------------- */
  function totalItems() {
    return data.categories.reduce((n, c) => n + c.items.length, 0);
  }

  function renderCats() {
    const list = [{ id: "all", name: "全部", icon: "🏠", items: { length: totalItems() } }]
      .concat(data.categories);

    el.catList.innerHTML = list.map((c) => {
      const n = c.id === "all" ? totalItems() : c.items.length;
      return `<li class="cat-item ${c.id === activeCat ? "active" : ""}" data-id="${c.id}">
        <span class="cat-emoji">${U.esc(c.icon || "📁")}</span>
        <span class="cat-name">${U.esc(c.name)}</span>
        <span class="cat-count">${n}</span>
      </li>`;
    }).join("");

    el.catList.querySelectorAll(".cat-item").forEach((node) => {
      node.addEventListener("click", () => {
        activeCat = node.dataset.id;
        renderCats();
        renderGrid();
      });
    });
  }

  /* ---------------- 卡片 ---------------- */
  function logoHtml(item) {
    if (item.icon) {
      return `<div class="card-logo" style="background:${U.colorFor(item.name)}">${U.esc(item.icon)}</div>`;
    }
    return `<div class="card-logo" style="background:${U.colorFor(item.name)}">${U.esc(U.initial(item.name))}</div>`;
  }

  function cardHtml(item) {
    if (item.kind === "download") return downloadCardHtml(item);
    const tags = (item.tags || []).map((t) => `<span class="tag">${U.esc(t)}</span>`).join("");
    const host = U.hostname(item.url);
    return `<a class="card" href="${U.esc(item.url)}" target="_blank" rel="noopener noreferrer">
      ${logoHtml(item)}
      <div class="card-body">
        <div class="card-name">
          <span class="name-text">${U.esc(item.name)}</span>
          ${item.hot ? '<span class="badge-hot">HOT</span>' : ""}
        </div>
        ${item.desc ? `<div class="card-desc">${U.esc(item.desc)}</div>` : ""}
        ${tags ? `<div class="card-tags">${tags}</div>` : ""}
        ${host ? `<div class="card-host">${U.esc(host)} ↗</div>` : ""}
      </div>
    </a>`;
  }

  /** 安装包 / 可下载文件 卡片 */
  function downloadCardHtml(item) {
    const tags = (item.tags || []).map((t) => `<span class="tag">${U.esc(t)}</span>`).join("");
    const chips = [item.platform, item.version, item.size]
      .filter(Boolean)
      .map((c) => `<span class="chip">${U.esc(c)}</span>`)
      .join("");

    const mirrors = item.mirrors || [];
    const mirrorHtml = mirrors.length
      ? `<div class="mirror-list" hidden>
           ${mirrors.map((m) => `<a href="${U.esc(m.url)}" target="_blank" rel="noopener noreferrer">${U.esc(m.label)} ↗</a>`).join("")}
         </div>`
      : "";
    const mirrorBtn = mirrors.length
      ? `<button class="btn ghost sm" data-mirrors type="button">更多源 <span class="caret">▾</span></button>`
      : "";

    return `<div class="card card-dl">
      ${logoHtml(item)}
      <div class="card-body">
        <div class="card-name">
          <span class="name-text">${U.esc(item.name)}</span>
          ${item.hot ? '<span class="badge-hot">HOT</span>' : ""}
          <span class="badge-dl">安装包</span>
        </div>
        ${item.desc ? `<div class="card-desc">${U.esc(item.desc)}</div>` : ""}
        ${chips ? `<div class="dl-chips">${chips}</div>` : ""}
        ${tags ? `<div class="card-tags">${tags}</div>` : ""}
        <div class="card-actions">
          <a class="btn sm" href="${U.esc(item.url)}" target="_blank" rel="noopener noreferrer">⬇ 下载</a>
          ${mirrorBtn}
        </div>
        ${mirrorHtml}
      </div>
    </div>`;
  }

  function matches(item) {
    if (!keyword) return true;
    const k = keyword.toLowerCase();
    return (
      item.name.toLowerCase().includes(k) ||
      (item.desc || "").toLowerCase().includes(k) ||
      (item.url || "").toLowerCase().includes(k) ||
      (item.tags || []).some((t) => t.toLowerCase().includes(k))
    );
  }

  function renderGrid() {
    const cats = activeCat === "all"
      ? data.categories
      : data.categories.filter((c) => c.id === activeCat);

    let html = "";
    let shown = 0;

    cats.forEach((c) => {
      const items = c.items.filter(matches);
      if (!items.length) return;
      shown += items.length;

      // 「全部」视图下带分组标题，单分类视图只出卡片
      if (activeCat === "all" && !keyword) {
        html += `<div class="grid-section-title" style="grid-column:1/-1;display:flex;align-items:center;gap:8px;margin:14px 0 -2px;font-weight:700;font-size:15px">
          <span>${U.esc(c.icon || "📁")}</span><span>${U.esc(c.name)}</span>
          <span class="muted" style="font-size:12px;font-weight:400">${U.esc(c.desc || "")}</span>
        </div>`;
      }
      html += items.map(cardHtml).join("");
    });

    el.grid.innerHTML = html;
    el.empty.hidden = shown > 0;

    // 安装包卡片的「更多源」展开 / 收起
    el.grid.querySelectorAll("[data-mirrors]").forEach((btn) => {
      btn.addEventListener("click", () => {
        const list = btn.closest(".card-dl").querySelector(".mirror-list");
        if (!list) return;
        const willOpen = list.hidden;
        list.hidden = !willOpen;
        const caret = btn.querySelector(".caret");
        if (caret) caret.textContent = willOpen ? "▴" : "▾";
      });
    });

    // 标题区
    if (activeCat === "all") {
      el.catTitle.textContent = keyword ? "搜索结果" : "全部";
      el.catDesc.textContent = keyword ? `关键词：“${keyword}”` : (data.settings.siteSubtitle || "");
    } else {
      const c = data.categories.find((x) => x.id === activeCat);
      el.catTitle.textContent = (c ? (c.icon || "") + " " + c.name : "");
      el.catDesc.textContent = c ? (c.desc || "") : "";
    }
    el.count.textContent = `共 ${shown} 个`;
  }

  /* ---------------- 搜索（防抖） ---------------- */
  let timer = null;
  el.search.addEventListener("input", (e) => {
    clearTimeout(timer);
    timer = setTimeout(() => {
      keyword = e.target.value.trim();
      renderGrid();
    }, 140);
  });

  /* ---------------- 启动 ---------------- */
  renderSite();
  renderCats();
  renderGrid();

  // 从后台编辑返回时自动刷新
  window.addEventListener("storage", (e) => {
    if (e.key === "freehub.data.v1") {
      data = Store.load();
      renderSite(); renderCats(); renderGrid();
    }
  });
})();
