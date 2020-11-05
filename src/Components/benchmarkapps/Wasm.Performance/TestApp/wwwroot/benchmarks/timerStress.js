import { group, benchmark, setup, teardown } from './lib/minibench/minibench.js';
import { BlazorApp } from './util/BlazorApp.js';
import { receiveEvent } from './util/BenchmarkEvents.js';

group('Navigation', () => {
  let app;

  setup(async () => {
    app = new BlazorApp();
    await app.start();
  });

  teardown(() => app.dispose());

  // Timers tend to make for good stress scenarios in helping identify memory leaks / use-after-dispose etc.
  // While benchmarking it isn't super useful, we'll use it to keep with the theme.
  benchmark('Timer', () =>
    benchmarkNavigation(app), {
    descriptor: {
      name: 'blazorwasm/timer',
      description: 'Timers - Time in ms'
    }
  });
});

async function benchmarkNavigation(app) {
  for (let i = 0; i < 3; i++) {
    const nextCompletion = receiveEvent('Finished updating color');
    app.navigateTo('timer');
    await nextCompletion;
  }
}
