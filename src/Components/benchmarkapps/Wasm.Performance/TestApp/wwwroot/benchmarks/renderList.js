import { group, benchmark, setup, teardown } from './lib/minibench/minibench.js';
import { BlazorApp } from './util/BlazorApp.js';
import { measureRenderList } from './renderListBenchmark.js';

group('Rendering list', () => {
  let app;

  setup(async () => {
    app = new BlazorApp();
    await app.start();
    app.navigateTo('renderList');
  });

  teardown(() => {
    app.dispose();
  });

  benchmark('Render 10 items', () => measureRenderList(app, 10), {
    descriptor: {
      name: 'blazorwasm/render-10-items',
      description: 'Time to render 10 item list (ms)'
    }
  });
  benchmark('Render 100 items', () => measureRenderList(app, 100), {
    descriptor: {
      name: 'blazorwasm/render-100-items',
      description: 'Time to render 100 item list (ms)'
    }
  });
  benchmark('Render 1000 items', () => measureRenderList(app, 1000), {
    descriptor: {
      name: 'blazorwasm/render-1000-items',
      description: 'Time to render 1000 item list (ms)'
    }
  });
});
