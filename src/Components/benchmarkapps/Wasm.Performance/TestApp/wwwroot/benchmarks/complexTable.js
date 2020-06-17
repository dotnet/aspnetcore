import { group, benchmark, setup, teardown } from './lib/minibench/minibench.js';
import { receiveEvent } from './util/BenchmarkEvents.js';
import { BlazorApp } from './util/BlazorApp.js';

group('Complex table', () => {
  let app;

  setup(async () => {
    app = new BlazorApp();
    await app.start();
    app.navigateTo('complextable');
  });

  teardown(() => {
    app.dispose();
  });

  benchmark('Initial render from blank', () => measureRenderComplexTableFromBlank(app), {
    descriptor: {
      name: 'blazorwasm/render-complex-table-from-blank',
      description: 'Time to render complex table from blank (ms)'
    }
  });

  benchmark('Switch pages', () => measureRenderComplexTableSwitchPages(app), {
    setup: async function() {
      let nextRenderCompletion = receiveEvent('Finished rendering table');
      app.window.document.querySelector('#show').click();
      await nextRenderCompletion;
    },
    descriptor: {
      name: 'blazorwasm/render-complex-table-switch-pages',
      description: 'Time to render change of page (ms)'
    }
  });
});

async function measureRenderComplexTableFromBlank(app) {
    const appDocument = app.window.document;

    let nextRenderCompletion = receiveEvent('Finished rendering table');
    appDocument.querySelector('#hide').click();
    await nextRenderCompletion;

    if (appDocument.querySelectorAll('tr.complex-table-row').length !== 0) {
        throw new Error('Wrong number of rows rendered');
    }

    nextRenderCompletion = receiveEvent('Finished rendering table');
    appDocument.querySelector('#show').click();
    await nextRenderCompletion;

    if (appDocument.querySelectorAll('tr.complex-table-row').length !== 200) {
        throw new Error('Wrong number of rows rendered');
    }
}

async function measureRenderComplexTableSwitchPages(app) {
    const appDocument = app.window.document;

    let nextRenderCompletion = receiveEvent('Finished rendering table');
    appDocument.querySelector('#change-page').click();
    await nextRenderCompletion;
}
