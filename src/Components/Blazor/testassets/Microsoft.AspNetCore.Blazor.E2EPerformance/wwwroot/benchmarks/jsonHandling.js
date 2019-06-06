import { group, benchmark, setup, teardown } from './lib/minibench/minibench.js';
import { BlazorApp } from './util/BlazorApp.js';
import { receiveEvent } from './util/BenchmarkEvents.js';
import { setInputValue } from './util/DOM.js';
import { largeJsonToDeserialize, largeObjectToSerialize } from './jsonHandlingData.js';

group('JSON handling', () => {
  let app;

  setup(async () => {
    app = new BlazorApp();
    await app.start();
    app.navigateTo('json');
  });

  teardown(() => app.dispose());

  benchmark('Serialize 1kb', () =>
    benchmarkJson(app, '#serialize-small', '#serialized-length', 935));

  benchmark('Serialize 340kb', () =>
    benchmarkJson(app, '#serialize-large', '#serialized-length', 339803));

  benchmark('Deserialize 1kb', () =>
    benchmarkJson(app, '#deserialize-small', '#deserialized-count', 5));

  benchmark('Deserialize 340kb', () =>
    benchmarkJson(app, '#deserialize-large', '#deserialized-count', 1365));

  benchmark('Serialize 340kb (JavaScript)', () => {
    const json = JSON.stringify(largeObjectToSerialize);
    if (json.length !== 339803) {
      throw new Error(`Incorrect length: ${json.length}`);
    }
  });

  benchmark('Deserialize 340kb (JavaScript)', () => {
    const parsed = JSON.parse(largeJsonToDeserialize);
    if (parsed.name !== 'CEO - Subordinate 0') {
      throw new Error('Incorrect result');
    }
  });
});

async function benchmarkJson(app, buttonSelector, resultSelector, expectedResult) {
  const appDocument = app.window.document;
  appDocument.querySelector('#reset-all').click();

  let nextRenderCompletion = receiveEvent('Finished JSON processing');
  appDocument.querySelector(buttonSelector).click();
  await nextRenderCompletion;

  const resultElem = appDocument.querySelector(resultSelector);
  if (resultElem.textContent != expectedResult.toString()) {
    throw new Error(`Incorrect result: ${resultElem.textContent}`);
  }
}
