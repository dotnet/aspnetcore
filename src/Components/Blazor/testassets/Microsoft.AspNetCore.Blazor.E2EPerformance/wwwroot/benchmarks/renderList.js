import { group, benchmark, setup, teardown } from './lib/minibench/minibench.js';
import { BlazorApp } from './util/BlazorApp.js';
import { receiveEvent } from './util/BenchmarkEvents.js';
import { setInputValue } from './util/DOM.js';

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

  benchmark('Render 10 items', () => measureRenderList(app, 10));
  benchmark('Render 100 items', () => measureRenderList(app, 100));
  benchmark('Render 1000 items', () => measureRenderList(app, 1000));

});

async function measureRenderList(app, numItems) {
  const appDocument = app.window.document;
  const numItemsTextbox = appDocument.querySelector('#num-items');
  setInputValue(numItemsTextbox, numItems.toString());

  let nextRenderCompletion = receiveEvent('Finished rendering list');
  appDocument.querySelector('#hide-list').click();
  await nextRenderCompletion;

  if (appDocument.querySelectorAll('tbody tr').length !== 0) {
    throw new Error('Wrong number of items rendered');
  }

  nextRenderCompletion = receiveEvent('Finished rendering list');
  appDocument.querySelector('#show-list').click();
  await nextRenderCompletion;

  if (appDocument.querySelectorAll('tbody tr').length !== numItems) {
    throw new Error('Wrong number of items rendered');
  }
}
