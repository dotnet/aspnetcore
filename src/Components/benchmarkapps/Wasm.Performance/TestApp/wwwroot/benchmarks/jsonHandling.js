import { group, benchmark, setup, teardown } from './lib/minibench/minibench.js';
import { BlazorApp } from './util/BlazorApp.js';
import { receiveEvent } from './util/BenchmarkEvents.js';
import { setInputValue } from './util/DOM.js';
import { largeJsonToDeserialize, largeObjectToSerialize, benchmarkJson } from './jsonHandlingData.js';

group('JSON handling', () => {
  let app;

  setup(async () => {
    app = new BlazorApp();
    await app.start();
    app.navigateTo('json');
  });

  teardown(() => app.dispose());

  benchmark('Serialize 1kb', () =>
    benchmarkJson(app, '#serialize-small', '#serialized-length', 935), {
      descriptor: {
        name: 'blazorwasm/jsonserialize-1kb',
        description: 'Serialize JSON 1kb - Time in ms'
      }
    });

  benchmark('Serialize 340kb', () =>
    benchmarkJson(app, '#serialize-large', '#serialized-length', 339803), {
      descriptor: {
        name: 'blazorwasm/jsonserialize-340kb',
        description: 'Serialize JSON 340kb - Time in ms'
      }
    });

  benchmark('Serialize 340kb (Source Generated)', () =>
    benchmarkJson(app, '#serialize-large-sourcegen', '#serialized-length', 339803), {
      descriptor: {
        name: 'blazorwasm/jsonserialize-sourcegen-340kb',
        description: 'Serialize JSON (SourceGen) 340kb - Time in ms'
      }
    });

  benchmark('Deserialize 1kb', () =>
    benchmarkJson(app, '#deserialize-small', '#deserialized-count', 5), {
      descriptor: {
        name: 'blazorwasm/jsondeserialize-1kb',
        description: 'Deserialize JSON 1kb - Time in ms'
      }
    });

  benchmark('Deserialize 340kb', () =>
    benchmarkJson(app, '#deserialize-large', '#deserialized-count', 1365), {
      descriptor: {
        name: 'blazorwasm/jsondeserialize-340kb',
        description: 'Deserialize JSON 340kb - Time in ms'
      }
    });

  benchmark('Deserialize 340kb (Source Generated)', () =>
    benchmarkJson(app, '#deserialize-large-sourcegen', '#deserialized-count', 1365), {
      descriptor: {
        name: 'blazorwasm/jsondeserialize-sourcegen-340kb',
        description: 'Deserialize JSON (SourceGen) 340kb - Time in ms'
      }
    });

  benchmark('Serialize 340kb (JavaScript)', () => {
    const json = JSON.stringify(largeObjectToSerialize);
    if (json.length !== 339803) {
      throw new Error(`Incorrect length: ${json.length}`);
    }
  }, {
    descriptor: {
      name: 'blazorwasm/jsonserialize-javascript-340kb',
      description: 'Serialize JSON 340kb using JavaScript - Time in ms'
    }
  });

  benchmark('Deserialize 340kb (JavaScript)', () => {
    const parsed = JSON.parse(largeJsonToDeserialize);
    if (parsed.name !== 'CEO - Subordinate 0') {
      throw new Error('Incorrect result');
    }
  }, {
    descriptor: {
      name: 'blazorwasm/jsondeserialize-javascript-340kb',
      description: 'Deserialize JSON 340kb using JavaScript - Time in ms'
    }
  });
});
