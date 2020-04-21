import { groups, BenchmarkEvent, onBenchmarkEvent } from './lib/minibench/minibench.js';
import { HtmlUI } from './lib/minibench/minibench.ui.js';
import './renderListStress.js';
import './jsonHandlingStress.js';
import './orgChartStress.js';

import { BlazorStressApp } from './util/BlazorStressApp.js';

new HtmlUI('E2E Performance', '#display');

if (location.href.indexOf('#automated') !== -1) {
  (async function () {
    const query = new URLSearchParams(window.location.search);
    const resultsUrl = query.get('resultsUrl');
    // Executes up to the specified number of iterations, defaulting to 5. If '-1' is specified, stress
    // runs continuously until the browser is closed.
    const iterations = query.get('iterations') || 5;

    await new BlazorStressApp().start();

    for (let i = 0; iterations === '-1' || i < parseInt(iterations); i++) {
      const scenarioResults = [];
      const promise = new Promise((resolve, reject) => {
        onBenchmarkEvent(async (status, args) => {
          switch (status) {
            case BenchmarkEvent.runStarted:
              scenarioResults.length = 0;
              break;
            case BenchmarkEvent.benchmarkCompleted:
            case BenchmarkEvent.benchmarkError:
              console.log(`Completed benchmark ${args.name}`);
              scenarioResults.push(args);
              break;
            case BenchmarkEvent.runCompleted:
              if (resultsUrl) {
                await fetch(resultsUrl, {
                  method: 'post',
                  body: JSON.stringify({
                    downloadSize: downloadSize,
                    scenarioResults: scenarioResults
                  })
                });
              }
              resolve();
              break;
            default:
              reject(new Error(`Unknown status: ${status}`));
          }
        });
      });
      groups.forEach(g => g.runAll());
      await promise;
    }
  })();
}
