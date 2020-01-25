import { groups, BenchmarkEvent, onBenchmarkEvent } from './lib/minibench/minibench.js';
import { HtmlUI } from './lib/minibench/minibench.ui.js';
import './appStartup.js';
import './renderList.js';
import './jsonHandling.js';

new HtmlUI('E2E Performance', '#display');

if (location.href.indexOf('#automated') !== -1) {
  const query = new URLSearchParams(window.location.search);
  const group = query.get('group');
  const resultsUrl = query.get('resultsUrl');

  groups.filter(g => !group || g.name === group).forEach(g => g.runAll());

  const benchmarksResults = [];
  onBenchmarkEvent(async (status, args) => {
    switch (status) {
        case BenchmarkEvent.runStarted:
          benchmarksResults.length = 0;
          break;
        case BenchmarkEvent.benchmarkCompleted:
        case BenchmarkEvent.benchmarkError:
          console.log(`Completed benchmark ${args.name}`);
          benchmarksResults.push(args);
          break;
        case BenchmarkEvent.runCompleted:
            if (resultsUrl) {
              await fetch(resultsUrl, {
                method: 'post',
                body: JSON.stringify(benchmarksResults)
              });
            }
            break;
        default:
          throw new Error(`Unknown status: ${status}`);
      }
  })
}
