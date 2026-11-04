import { groups, BenchmarkEvent, onBenchmarkEvent } from './lib/minibench/minibench.js';
import { HtmlUI } from './lib/minibench/minibench.ui.js';
import './renderListStress.js';
import './jsonHandlingStress.js';
import './orgChartStress.js';
import './timerStress.js';

import { BlazorStressApp } from './util/BlazorStressApp.js';

new HtmlUI('E2E Performance', '#display');

if (location.href.indexOf('#automated') !== -1) {
  (async function () {
    const query = new URLSearchParams(window.location.search);
    const resultsUrl = query.get('resultsUrl');

    // timeout in ms. Defaults to 2 minutes.
    const timeout = query.get('timeout') || 2 * 60 * 1000;
    const scenarioResults = [];

    await BlazorStressApp.createAsync();

    let shouldRun = true;
    setTimeout(() => shouldRun = false, timeout);

    while (shouldRun) {
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
              {
                const wasmMemory = BlazorStressApp.instance.window.DotNet.invokeMethod('Wasm.Performance.TestApp', 'GetTotalMemory');

                const jsMemory = window.performance.memory;

                if (resultsUrl) {
                  await fetch(resultsUrl, {
                    method: 'post',
                    body: JSON.stringify({
                      wasmMemory: wasmMemory,
                      usedJSHeapSize: jsMemory.usedJSHeapSize,
                      totalJSHeapSize: jsMemory.totalJSHeapSize,
                      scenarioResults: scenarioResults
                    })
                  });
                }
                resolve();
                break;
              }
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
