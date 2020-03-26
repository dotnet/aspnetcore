namespace Wasm.Performance.Driver
{
    class BenchmarkResult
    {
        public string Name { get; set; }

        public bool Success { get; set; }

        public int NumExecutions { get; set; }

        public double Duration { get; set; }
    }
}