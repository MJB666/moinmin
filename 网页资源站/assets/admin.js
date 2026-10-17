/* ===========================================================
 * admin.js —— 后台管理逻辑
 * =========================================================== */
(function () {
  const { U } = window;
  const $ = (id) => document.getElementById(id);

  let data = Store.load();
  let curCatId = data.categories[0] ? data.categories[0].id : null;
  let revertTimer = null; // 用于「恢复默认」二次确认

  /* ---------------- 主题 ---------------- */
  const THEME_KEY = "freehub.theme";
  const themeBtn = $("themeBtn");
  function applyTheme(t) {
    document.documentElement.setAttribute("data-theme", t);
    themeBtn.textContent = t === "dark" ? "☀️" : "🌙";
  }
  applyTheme(localStorage.getItem(THEME_KEY) || "light");
  themeBtn.addEventListener("click", () => {
    const next = document.documentElement.getAttribute("data-theme") === "dark" ? "light" : "dark";
    localStorage.setItem(THEME_KEY, next);
    applyTheme(next);
  });

  /* ---------------- Toast ---------------- */
  let toastTimer = null;
  function toast(msg) {
    const t = $("toast");
    t.textContent = msg;
    t.classList.add("show");
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => t.classList.remove("show"), 1900);
  }

  function persist() {
    Store.save(data);
    renderCats();
    renderItems();
    renderSettings();
  }

  /* ---------------- 登录 ---------------- */
  const AUTH_KEY = "freehub.auth";
  function setAuth(on) {
    if (on) sessionStorage.setItem(AUTH_KEY, "1");
    else sessionStorage.removeItem(AUTH_KEY);
    $("loginView").hidden = on;
    $("adminView").hidden = !on;
    $("logoutBtn").hidden = !on;
  }
  $("loginBtn").addEventListener("click", () => {
    const pass = $("loginPass").value;
    const real = (data.settings && data.settings.adminPass) || "admin123";
    if (pass === real) {
      setAuth(true);
      toast("登录成功");
    } else {
      toast("密码错误");
    }
  });
  $("loginPass").addEventListener("keydown", (e) => {
    if (e.key === "Enter") $("loginBtn").click();
  });
  $("logoutBtn").addEventListener("click", () => {
    setAuth(false);
    toast("已退出");
  });

  /* ---------------- 分类渲染 ---------------- */
  function renderCats() {
    const ul = $("adminCatList");
    if (!data.categories.length) {
      ul.innerHTML = '<li class="muted" style="padding:10px">还没有分类</li>';
      $("catTotal").textContent = "0 个分类";
      curCatId = null;
      renderItems();
      return;
    }
    if (!data.categories.some((c) => c.id === curCatId)) curCatId = data.categories[0].id;

    ul.innerHTML = data.categories.map((c) => `
      <li class="admin-cat ${c.id === curCatId ? "active" : ""}" data-id="${c.id}">
        <span>${U.esc(c.icon || "📁")}</span>
        <span class="n">${U.esc(c.name)}</span>
        <span class="c">${c.items.length}</span>
      </li>`).join("");

    ul.querySelectorAll(".admin-cat").forEach((n) => {
      n.addEventListener("click", () => { curCatId = n.dataset.id; renderCats(); renderItems(); });
    });
    $("catTotal").textContent = data.categories.length + " 个分类";
  }

  /* ---------------- 链接渲染 ---------------- */
  function curCat() {
    return data.categories.find((c) => c.id === curCatId) || null;
  }

  function renderItems() {
    const cat = curCat();
    const tb = $("itemsBody");
    if (!cat) {
      $("curCatName").textContent = "请选择分类";
      tb.innerHTML = "";
      $("itemsEmpty").hidden = false;
      return;
    }
    $("curCatName").textContent = (cat.icon || "📁") + " " + cat.name;
    $("itemsEmpty").hidden = cat.items.length > 0;

    tb.innerHTML = cat.items.map((it) => `
      <tr data-id="${it.id}">
        <td><span style="display:inline-grid;place-items:center;width:30px;height:30px;border-radius:8px;color:#fff;font-weight:700;background:${U.colorFor(it.name)}">${U.esc(it.icon || U.initial(it.name))}</span></td>
        <td>${U.esc(it.name)} ${it.hot ? '<span class="badge-hot">HOT</span>' : ""}${it.kind === "download" ? ' <span class="badge-dl">安装包</span>' : ""}
          <div class="muted" style="font-size:12px">${U.esc(it.desc || "")}</div></td>
        <td class="u">${U.esc(it.url)}</td>
        <td>${it.hot ? "🔥" : "—"}</td>
        <td class="actions">
          <button class="icon-btn" data-act="up" title="上移">↑</button>
          <button class="icon-btn" data-act="edit" title="编辑">✏️</button>
          <button class="icon-btn del" data-act="del" title="删除">🗑️</button>
        </td>
      </tr>`).join("");

    tb.querySelectorAll("tr").forEach((tr) => {
      const id = tr.dataset.id;
      tr.querySelectorAll("[data-act]").forEach((btn) => {
        btn.addEventListener("click", () => {
          const act = btn.dataset.act;
          const c = curCat();
          const idx = c.items.findIndex((x) => x.id === id);
          if (act === "edit") openItemModal(c.items[idx]);
          if (act === "del") {
            if (confirm("确定删除「" + c.items[idx].name + "」？")) {
              c.items.splice(idx, 1);
              persist();
              toast("已删除");
            }
          }
          if (act === "up" && idx > 0) {
            [c.items[idx - 1], c.items[idx]] = [c.items[idx], c.items[idx - 1]];
            persist();
          }
        });
      });
    });
  }

  /* ---------------- 站点设置 ---------------- */
  function renderSettings() {
    const s = data.settings || (data.settings = {});
    $("setTitle").value = s.siteTitle || "";
    $("setSubtitle").value = s.siteSubtitle || "";
    $("setIcp").value = s.icp || "";
    $("setPass").value = s.adminPass || "admin123";
  }
  $("saveSettingsBtn").addEventListener("click", () => {
    const s = data.settings || (data.settings = {});
    s.siteTitle = $("setTitle").value.trim() || "FreeHub 免费资源导航";
    s.siteSubtitle = $("setSubtitle").value.trim();
    s.icp = $("setIcp").value.trim();
    s.adminPass = $("setPass").value.trim() || "admin123";
    persist();
    toast("设置已保存");
  });

  /* ---------------- 链接弹窗 ---------------- */
  function toggleDlFields() {
    const isDl = $("itemKind").value === "download";
    $("dlFields").hidden = !isDl;
    $("itemUrlHint").textContent = isDl
      ? "填文件下载直链（如 OSS 给的 https://xxx.oss-cn-xxx.aliyuncs.com/xxx.exe）"
      : "填网站地址";
  }
  $("itemKind").addEventListener("change", toggleDlFields);

  function openItemModal(item) {
    if (!curCat()) { toast("请先创建并选择一个分类"); return; }
    const isNew = !item;
    $("itemModalTitle").textContent = isNew ? "添加链接" : "编辑链接";
    $("itemId").value = isNew ? "" : item.id;
    $("itemName").value = isNew ? "" : item.name;
    $("itemIcon").value = isNew ? "" : (item.icon || "");
    $("itemKind").value = isNew ? "link" : (item.kind || "link");
    $("itemUrl").value = isNew ? "" : item.url;
    $("itemDesc").value = isNew ? "" : (item.desc || "");
    $("itemVersion").value = isNew ? "" : (item.version || "");
    $("itemSize").value = isNew ? "" : (item.size || "");
    $("itemPlatform").value = isNew ? "" : (item.platform || "");
    $("itemMirrors").value = isNew ? "" : (item.mirrors || [])
      .map((m) => m.label + " | " + m.url).join("\n");
    $("itemTags").value = isNew ? "" : (item.tags || []).join(", ");
    $("itemHot").value = isNew ? "0" : (item.hot ? "1" : "0");
    toggleDlFields();
    $("itemModal").classList.add("open");
    setTimeout(() => $("itemName").focus(), 30);
  }

  /** 解析镜像文本框：每行 "名称 | 链接" */
  function parseMirrors(text) {
    return text.split("\n").map((line) => {
      const t = line.trim();
      if (!t) return null;
      const i = t.indexOf("|");
      let label, url;
      if (i === -1) { label = "下载源"; url = t; }
      else { label = t.slice(0, i).trim() || "下载源"; url = t.slice(i + 1).trim(); }
      if (!url) return null;
      if (!/^https?:\/\//i.test(url)) url = "https://" + url;
      return { label, url };
    }).filter(Boolean);
  }

  $("addItemBtn").addEventListener("click", () => openItemModal(null));
  $("itemSaveBtn").addEventListener("click", () => {
    const name = $("itemName").value.trim();
    let url = $("itemUrl").value.trim();
    if (!name) { toast("请填写名称"); return; }
    if (!url) { toast("请填写链接"); return; }
    if (!/^https?:\/\//i.test(url)) url = "https://" + url;

    const c = curCat();
    const id = $("itemId").value;
    const kind = $("itemKind").value;
    const payload = {
      name,
      url,
      kind,
      icon: $("itemIcon").value.trim(),
      desc: $("itemDesc").value.trim(),
      tags: $("itemTags").value.split(/[,，]/).map((s) => s.trim()).filter(Boolean),
      hot: $("itemHot").value === "1"
    };
    if (kind === "download") {
      payload.version = $("itemVersion").value.trim();
      payload.size = $("itemSize").value.trim();
      payload.platform = $("itemPlatform").value.trim();
      payload.mirrors = parseMirrors($("itemMirrors").value);
    }
    if (id) {
      const it = c.items.find((x) => x.id === id);
      Object.assign(it, payload);
    } else {
      c.items.push(Object.assign({ id: Store.uid("i") }, payload));
    }
    persist();
    closeModal($("itemModal"));
    toast("已保存");
  });

  /* ---------------- 分类弹窗 ---------------- */
  function openCatModal(cat) {
    const isNew = !cat;
    $("catModalTitle").textContent = isNew ? "新建分类" : "重命名 / 编辑分类";
    $("catId").value = isNew ? "" : cat.id;
    $("catName").value = isNew ? "" : cat.name;
    $("catIcon").value = isNew ? "" : (cat.icon || "");
    $("catDesc").value = isNew ? "" : (cat.desc || "");
    $("catModal").classList.add("open");
    setTimeout(() => $("catName").focus(), 30);
  }
  $("addCatBtn").addEventListener("click", () => openCatModal(null));
  $("renameCatBtn").addEventListener("click", () => {
    const c = curCat();
    if (!c) { toast("请先选择分类"); return; }
    openCatModal(c);
  });
  $("catSaveBtn").addEventListener("click", () => {
    const name = $("catName").value.trim();
    if (!name) { toast("请填写分类名称"); return; }
    const id = $("catId").value;
    if (id) {
      const c = data.categories.find((x) => x.id === id);
      c.name = name;
      c.icon = $("catIcon").value.trim() || "📁";
      c.desc = $("catDesc").value.trim();
    } else {
      const nc = {
        id: Store.uid("c"),
        name,
        icon: $("catIcon").value.trim() || "📁",
        desc: $("catDesc").value.trim(),
        items: []
      };
      data.categories.push(nc);
      curCatId = nc.id;
    }
    persist();
    closeModal($("catModal"));
    toast("已保存");
  });

  $("delCatBtn").addEventListener("click", () => {
    const c = curCat();
    if (!c) { toast("请先选择分类"); return; }
    if (!confirm(`确定删除分类「${c.name}」及其下 ${c.items.length} 个链接？`)) return;
    data.categories = data.categories.filter((x) => x.id !== c.id);
    curCatId = data.categories[0] ? data.categories[0].id : null;
    persist();
    toast("已删除分类");
  });

  /* ---------------- 关闭弹窗 ---------------- */
  function closeModal(mask) { mask.classList.remove("open"); }
  document.querySelectorAll(".modal-mask").forEach((mask) => {
    mask.addEventListener("click", (e) => { if (e.target === mask) closeModal(mask); });
    mask.querySelectorAll("[data-close]").forEach((b) =>
      b.addEventListener("click", () => closeModal(mask)));
  });
  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape") document.querySelectorAll(".modal-mask.open").forEach(closeModal);
  });

  /* ---------------- 导入 / 导出 / 重置 ---------------- */
  $("exportBtn").addEventListener("click", () => {
    Store.exportFile(data);
    toast("已导出 JSON");
  });
  $("importBtn").addEventListener("click", () => $("importFile").click());
  $("importFile").addEventListener("change", async (e) => {
    const file = e.target.files[0];
    if (!file) return;
    try {
      const incoming = await Store.importFile(file);
      if (!confirm("导入将覆盖当前全部数据，确定继续？")) return;
      data = incoming;
      data.settings = data.settings || {};
      curCatId = data.categories[0] ? data.categories[0].id : null;
      persist();
      toast("导入成功");
    } catch (err) {
      toast("导入失败：" + err.message);
    } finally {
      e.target.value = "";
    }
  });

  $("resetBtn").addEventListener("click", () => {
    const btn = $("resetBtn");
    if (revertTimer) { // 第二次点击，真正执行
      clearTimeout(revertTimer);
      revertTimer = null;
      data = Store.reset();
      curCatId = data.categories[0] ? data.categories[0].id : null;
      renderCats(); renderItems(); renderSettings();
      btn.textContent = "恢复默认";
      toast("已恢复默认数据");
      return;
    }
    // 第一次点击 → 变成确认态，5 秒后自动还原
    btn.textContent = "再点一次确认！";
    revertTimer = setTimeout(() => {
      revertTimer = null;
      btn.textContent = "恢复默认";
    }, 5000);
  });

  /* ---------------- 启动 ---------------- */
  renderCats();
  renderItems();
  renderSettings();
  setAuth(sessionStorage.getItem(AUTH_KEY) === "1");
})();
