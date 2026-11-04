import { group, benchmark, setup, teardown } from './lib/minibench/minibench.js';
import { BlazorApp } from './util/BlazorApp.js';
import { measureOrgChart, measureOrgChartEdit } from './orgChartBenchmark.js';

group('Nested components', () => {
  let app;

  setup(async () => {
    app = new BlazorApp();
    await app.start();
    app.navigateTo('orgChart');
  });

  teardown(() => {
    app.dispose();
  });

  benchmark('Render small nested component', () => measureOrgChart(app, 1, 4), {
    descriptor: {
      name: 'blazorwasm/orgchart-1-4-org',
      description: 'Time to render a complex component with small nesting (ms)'
    }
  });
  benchmark('Render large nested component', () => measureOrgChart(app, 3, 3), {
    descriptor: {
      name: 'blazorwasm/orgchart-3-3-org',
      description: 'Time to render a complex component with large nesting (ms)'
    }
  });
  benchmark('Render component with edit', () => measureOrgChartEdit(app, 3, 2), {
    descriptor: {
      name: 'blazorwasm/edit-orgchart-3-2',
      description: 'Time to peform updates in a nested component (ms)'
    }
  });
});

