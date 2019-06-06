import { group, benchmark } from './lib/minibench/minibench.js';
import { BlazorApp } from './util/BlazorApp.js';

group('App Startup', () => {

  benchmark('Time to first UI', async () => {
    const app = new BlazorApp();
    try {
      await app.start();
    } finally {
      app.dispose();
    }
  });

});
