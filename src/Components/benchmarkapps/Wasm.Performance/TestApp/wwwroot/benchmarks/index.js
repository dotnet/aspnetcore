import { groups, BenchmarkEvent, onBenchmarkEvent } from './lib/minibench/minibench.js';
import { HtmlUI } from './lib/minibench/minibench.ui.js';
import './appStartup.js';
import './renderList.js';
import './jsonHandling.js';
import './orgChart.js';
import './grid.js';
import { getBlazorDownloadSize } from './blazorDownloadSize.js';

new HtmlUI('E2E Performance', '#display');

if (location.href.indexOf('#automated') !== -1) {
  (async function() {
    const query = new URLSearchParams(window.location.search);
    const resultsUrl = query.get('resultsUrl');

    console.log('Calculating download size...');
    const downloadSize = await getBlazorDownloadSize();
    console.log('Download size: ', downloadSize);

    const scenarioResults = [];
    groups.forEach(g => g.runAll());

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
          break;
        default:
          throw new Error(`Unknown status: ${status}`);
      }
    });
  })();
}
