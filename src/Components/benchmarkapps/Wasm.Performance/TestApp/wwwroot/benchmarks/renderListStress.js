import { group, setup, benchmark } from './lib/minibench/minibench.js';
import { BlazorStressApp } from './util/BlazorStressApp.js';
import { measureRenderList } from './renderListBenchmark.js';

group('Rendering list', () => {
  let app;

  setup(() => {
    app = BlazorStressApp.instance;
    app.navigateTo('renderList');
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
