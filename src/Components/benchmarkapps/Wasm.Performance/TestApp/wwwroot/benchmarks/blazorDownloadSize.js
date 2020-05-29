import { BlazorApp } from './util/BlazorApp.js';

export async function getBlazorDownloadSize() {
  // Clear caches
  for (var key of await caches.keys()) {
    await caches.delete(key);
  }

  const app = new BlazorApp();
  try {
    await app.start();
    const downloadSize = app.window.performance.getEntries().reduce((prev, next) => (next.encodedBodySize || 0) + prev, 0);
    return downloadSize;
  } finally {
    app.dispose();
  }
}
