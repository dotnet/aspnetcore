import { receiveEvent } from './util/BenchmarkEvents.js';
import { setInputValue } from './util/DOM.js';

export async function measureRenderList(app, numItems) {
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

