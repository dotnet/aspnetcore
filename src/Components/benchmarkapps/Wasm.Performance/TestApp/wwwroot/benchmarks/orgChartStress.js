import { group, benchmark, setup, teardown } from './lib/minibench/minibench.js';
import { BlazorStressApp } from './util/BlazorStressApp.js';
import { measureOrgChart, measureOrgChartEdit } from './orgChartBenchmark.js';

group('Nested components', () => {
  let app;

  setup(() => {
    app = BlazorStressApp.instance;
    app.navigateTo('orgChart');
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

