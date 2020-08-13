using System.Collections.Generic;

namespace Wasm.Performance.Driver
{
    class BenchmarkResult
    {
        /// <summary>The result of executing scenario benchmarks</summary>
        public List<BenchmarkScenarioResult> ScenarioResults { get; set; }

        /// <summary>Downloaded application size in bytes</summary>
        public long? DownloadSize { get; set; }

        /// <summary>WASM memory usage</summary>
        public long? WasmMemory { get; set; }

        // See https://developer.mozilla.org/en-US/docs/Web/API/Performance/memory
        /// <summary>JS memory usage</summary>
        public long? UsedJSHeapSize { get; set; }

        /// <summary>JS memory usage</summary>
        public long? TotalJSHeapSize { get; set; }
    }
}
