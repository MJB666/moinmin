/* ============================================================
   莫印泯 · 手机版  app.js
   · 图片背景每 60 秒自动切换（可关、可自选，自选后自动停）
   · 视频背景：点按钮开关；长按按钮选视频
   · 长按 2 秒 = 电脑上的「右键」→ 弹出半透明液态玻璃面板
   · 数据存在手机本地（localStorage，不支持时退化为内存）
   ============================================================ */
(function () {
  'use strict';

  var STORE = 'moin.mobile.v1';
  var BUNDLED = (window.BUNDLED && window.BUNDLED.images) ? window.BUNDLED : { images: [], videos: [] };

  /* ============================ 本地存储 ============================ */
  var store = (function () {
    var ok = true;
    try { localStorage.setItem('__moin__', '1'); localStorage.removeItem('__moin__'); }
    catch (e) { ok = false; }
    var mem = {};
    return {
      usable: ok,
      get: function (k, d) {
        try {
          var v = ok ? localStorage.getItem(k) : mem[k];
          return (v === null || v === undefined) ? d : JSON.parse(v);
        } catch (e) { return d; }
      },
      set: function (k, v) {
        var s;
        try { s = JSON.stringify(v); } catch (e) { return; }
        try { if (ok) localStorage.setItem(k, s); else mem[k] = s; }
        catch (e) { mem[k] = s; ok = false; }
      }
    };
  })();

  /* ============================ 默认数据 ============================ */
  var DEFAULT_CARDS = [
    { name: '淘宝',   url: 'taobao://m.taobao.com',      note: '购物 · 官方 App', fb: 'https://m.taobao.com' },
    { name: '天猫',   url: 'tmall://page.tm/index',      note: '品牌旗舰店',     fb: 'https://www.tmall.com' },
    { name: '京东',   url: 'openapp.jdmobile://virtual', note: '自营 · 秒杀',    fb: 'https://m.jd.com' },
    { name: '拼多多', url: 'pinduoduo://com.xunmeng.pinduoduo', note: '百亿补贴', fb: 'https://mobile.yangkeduo.com' },
    { name: '大麦网', url: 'damai://V1',                 note: '演唱会 · 演出票', fb: 'https://m.damai.cn' },
    { name: '猫眼演出', url: 'Maoyan://maoyan.com',      note: '电影 · 演出票',  fb: 'https://maoyan.com' },
    { name: '美团',   url: 'imeituan://www.meituan.com', note: '到店 · 门票',    fb: 'https://i.meituan.com' },
    { name: '哔哩哔哩会员购', url: 'bilibili://mall',    note: 'B 站演出票',     fb: 'https://show.bilibili.com' },
    { name: '苏宁易购', url: 'suning://m.suning.com',    note: '家电 · 数码',    fb: 'https://m.suning.com' },
    { name: '唯品会', url: 'vipshop://home',             note: '品牌特卖',       fb: 'https://m.vip.com' },
    { name: '得物',   url: 'dewu://home',                note: '潮品 · 球鞋',    fb: 'https://m.dewu.com' },
    { name: '小红书', url: 'xhsdiscover://home',         note: '种草 · 笔记',    fb: 'https://www.xiaohongshu.com' }
  ];

  var DEFAULT_SET = {
    imgAuto: true,
    autoSec: 60,
    videoOn: false,
    videoSrc: '',
    imgSrc: '',
    lite: false,
    glassCards: false,
    longPressSec: 2
  };

  /* ============================ 状态 ============================ */
  var cards = store.get(STORE + '.cards', null);
  if (!cards || !cards.length) {
    cards = DEFAULT_CARDS.map(function (c) {
      return { name: c.name, url: c.url, note: c.note, fb: c.fb, icon: '' };
    });
  }

  var set = {};
  (function () {
    var saved = store.get(STORE + '.set', {}) || {};
    for (var k in DEFAULT_SET) {
      if (DEFAULT_SET.hasOwnProperty(k)) {
        set[k] = (saved[k] === undefined || saved[k] === null) ? DEFAULT_SET[k] : saved[k];
      }
    }
  })();

  function saveCards() { store.set(STORE + '.cards', cards); }
  function saveSet() { store.set(STORE + '.set', set); }

  // 自选图片用 dataURL 存下来（换手机/重开也在）
  var customImg = store.get(STORE + '.customImg', '');

  /* ============================ 小工具 ============================ */
  var $ = function (id) { return document.getElementById(id); };

  function hueOf(s) {
    var h = 0;
    s = String(s || '');
    for (var i = 0; i < s.length; i++) h = (h * 31 + s.charCodeAt(i)) >>> 0;
    return h % 360;
  }
  function gradOf(s) {
    var h = hueOf(s);
    return 'linear-gradient(150deg, hsl(' + h + ',80%,66%), hsl(' + ((h + 42) % 360) + ',72%,50%))';
  }
  function shortUrl(u) {
    u = String(u || '');
    var m = u.match(/^[a-z][a-z0-9+.\-]*:\/\//i);
    if (m) u = u.slice(m[0].length);
    u = u.replace(/^www\./, '').replace(/\/+$/, '');
    return u.length > 26 ? u.slice(0, 26) + '…' : u;
  }
  function esc(s) {
    return String(s === null || s === undefined ? '' : s)
      .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
  }
  function isHttp(u) { return /^https?:\/\//i.test(String(u || '')); }

  var toastTimer = null;
  function toast(msg, ms) {
    var t = $('toast');
    t.textContent = msg;
    t.classList.add('on');
    if (toastTimer) clearTimeout(toastTimer);
    toastTimer = setTimeout(function () { t.classList.remove('on'); }, ms || 2200);
  }

  /* ============================ 背景：图片 ============================ */
  var bgA = $('bgA'), bgB = $('bgB'), frontB = false;
  var imgTimer = null;

  function allImages() {
    var list = BUNDLED.images.slice();
    if (customImg) list.unshift({ src: customImg, title: '我选的图片', custom: true });
    return list;
  }

  function pickImageIndex() {
    var list = allImages();
    if (!list.length) return -1;
    if (set.imgSrc === '__custom__' && customImg) return 0;
    for (var i = 0; i < list.length; i++) if (list[i].src === set.imgSrc) return i;
    return 0;
  }

  function showImage(idx, animate) {
    var list = allImages();
    if (!list.length) return;
    if (idx < 0 || idx >= list.length) idx = 0;
    var item = list[idx];
    set.imgSrc = item.custom ? '__custom__' : item.src;
    saveSet();

    var back = frontB ? bgA : bgB;
    var next = frontB ? bgB : bgA;

    if (animate === false) {
      next.style.transition = 'none';
      next.style.backgroundImage = 'url("' + item.src + '")';
      next.classList.add('on');
      back.classList.remove('on');
      // 强制一次重排，再把过渡恢复
      void next.offsetWidth;
      next.style.transition = '';
    } else {
      next.style.backgroundImage = 'url("' + item.src + '")';
      next.classList.add('on');
      back.classList.remove('on');
    }
    frontB = !frontB;
    updateHud();
  }

  function nextImage() {
    var list = allImages();
    if (list.length < 2) { toast('只有一张背景图，没得换'); return; }
    var cur = pickImageIndex();
    showImage((cur + 1) % list.length, true);
  }

  function startImgTimer() {
    stopImgTimer();
    if (!set.imgAuto) return;
    var list = allImages();
    if (list.length < 2) return;
    imgTimer = setInterval(function () {
      if (set.videoOn) return;         // 视频开着就不换图
      nextImage();
    }, Math.max(5, set.autoSec) * 1000);
  }
  function stopImgTimer() { if (imgTimer) { clearInterval(imgTimer); imgTimer = null; } }

  /* ============================ 背景：视频 ============================ */
  var vid = $('bgVideo');

  function allVideos() {
    return BUNDLED.videos.slice();
  }

  function applyVideo() {
    if (set.videoOn && set.videoSrc) {
      if (vid.getAttribute('data-src') !== set.videoSrc) {
        vid.setAttribute('data-src', set.videoSrc);
        vid.src = set.videoSrc;
      }
      vid.classList.add('on');
      var pr = vid.play();
      if (pr && pr.catch) pr.catch(function () { /* 自动播放被拦，等用户点一下 */ });
    } else {
      try { vid.pause(); } catch (e) { }
      vid.classList.remove('on');
    }
    updateHud();
  }

  function setVideo(src) {
    if (!src) { toast('没有可用的视频'); return; }
    set.videoSrc = src;
    set.videoOn = true;
    saveSet();
    applyVideo();
  }

  function toggleVideo() {
    if (set.videoOn) {
      set.videoOn = false;
      saveSet();
      applyVideo();
      toast('视频已关，现在显示图片背景');
      return;
    }
    var v = set.videoSrc || (BUNDLED.videos[0] && BUNDLED.videos[0].src) || '';
    if (!v) { openVideoPicker(); return; }
    setVideo(v);
  }

  /* ============================ 顶部状态 ============================ */
  function updateHud() {
    var bAuto = $('btnAuto'), bVid = $('btnVideo'), bImg = $('btnPickImg');

    var n = allImages().length;
    if (!n) {
      bAuto.textContent = '🔁 没有图片';
      bAuto.classList.remove('on');
    } else if (set.imgAuto) {
      bAuto.textContent = '🔁 自动切换：开';
      bAuto.classList.add('on');
    } else {
      bAuto.textContent = '🔁 自动切换：关';
      bAuto.classList.remove('on');
    }
    bAuto.setAttribute('data-count', n ? n + '张' : '');

    bVid.textContent = set.videoOn ? '⏸ 视频：开' : '▶ 视频：关';
    if (set.videoOn) bVid.classList.add('on'); else bVid.classList.remove('on');

    bImg.textContent = customImg ? '🖼 换回内置图' : '🖼 选图';
    updateHint();
  }

  function updateHint() {
    var h = $('hint');
    var n = cards.length;
    if (set.videoOn) h.textContent = '共 ' + n + ' 项 · 轻点打开 · 长按 ' + set.longPressSec + ' 秒弹菜单';
    else if (set.imgAuto && allImages().length > 1) h.textContent = '共 ' + n + ' 项 · 背景每 ' + set.autoSec + ' 秒自动换 · 长按卡片弹菜单';
    else h.textContent = '共 ' + n + ' 项 · 轻点打开 · 长按 ' + set.longPressSec + ' 秒弹菜单';
  }

  /* ============================ 卡片渲染 ============================ */
  function renderCards() {
    var box = $('cards');
    box.innerHTML = '';
    if (!cards.length) {
      var d = document.createElement('div');
      d.className = 'cards-empty';
      d.textContent = '还没有入口，点上面的「＋ 添加」加一个吧';
      box.appendChild(d);
      updateHint();
      return;
    }

    cards.forEach(function (c, idx) {
      var el = document.createElement('button');
      el.className = 'card glass';
      if (set.glassCards) el.classList.add('lg');
      el.setAttribute('data-idx', idx);
      el.setAttribute('type', 'button');

      var ico = document.createElement('div');
      ico.className = 'card-ico';
      if (c.icon) {
        if (isHttp(c.icon)) {
          var im = document.createElement('img');
          im.src = c.icon; im.alt = '';
          ico.appendChild(im);
        } else {
          ico.style.background = 'rgba(255,255,255,.14)';
          ico.textContent = c.icon;
        }
      } else {
        ico.style.background = gradOf(c.name);
        ico.textContent = (c.name || '?').trim().charAt(0) || '?';
      }

      var txt = document.createElement('div');
      txt.className = 'card-txt';
      txt.innerHTML =
        '<div class="card-name">' + esc(c.name) + '</div>' +
        (c.note ? '<div class="card-note">' + esc(c.note) + '</div>' : '') +
        '<div class="card-url">' + esc(shortUrl(c.url)) + '</div>';

      el.appendChild(ico);
      el.appendChild(txt);

      bindLongPress(
        el,
        function () { openCard(idx); },
        function (x, y) { openCardMenu(idx, x, y); }
      );

      box.appendChild(el);
    });
    updateHint();
  }

  function openCard(idx) {
    var c = cards[idx];
    if (!c) return;
    var url = c.url;
    if (!url) { toast('这条没有填链接'); return; }

    // App 协议 → 先试协议，试不动就换备用链接
    if (!isHttp(url)) {
      var t0 = Date.now();
      var fallback = c.fb || '';
      location.href = url;
      setTimeout(function () {
        if (Date.now() - t0 < 1900) {
          if (document.visibilityState === 'visible') {
            if (fallback) location.href = fallback;
            else toast('没装上这个 App？可以填个「备用链接」');
          }
        }
      }, 1500);
      return;
    }
    location.href = url;
  }

  /* ============================ 长按 = 右键 ============================ */
  var ringEl = $('ring'), ringFg = $('ringFg'), ringTimer = null;

  function showRing(x, y, ms) {
    ringEl.style.left = x + 'px';
    ringEl.style.top = y + 'px';
    ringEl.hidden = false;
    ringFg.style.transition = 'none';
    ringFg.style.strokeDashoffset = '119.4';
    void ringFg.getBoundingClientRect();
    ringFg.style.transition = 'stroke-dashoffset ' + ms + 'ms linear';
    ringFg.style.strokeDashoffset = '0';
    ringTimer = setTimeout(function () { ringEl.hidden = true; }, ms + 60);
  }
  function hideRing() {
    if (ringTimer) { clearTimeout(ringTimer); ringTimer = null; }
    ringEl.hidden = true;
  }

  function bindLongPress(el, onTap, onLong) {
    var timer = null, sx = 0, sy = 0, fired = false, moved = false, touched = false;
    var ms = Math.max(300, (set.longPressSec || 2) * 1000);

    function cancel() {
      if (timer) { clearTimeout(timer); timer = null; }
      hideRing();
      el.classList.remove('press');
    }

    el.addEventListener('touchstart', function (e) {
      if (e.touches.length !== 1) return;
      touched = true;
      var t = e.touches[0];
      sx = t.clientX; sy = t.clientY;
      fired = false; moved = false;
      el.classList.add('press');
      showRing(sx, sy, ms);
      timer = setTimeout(function () {
        timer = null;
        fired = true;
        hideRing();
        el.classList.remove('press');
        if (navigator.vibrate) { try { navigator.vibrate(20); } catch (err) { } }
        onLong(sx, sy);
      }, ms);
    }, { passive: true });

    el.addEventListener('touchmove', function (e) {
      var t = e.touches[0];
      if (!t) return;
      if (Math.abs(t.clientX - sx) > 12 || Math.abs(t.clientY - sy) > 12) { moved = true; cancel(); }
    }, { passive: true });

    el.addEventListener('touchend', cancel);
    el.addEventListener('touchcancel', cancel);

    el.addEventListener('click', function (e) {
      if (fired) { fired = false; e.preventDefault(); e.stopPropagation(); return; }
      if (moved) { moved = false; e.preventDefault(); e.stopPropagation(); return; }
      onTap();
    });

    el.addEventListener('contextmenu', function (e) {
      e.preventDefault();
      if (touched) return;              // 手机上已经用长按了
      onLong(e.clientX, e.clientY);
    });
  }

  /* ============================ 右键面板（液态玻璃） ============================ */
  var ctx = $('ctx');

  function closeCtx() {
    if (ctx.hidden) return;
    ctx.hidden = true;
    ctx.innerHTML = '';
  }

  function openCtx(x, y, title, items) {
    closeCtx();
    var html = '';
    if (title) html += '<div class="ctx-title">' + esc(title) + '</div>';
    items.forEach(function (it, i) {
      if (it.sep) { html += '<div class="ctx-sep"></div>'; return; }
      html += '<button class="ctx-item' + (it.danger ? ' danger' : '') + '" data-i="' + i + '">' +
        '<span class="em">' + it.em + '</span><span>' + esc(it.text) + '</span></button>';
    });
    ctx.innerHTML = html;
    ctx.classList.add('enter');
    ctx.hidden = false;

    var vw = window.innerWidth, vh = window.innerHeight;
    var w = ctx.offsetWidth, h = ctx.offsetHeight;
    var left = Math.min(Math.max(8, x - 18), vw - w - 8);
    var top = Math.min(Math.max(8, y - 10), vh - h - 8);
    ctx.style.left = left + 'px';
    ctx.style.top = top + 'px';

    // 先摆好位置，再让它淡进来
    void ctx.offsetWidth;
    ctx.classList.remove('enter');

    Array.prototype.forEach.call(ctx.querySelectorAll('.ctx-item'), function (b) {
      b.addEventListener('click', function () {
        var it = items[+b.getAttribute('data-i')];
        closeCtx();
        if (it && it.run) it.run();
      });
    });
  }

  function openCardMenu(idx, x, y) {
    var c = cards[idx];
    if (!c) return;
    openCtx(x, y, c.name, [
      { em: '🚀', text: '打开', run: function () { openCard(idx); } },
      { em: '📋', text: '复制链接', run: function () { copyText(c.url); } },
      { sep: true },
      { em: '✏️', text: '编辑', run: function () { openEdit(idx); } },
      { em: '⬆️', text: '移到最前', run: function () {
          cards.unshift(cards.splice(idx, 1)[0]); saveCards(); renderCards(); toast('已移到最前');
        } },
      { em: '🗑️', text: '删除', danger: true, run: function () { removeCard(idx); } }
    ]);
  }

  function removeCard(idx) {
    var c = cards[idx];
    if (!c) return;
    cards.splice(idx, 1);
    saveCards();
    renderCards();
    toast('已删除「' + c.name + '」');
  }

  function copyText(text) {
    if (!text) return;
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).then(function () { toast('已复制'); },
        function () { try { legacyCopy(text); toast('已复制'); } catch (e) { toast('复制失败，请手动长按选择'); } });
    } else {
      try { legacyCopy(text); toast('已复制'); } catch (e) { toast('复制失败，请手动长按选择'); }
    }
  }
  function legacyCopy(text) {
    var a = document.createElement('textarea');
    a.value = text;
    a.style.position = 'fixed';
    a.style.opacity = '0';
    document.body.appendChild(a);
    a.select();
    document.execCommand('copy');
    document.body.removeChild(a);
  }

  /* ============================ 底部面板 ============================ */
  var wrap = $('sheetWrap'), sheet = $('sheet'), sheetBody = $('sheetBody'),
      sheetTitle = $('sheetTitle'), sheetActions = $('sheetActions');

  var sheetOnClose = null;

  function openSheet(title, bodyHTML, actions, onClose) {
    sheetTitle.textContent = title;
    sheetBody.innerHTML = bodyHTML;
    sheetBody.scrollTop = 0;
    sheetOnClose = onClose || null;

    // 按钮区
    var old = sheet.querySelector('.sheet-actions');
    if (old) old.parentNode.removeChild(old);
    sheetActions = null;
    if (actions && actions.length) {
      var bar = document.createElement('div');
      bar.className = 'sheet-actions';
      actions.forEach(function (a, i) {
        var b = document.createElement('button');
        b.type = 'button';
        b.className = 'btn' + (a.kind ? ' ' + a.kind : '');
        b.textContent = a.text;
        b.setAttribute('data-a', i);
        b.addEventListener('click', function () { if (a.run) a.run(); });
        bar.appendChild(b);
      });
      sheet.appendChild(bar);
    }

    wrap.hidden = false;
    // 让浏览器先渲染初始位置，再加 show 触发动画
    void wrap.offsetWidth;
    wrap.classList.add('show');
  }

  function closeSheet() {
    if (wrap.hidden) return;
    wrap.classList.remove('show');

    var finished = false;
    function done() {
      if (finished) return;
      finished = true;
      sheet.removeEventListener('transitionend', onEnd);
      wrap.hidden = true;
      sheetBody.innerHTML = '';
      var bar = sheet.querySelector('.sheet-actions');
      if (bar) bar.parentNode.removeChild(bar);
      var cb = sheetOnClose;
      sheetOnClose = null;
      if (cb) cb();
    }
    function onEnd(e) {
      if (e.target === sheet && e.propertyName === 'transform') done();
    }
    sheet.addEventListener('transitionend', onEnd);
    setTimeout(done, 420);   // 兜底：动画被系统打断也能关掉，绝不卡住
  }

  $('sheetX').addEventListener('click', closeSheet);
  $('sheetMask').addEventListener('click', closeSheet);

  // 面板下拉关闭（只在下拉把手上拖，避免和内容滚动打架）
  (function () {
    var grip = $('sheetGrip');
    var head = sheet.querySelector('.sheet-head');
    var startY = 0, curY = 0, dragging = false;

    function down(e) {
      var t = e.touches ? e.touches[0] : e;
      dragging = true;
      startY = t.clientY;
      curY = 0;
      sheet.classList.add('dragging');
      sheet.style.transform = 'translate3d(0,0,0)';
    }
    function move(e) {
      if (!dragging) return;
      var t = e.touches ? e.touches[0] : e;
      curY = Math.max(0, t.clientY - startY);
      sheet.style.transform = 'translate3d(0,' + curY + 'px,0)';
      if (e.cancelable) e.preventDefault();
    }
    function up() {
      if (!dragging) return;
      dragging = false;
      sheet.classList.remove('dragging');
      sheet.style.transform = '';
      if (curY > sheet.offsetHeight * 0.28) closeSheet();
      curY = 0;
    }

    [grip, head].forEach(function (n) {
      if (!n) return;
      n.addEventListener('touchstart', down, { passive: true });
      n.addEventListener('touchmove', move, { passive: false });
      n.addEventListener('touchend', up);
      n.addEventListener('touchcancel', up);
      n.addEventListener('mousedown', down);
    });
    document.addEventListener('mousemove', move);
    document.addEventListener('mouseup', up);
  })();

  /* ============================ 编辑卡片 ============================ */
  function openEdit(idx) {
    var c = idx >= 0 ? cards[idx] : { name: '', url: '', note: '', fb: '', icon: '' };
    var isNew = idx < 0;

    var html =
      fld('fName', '名称', c.name, '例如：我常买的那家店', 'text') +
      fld('fUrl', '链接', c.url, 'https://… 或 App 协议 taobao://…', 'text') +
      fld('fNote', '备注', c.note, '一句话说明（可留空）', 'text') +
      fld('fIcon', '图标', c.icon, '填一个 emoji（可留空，默认用名字首字）', 'text') +
      fld('fFb', '备用链接', c.fb, '主链接打不开时自动改用它（可留空）', 'text');

    var actions = [];
    if (!isNew) actions.push({ text: '删除', kind: 'danger', run: function () {
      closeSheet();
      setTimeout(function () { removeCard(idx); }, 260);
    } });
    actions.push({ text: '取消', run: closeSheet });
    actions.push({ text: '保存', kind: 'primary', run: function () {
      var name = ($('fName').value || '').trim();
      var url = ($('fUrl').value || '').trim();
      if (!name) { toast('请填名称'); $('fName').focus(); return; }
      if (!url) { toast('请填链接'); $('fUrl').focus(); return; }
      var obj = {
        name: name, url: url,
        note: ($('fNote').value || '').trim(),
        icon: ($('fIcon').value || '').trim(),
        fb: ($('fFb').value || '').trim()
      };
      if (isNew) { cards.unshift(obj); toast('已添加「' + name + '」'); }
      else { cards[idx] = obj; toast('已保存'); }
      saveCards();
      renderCards();
      closeSheet();
    } });

    openSheet(isNew ? '添加一个入口' : '编辑入口', html, actions);
    setTimeout(function () {
      var n = $('fName');
      if (n && isNew) n.focus();
    }, 340);
  }

  function fld(id, label, val, ph, type) {
    return '<div class="field"><label for="' + id + '">' + esc(label) + '</label>' +
      '<input id="' + id + '" type="' + type + '" value="' + esc(val) + '" placeholder="' + esc(ph) + '" autocomplete="off" autocapitalize="off" spellcheck="false">' +
      '</div>';
  }

  /* ============================ 存剪贴板链接 ============================ */
  function addFromClipboard() {
    function use(text) {
      text = String(text || '').trim();
      if (!text) { toast('剪贴板里没有内容'); return; }
      var url = text;
      var m = text.match(/https?:\/\/[^\s"'<>）)】]+/i);
      if (m) url = m[0];
      var name = '来自剪贴板';
      try {
        var host = url.replace(/^https?:\/\//i, '').split('/')[0].replace(/^www\./, '');
        if (host && host.indexOf('.') > 0) name = host.split('.')[0];
      } catch (e) { }
      cards.unshift({ name: name, url: url, note: '剪贴板保存', fb: '', icon: '🔗' });
      saveCards();
      renderCards();
      toast('已存成卡片：' + name);
    }

    if (navigator.clipboard && navigator.clipboard.readText) {
      navigator.clipboard.readText().then(use, function () {
        manualPaste(use);
      });
    } else {
      manualPaste(use);
    }
  }

  function manualPaste(cb) {
    openSheet('粘贴链接',
      '<div class="field"><label for="fPaste">把链接粘在这里</label>' +
      '<input id="fPaste" type="url" placeholder="https://…" autocomplete="off" spellcheck="false"></div>' +
      '<div class="field"><div class="tip">有些手机浏览器不给读剪贴板，手动粘一下效果一样。</div></div>',
      [
        { text: '取消', run: closeSheet },
        { text: '存起来', kind: 'primary', run: function () {
            var v = ($('fPaste').value || '').trim();
            closeSheet();
            setTimeout(function () { cb(v); }, 280);
          } }
      ]);
  }

  /* ============================ 选图片 ============================ */
  function openImagePicker() {
    var list = allImages();
    var html = '<div class="media-grid" id="ig">';
    list.forEach(function (it, i) {
      html += '<button class="media-cell' + (it.custom ? ' custom' : '') + '" data-i="' + i + '">' +
        '<img src="' + esc(it.src) + '" alt="" loading="lazy">' +
        '<span class="tag">' + esc(it.title || '图片') + '</span></button>';
    });
    // 相册按钮
    html += '<button class="media-cell vid" id="ipick"><span class="ph">📱</span><span class="tag">从相册选</span></button>';
    html += '</div>';

    openSheet('选择背景图片', html, [
      { text: '恢复自动切换', run: function () {
          set.imgAuto = true;
          set.imgSrc = '';
          saveSet();
          var l = allImages();
          if (l.length) showImage(0, true);
          startImgTimer();
          updateHud();
          closeSheet();
          toast('已恢复自动切换（每 ' + set.autoSec + ' 秒换一张）');
        } },
      { text: '关闭', run: closeSheet }
    ]);

    setTimeout(function () {
      var g = $('ig');
      if (!g) return;
      Array.prototype.forEach.call(g.querySelectorAll('.media-cell[data-i]'), function (b) {
        b.addEventListener('click', function () {
          var i = +b.getAttribute('data-i');
          showImage(i, true);
          set.imgAuto = false;              // 自选后就不再自动切换
          saveSet();
          startImgTimer();
          updateHud();
          closeSheet();
          toast('已换成这张，自动切换已停');
        });
      });
      var ip = $('ipick');
      if (ip) ip.addEventListener('click', function () {
        // 必须同步触发，iPhone 上延时点击会不认
        $('fileImg').click();
        closeSheet();
      });
    }, 20);
  }

  function handlePickedImage(file) {
    if (!file) return;
    var fr = new FileReader();
    fr.onload = function () {
      var im = new Image();
      im.onload = function () {
        // 压到最长边 1600，存成 dataURL，重开也还在
        var max = 1600;
        var w = im.naturalWidth, h = im.naturalHeight;
        if (w > max || h > max) {
          var r = Math.min(max / w, max / h);
          w = Math.round(w * r); h = Math.round(h * r);
        }
        var cv = document.createElement('canvas');
        cv.width = w; cv.height = h;
        cv.getContext('2d').drawImage(im, 0, 0, w, h);
        var data = cv.toDataURL('image/jpeg', 0.85);
        customImg = data;
        store.set(STORE + '.customImg', data);
        set.imgAuto = false;
        saveSet();
        showImage(0, true);
        startImgTimer();
        updateHud();
        toast('已换成你选的图片，自动切换已停');
      };
      im.onerror = function () { toast('这张图读不了'); };
      im.src = fr.result;
    };
    fr.onerror = function () { toast('读取失败'); };
    fr.readAsDataURL(file);
  }

  /* ============================ 选视频 ============================ */
  function openVideoPicker() {
    var list = allVideos();
    var cur = set.videoSrc;
    var html = '<div class="media-grid" id="vg">';
    list.forEach(function (it, i) {
      html += '<button class="media-cell vid' + (it.src === cur ? ' on' : '') + '" data-i="' + i + '">' +
        '<span class="ph">🎬</span>' +
        '<span class="tag">' + esc(it.title || '视频') + '</span></button>';
    });
    html += '<button class="media-cell vid" id="vpick"><span class="ph">📱</span><span class="tag">从相册/文件选</span></button>';
    html += '</div>';
    if (list.length) {
      html += '<div class="field" style="margin-top:6px"><div class="tip">推荐选体积小的那个，手机上更流畅。' +
              (list[0] && list[0].size ? '（第一个 ' + esc(list[0].title) + ' 只有 ' + esc(list[0].size) + '）' : '') + '</div></div>';
    } else {
      html += '<div class="cards-empty">没找到视频。把视频放进「视频」文件夹，或者点下面的「从相册/文件选」。</div>';
    }

    openSheet('选择背景视频', html, [
      { text: '不用视频', run: function () {
          set.videoOn = false;
          saveSet();
          applyVideo();
          closeSheet();
          toast('已关掉视频，显示图片背景');
        } },
      { text: '关闭', run: closeSheet }
    ]);

    setTimeout(function () {
      var g = $('vg');
      if (!g) return;
      Array.prototype.forEach.call(g.querySelectorAll('.media-cell[data-i]'), function (b) {
        b.addEventListener('click', function () {
          var i = +b.getAttribute('data-i');
          var v = allVideos()[i];
          closeSheet();
          setTimeout(function () { setVideo(v.src); toast('正在播放：' + (v.title || '')); }, 240);
        });
      });
      var vp = $('vpick');
      if (vp) vp.addEventListener('click', function () {
        // 相册选的视频只在本次有效（太大，存不进手机本地存储）
        $('fileVid').click();
        closeSheet();
      });
    }, 20);
  }

  function handlePickedVideo(file) {
    if (!file) return;
    var url = URL.createObjectURL(file);
    var fake = { src: url, title: file.name.replace(/\.[^.]+$/, ''), size: (Math.round(file.size / 1048576) + ' MB') };
    BUNDLED.videos.push(fake);
    setVideo(url);
    toast('正在播放：' + fake.title + '（本次有效，想长期用请放进「视频」文件夹）');
  }

  /* ============================ 设置面板 ============================ */
  function openSettings() {
    var html = '';
    html += rowSwitch('sAuto', '🔁', '图片自动切换', '每 ' + set.autoSec + ' 秒换一张', set.imgAuto);
    html += row('sSec', '⏱️', '切换间隔', set.autoSec + ' 秒', '');
    html += rowSwitch('sVid', '🎬', '视频背景', set.videoOn ? '正在播放' : '已关闭', set.videoOn);
    html += row('sPickV', '📼', '选择视频', '', set.videoSrc ? '已选' : '未选');
    html += row('sLp', '👆', '长按判定（=右键）', set.longPressSec + ' 秒', '');
    html += rowSwitch('sLite', '⚡', '省电模式', '关掉毛玻璃，老手机更流畅', set.lite);
    html += rowSwitch('sGlass', '✨', '卡片也用液态玻璃', '更漂亮，但吃性能', !!set.glassCards);
    html += '<div class="ctx-sep" style="margin:10px 4px"></div>';
    html += row('sExp', '📤', '导出我的入口', '生成一段文字，方便备份', '');
    html += row('sImp', '📥', '导入入口', '粘贴之前导出的文字', '');
    html += row('sReset', '♻️', '恢复默认 12 个入口', '', '');
    html += '<div class="field" style="margin-top:10px"><div class="tip">' +
      (store.usable ? '数据保存在这台手机的浏览器里（localStorage）。' :
        '⚠️ 当前打开方式不允许保存数据（比如直接双击文件打开）。这次改动关掉页面就没了，建议用手机浏览器「添加到主屏幕」后再用。') +
      '</div></div>';

    openSheet('设置', html, [{ text: '完成', kind: 'primary', run: closeSheet }]);

    setTimeout(function () {
      bindSwitch('sAuto', function (v) { set.imgAuto = v; saveSet(); startImgTimer(); updateHud(); return '图片自动切换'; });
      bindSwitch('sVid', function (v) {
        set.videoOn = v;
        saveSet();
        applyVideo();
        return '视频背景';
      });
      bindSwitch('sLite', function (v) { set.lite = v; saveSet(); applyLite(); return '省电模式'; });
      bindSwitch('sGlass', function (v) {
        set.glassCards = v; saveSet(); renderCards(); return '卡片液态玻璃';
      });

      on('sSec', function () {
        cycleSec();
      });
      on('sPickV', function () { closeSheet(); setTimeout(openVideoPicker, 280); });
      on('sLp', function () { cycleLongPress(); });
      on('sExp', doExport);
      on('sImp', doImport);
      on('sReset', function () {
        cards = DEFAULT_CARDS.map(function (c) {
          return { name: c.name, url: c.url, note: c.note, fb: c.fb, icon: '' };
        });
        saveCards(); renderCards(); closeSheet(); toast('已恢复默认入口');
      });
    }, 20);
  }

  function on(id, fn) {
    var el = $(id);
    if (el) el.addEventListener('click', fn);
  }

  function row(id, em, title, sub, val) {
    return '<button class="row" id="' + id + '">' +
      '<span class="em">' + em + '</span>' +
      '<span class="rb"><span class="rt">' + esc(title) + '</span>' +
      (sub ? '<span class="rs">' + esc(sub) + '</span>' : '') + '</span>' +
      (val ? '<span class="rv">' + esc(val) + '</span>' : '<span class="rv">›</span>') +
      '</button>';
  }
  function rowSwitch(id, em, title, sub, on_) {
    return '<button class="row' + (on_ ? ' on' : '') + '" id="' + id + '">' +
      '<span class="em">' + em + '</span>' +
      '<span class="rb"><span class="rt">' + esc(title) + '</span>' +
      (sub ? '<span class="rs">' + esc(sub) + '</span>' : '') + '</span>' +
      '<span class="sw"></span></button>';
  }
  function bindSwitch(id, fn) {
    var el = $(id);
    if (!el) return;
    el.addEventListener('click', function () {
      var now = !el.classList.contains('on');
      el.classList.toggle('on', now);
      var label = fn(now);
      toast(label + (now ? ' 已打开' : ' 已关闭'));
      // 重画设置面板里的说明文字
      if (id === 'sVid') {
        var sub = el.querySelector('.rs');
        if (sub) sub.textContent = now ? '正在播放' : '已关闭';
      }
    });
  }

  function cycleSec() {
    var opts = [10, 20, 30, 60, 120, 300];
    var i = opts.indexOf(set.autoSec);
    set.autoSec = opts[(i + 1) % opts.length];
    saveSet(); startImgTimer(); updateHud();
    var el = $('sSec');
    if (el) el.querySelector('.rv').textContent = set.autoSec + ' 秒';
    var sub = $('sAuto') && $('sAuto').querySelector('.rs');
    if (sub) sub.textContent = '每 ' + set.autoSec + ' 秒换一张';
    toast('切换间隔改成 ' + set.autoSec + ' 秒');
  }

  function cycleLongPress() {
    var opts = [0.5, 0.8, 1, 2, 3];
    var i = opts.indexOf(set.longPressSec);
    set.longPressSec = opts[(i + 1) % opts.length];
    saveSet(); updateHint(); renderCards();
    var el = $('sLp');
    if (el) el.querySelector('.rv').textContent = set.longPressSec + ' 秒';
    toast('长按判定改成 ' + set.longPressSec + ' 秒');
  }

  function doExport() {
    var data = JSON.stringify({ v: 1, cards: cards }, null, 1);
    copyText(data);
    toast('已复制到剪贴板（' + cards.length + ' 个入口）', 2600);
  }

  function doImport() {
    openSheet('导入入口',
      '<div class="field"><label for="fImp">把导出的文字粘进来</label>' +
      '<input id="fImp" type="text" placeholder=\'{"v":1,"cards":[...]}\' autocomplete="off" spellcheck="false"></div>' +
      '<div class="field"><div class="tip">粘贴后点「导入」，会合并到现有列表里。</div></div>',
      [
        { text: '取消', run: closeSheet },
        { text: '导入', kind: 'primary', run: function () {
            var v = ($('fImp').value || '').trim();
            if (!v) { toast('还没粘内容'); return; }
            try {
              var o = JSON.parse(v);
              var arr = o && o.cards ? o.cards : (Array.isArray(o) ? o : null);
              if (!arr) throw new Error('格式不对');
              var added = 0;
              arr.forEach(function (c) {
                if (c && c.name && c.url) {
                  cards.unshift({ name: String(c.name), url: String(c.url), note: String(c.note || ''), icon: String(c.icon || ''), fb: String(c.fb || '') });
                  added++;
                }
              });
              saveCards(); renderCards(); closeSheet();
              toast('导入了 ' + added + ' 个入口');
            } catch (e) { toast('这段内容读不出来'); }
          } }
      ]);
  }

  function applyLite() {
    document.body.classList.toggle('lite', !!set.lite);
  }

  /* ============================ 快捷按钮 ============================ */
  $('btnAdd').addEventListener('click', function () { openEdit(-1); });
  $('btnClip').addEventListener('click', addFromClipboard);
  $('btnPickImg').addEventListener('click', function () {
    if (customImg) {
      // 有自选图时，先点一下问「继续换还是恢复内置」
      openCtx(window.innerWidth / 2 - 90, 150, '背景图片', [
        { em: '📱', text: '再从相册选一张', run: function () { $('fileImg').click(); } },
        { em: '🖼', text: '回到内置图片', run: function () {
            customImg = '';
            store.set(STORE + '.customImg', '');
            set.imgSrc = '';
            saveSet();
            var l = allImages();
            if (l.length) showImage(0, true);
            updateHud();
            toast('已回到内置图片');
          } },
        { em: '📂', text: '图片列表', run: function () { setTimeout(openImagePicker, 80); } }
      ]);
      return;
    }
    openImagePicker();
  });

  $('btnAuto').addEventListener('click', function () {
    var n = allImages().length;
    if (!n) { toast('没有图片，先点「🖼 选图」'); return; }
    set.imgAuto = !set.imgAuto;
    saveSet();
    startImgTimer();
    updateHud();
    toast(set.imgAuto ? '自动切换已开：每 ' + set.autoSec + ' 秒换一张' : '自动切换已关');
  });

  $('btnVideo').addEventListener('click', toggleVideo);
  $('btnMenu').addEventListener('click', openSettings);

  // 长按这两个按钮 = 右键
  function longPressOnly(el, fn, title) {
    var timer = null, sx = 0, sy = 0, touched = false;
    el.addEventListener('touchstart', function (e) {
      if (e.touches.length !== 1) return;
      touched = true;
      var t = e.touches[0];
      sx = t.clientX; sy = t.clientY;
      showRing(sx, sy, (set.longPressSec || 2) * 1000);
      timer = setTimeout(function () {
        timer = null; hideRing();
        if (navigator.vibrate) { try { navigator.vibrate(20); } catch (err) { } }
        fn(sx, sy);
      }, (set.longPressSec || 2) * 1000);
    }, { passive: true });
    el.addEventListener('touchmove', function (e) {
      var t = e.touches[0];
      if (!t) return;
      if (Math.abs(t.clientX - sx) > 12 || Math.abs(t.clientY - sy) > 12) { if (timer) clearTimeout(timer); hideRing(); }
    }, { passive: true });
    el.addEventListener('touchend', function () { if (timer) clearTimeout(timer); hideRing(); });
    el.addEventListener('contextmenu', function (e) {
      e.preventDefault();
      if (!touched) fn(e.clientX, e.clientY);
    });
  }

  longPressOnly($('btnVideo'), function (x, y) {
    openCtx(x, y, '视频背景', [
      { em: '📼', text: '选择视频', run: function () { setTimeout(openVideoPicker, 80); } },
      { em: '⏸', text: '不用视频', run: function () {
          set.videoOn = false; saveSet(); applyVideo(); toast('已关掉视频');
        } }
    ]);
  });

  longPressOnly($('btnAuto'), function (x, y) {
    openCtx(x, y, '自动切换', [
      { em: '⏭', text: '立刻换下一张', run: nextImage },
      { em: '🖼', text: '选择背景图片', run: function () { setTimeout(openImagePicker, 80); } },
      { em: '⏱', text: '切换间隔…', run: function () { setTimeout(openSettings, 80); } }
    ]);
  });

  // 顶部长按 = 更多
  longPressOnly($('brandIco'), function (x, y) {
    openCtx(x, y, '莫印泯', [
      { em: '⚙️', text: '设置', run: function () { setTimeout(openSettings, 80); } },
      { em: '⏭', text: '换一张背景', run: nextImage },
      { em: '📋', text: '存剪贴板链接', run: addFromClipboard }
    ]);
  });

  /* ============================ 文件选择 ============================ */
  $('fileImg').addEventListener('change', function (e) {
    var f = e.target.files && e.target.files[0];
    e.target.value = '';
    handlePickedImage(f);
  });
  $('fileVid').addEventListener('change', function (e) {
    var f = e.target.files && e.target.files[0];
    e.target.value = '';
    handlePickedVideo(f);
  });

  /* ============================ 其它 ============================ */
  // 点空白关掉浮层菜单
  document.addEventListener('touchstart', function (e) {
    if (!ctx.hidden && !ctx.contains(e.target)) closeCtx();
  }, { passive: true });
  document.addEventListener('mousedown', function (e) {
    if (!ctx.hidden && !ctx.contains(e.target)) closeCtx();
  });
  document.addEventListener('scroll', closeCtx, true);
  window.addEventListener('resize', closeCtx);
  window.addEventListener('blur', closeCtx);

  document.addEventListener('gesturestart', function (e) { e.preventDefault(); });

  // 视频加载失败 → 提示
  vid.addEventListener('error', function () {
    if (set.videoOn) toast('这个视频播不了，换一个试试');
  });

  // 从后台回来时续播
  document.addEventListener('visibilitychange', function () {
    if (document.visibilityState === 'visible' && set.videoOn) {
      var pr = vid.play();
      if (pr && pr.catch) pr.catch(function () { });
    }
  });

  /* ============================ 启动 ============================ */
  function boot() {
    applyLite();
    var l = allImages();
    var idx = pickImageIndex();
    if (l.length) showImage(Math.max(0, idx), false);
    applyVideo();
    startImgTimer();
    renderCards();
    updateHud();

    if (!store.usable) {
      setTimeout(function () {
        toast('提示：当前打开方式不能保存数据，建议用浏览器打开或用「添加到主屏幕」', 4200);
      }, 1200);
    }
  }

  boot();

  // PWA
  if ('serviceWorker' in navigator && location.protocol !== 'file:') {
    window.addEventListener('load', function () {
      navigator.serviceWorker.register('sw.js').catch(function () { });
    });
  }
})();
