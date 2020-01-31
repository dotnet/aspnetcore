import { group, benchmark, setup, teardown } from './lib/minibench/minibench.js';
import { BlazorApp } from './util/BlazorApp.js';
import { receiveEvent } from './util/BenchmarkEvents.js';
import { setInputValue } from './util/DOM.js';

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

async function measureOrgChart(app, depth, subs) {
  const appDocument = app.window.document;
  setInputValue(appDocument.querySelector('#depth'), depth.toString());
  setInputValue(appDocument.querySelector('#subs'), subs.toString());

  let nextRenderCompletion = receiveEvent('Finished OrgChart rendering');
  appDocument.querySelector('#hide').click();
  await nextRenderCompletion;

  if (appDocument.querySelectorAll('h2').length !== 0) {
    throw new Error('Wrong number of items rendered');
  }

  nextRenderCompletion = receiveEvent('Finished OrgChart rendering');
  appDocument.querySelector('#show').click();
  await nextRenderCompletion;

  if (appDocument.querySelectorAll('h2').length < depth * subs) {
    throw new Error('Wrong number of items rendered');
  }
}

async function measureOrgChartEdit(app, depth, subs) {
  const appDocument = app.window.document;
  setInputValue(appDocument.querySelector('#depth'), depth.toString());
  setInputValue(appDocument.querySelector('#subs'), subs.toString());

  let nextRenderCompletion = receiveEvent('Finished OrgChart rendering');
  appDocument.querySelector('#show').click();
  await nextRenderCompletion;

  const elements = appDocument.querySelectorAll('.person');
  if (!elements) {
    throw new Error("No person elements found.");
  }

  const personElement = elements.item(elements.length / 2);

  const display = personElement.querySelector('.salary');
  const input = personElement.querySelector('input[type=number]');

  nextRenderCompletion = receiveEvent('Finished PersonDisplay rendering');
  const updated = (Math.floor(Math.random() * 100000)).toString();
  setInputValue(input, updated);
  await nextRenderCompletion;

  if (display.innerHTML != updated) {
    throw new Error('Value not updated after render');
  }
}