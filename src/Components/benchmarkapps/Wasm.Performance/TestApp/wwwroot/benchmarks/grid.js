import { group, benchmark, setup, teardown } from './lib/minibench/minibench.js';
import { receiveEvent } from './util/BenchmarkEvents.js';
import { BlazorApp } from './util/BlazorApp.js';
import { setInputValue } from './util/DOM.js';

group('Grid', () => {
  let app;

  setup(async () => {
    app = new BlazorApp();
    await app.start();
    app.navigateTo('gridrendering');
  });

  teardown(() => {
    app.dispose();
  });

  benchmark('ComplexTable: From blank', () => measureRenderGridFromBlank(app), {
    setup: () => prepare(app, 'ComplexTable', false),
    descriptor: {
      name: 'blazorwasm/render-complextable-from-blank',
      description: 'Time to render complex table from blank (ms)'
    }
  });

  benchmark('ComplexTable: Switch pages', () => measureRenderGridSwitchPages(app), {
    setup: () => prepare(app, 'ComplexTable', true),
    descriptor: {
      name: 'blazorwasm/render-complextable-switch-pages',
      description: 'Time to render complex table change of page (ms)'
    }
  });

  benchmark('FastGrid: From blank', () => measureRenderGridFromBlank(app), {
    setup: () => prepare(app, 'FastGrid', false),
    descriptor: {
      name: 'blazorwasm/render-fastgrid-from-blank',
      description: 'Time to render fast grid from blank (ms)'
    }
  });

  benchmark('FastGrid: Switch pages', () => measureRenderGridSwitchPages(app), {
    setup: () => prepare(app, 'FastGrid', true),
    descriptor: {
      name: 'blazorwasm/render-fastgrid-switch-pages',
      description: 'Time to render fast grid change of page (ms)'
    }
  });
});

async function prepare(app, renderMode, populateTable) {
  const renderModeSelect = app.window.document.querySelector('#render-mode');
  setInputValue(renderModeSelect, renderMode);

  if (populateTable) {
    let nextRenderCompletion = receiveEvent('Finished rendering table');
    app.window.document.querySelector(populateTable ? '#show' : '#hide').click();
    await nextRenderCompletion;
  }
}

async function measureRenderGridFromBlank(app) {
    const appDocument = app.window.document;

    let nextRenderCompletion = receiveEvent('Finished rendering table');
    appDocument.querySelector('#hide').click();
    await nextRenderCompletion;

    if (appDocument.querySelectorAll('tbody tr').length !== 0) {
        throw new Error('Wrong number of rows rendered');
    }

    nextRenderCompletion = receiveEvent('Finished rendering table');
    appDocument.querySelector('#show').click();
    await nextRenderCompletion;

    if (appDocument.querySelectorAll('tbody tr').length !== 200) {
        throw new Error('Wrong number of rows rendered');
    }
}

async function measureRenderGridSwitchPages(app) {
    const appDocument = app.window.document;

    let nextRenderCompletion = receiveEvent('Finished rendering table');
    appDocument.querySelector('#change-page').click();
    await nextRenderCompletion;
}
