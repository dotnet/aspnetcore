// Caution! Be sure you understand the caveats before publishing an application with
// offline support. See https://aka.ms/blazor-offline-considerations
//
// This service worker implements a cache-first policy. For offline support to work,
// you must ensure that 'resourcesAvailableOffline' lists all the resources needed
// by your application (except for .NET assemblies and dotnet.wasm).

const resourcesAvailableOffline = [
    'index.html',
    'manifest.json',
    'icon-512.png',
    '_framework/blazor.boot.json',
    '_framework/blazor.webassembly.js',
    '_framework/wasm/dotnet.js',
    'css/site.css',
    'css/bootstrap/bootstrap.min.css',
    'css/open-iconic/font/css/open-iconic-bootstrap.min.css',
    'css/open-iconic/font/fonts/open-iconic.woff'
];

const cacheNamePrefix = 'offline-cache-';
const cacheName = () => `${cacheNamePrefix}${serviceWorkerVersion}`;

self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

async function onInstall(event) {
    console.info('Service worker: Install');
    const cache = await caches.open(cacheName());
    await cache.addAll(resourcesAvailableOffline);
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // Delete unused caches
    const cacheKeys = await caches.keys();
    return Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName())
        .map(key => caches.delete(key)));
}

async function onFetch(event) {
    let cachedResponse = null;
    if (event.request.method === 'GET') {
        // For all navigation requests, try to serve index.html from cache
        // If you need some URLs to be server-rendered, edit the following check to exclude those URLs
        const shouldServeIndexHtml = event.request.mode === 'navigate';

        const request = shouldServeIndexHtml ? 'index.html' : event.request;
        const cache = await caches.open(cacheName());
        cachedResponse = await cache.match(request);
    }

    return cachedResponse || fetch(event.request);
}
