using System.Collections.Generic;

namespace Wasm.Performance.Driver
{
    class BenchmarkResult
    {
        /// <summary>The result of executing scenario benchmarks</summary>
        public List<BenchmarkScenarioResult> ScenarioResults { get; set; }

        /// <summary>Downloaded application size in bytes</summary>
        public long DownloadSize { get; set; }
    }
}