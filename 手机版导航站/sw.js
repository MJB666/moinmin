/* 莫印泯 · 手机版 Service Worker
   只缓存「程序本身」这几个小文件，视频和大图不缓存（太占地方）。
   只有在 http/https 下才生效；直接双击 index.html（file://）不会注册，也不影响使用。 */

var CACHE = 'moin-core-v1';
var CORE = [
  'index.html',
  'style.css',
  'app.js',
  'media.js',
  'manifest.json',
  'icon-192.png',
  'icon-512.png'
];

self.addEventListener('install', function (e) {
  e.waitUntil(
    caches.open(CACHE).then(function (c) {
      return Promise.all(CORE.map(function (u) {
        return c.add(u).catch(function () { /* 少一个不影响 */ });
      }));
    }).then(function () { return self.skipWaiting(); })
  );
});

self.addEventListener('activate', function (e) {
  e.waitUntil(
    caches.keys().then(function (keys) {
      return Promise.all(keys.map(function (k) {
        return k === CACHE ? null : caches.delete(k);
      }));
    }).then(function () { return self.clients.claim(); })
  );
});

self.addEventListener('fetch', function (e) {
  if (e.request.method !== 'GET') return;
  e.respondWith(
    caches.match(e.request).then(function (hit) {
      return hit || fetch(e.request);
    })
  );
});
