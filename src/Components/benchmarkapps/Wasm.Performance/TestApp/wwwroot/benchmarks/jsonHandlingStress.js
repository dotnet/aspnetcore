import { group, benchmark, setup, teardown } from './lib/minibench/minibench.js';
import { benchmarkJson} from './jsonHandlingData.js';
import { BlazorStressApp } from './util/BlazorStressApp.js';

group('JSON handling', () => {
  let app;

  setup(() => {
    app = BlazorStressApp.instance;
    app.navigateTo('json');
  });

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
});
