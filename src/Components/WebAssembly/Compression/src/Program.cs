using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.WebAssembly.Build.BrotliCompression
{
    class Program
    {
        private const int _error = -1;

        static async Task<int> Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Invalid argument count. Usage: 'blazor-brotli <<path-to-manifest>>'");
                return _error;
            }

            var manifestPath = args[0];
            if (!File.Exists(manifestPath))
            {
                Console.Error.WriteLine($"Manifest '{manifestPath}' does not exist.");
                return -1;
            }

            using var manifestStream = File.OpenRead(manifestPath);

            var manifest = await JsonSerializer.DeserializeAsync<ManifestData>(manifestStream);
            var result = 0;
            Parallel.ForEach(manifest.FilesToCompress, (file) =>
            {
                var inputPath = file.Source;
                var inputSource = file.InputSource;
                var targetCompressionPath = file.Target;

                if (!File.Exists(inputSource))
                {
                    Console.WriteLine($"Skipping '{inputPath}' because '{inputSource}' does not exist.");
                    return;
                }

                if (File.Exists(targetCompressionPath) && File.GetLastWriteTimeUtc(inputSource) < File.GetLastWriteTimeUtc(targetCompressionPath))
                {
                    // Incrementalism. If input source doesn't exist or it exists and is not newer than the expected output, do nothing.
                    Console.WriteLine($"Skipping '{inputPath}' because '{targetCompressionPath}' is newer than '{inputSource}'.");
                    return;
                }

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(targetCompressionPath));

                    using var sourceStream = File.OpenRead(inputPath);
                    using var fileStream = new FileStream(targetCompressionPath, FileMode.Create);

                    var compressionLevel = CompressionLevel.Optimal;
                    if (Environment.GetEnvironmentVariable("_BlazorWebAssemblyBuildTest_BrotliCompressionLevel_NoCompression") == "1")
                    {
                        compressionLevel = CompressionLevel.NoCompression;
                    }
                    using var stream = new BrotliStream(fileStream, compressionLevel);

                    sourceStream.CopyTo(stream);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                    result = -1;
                }
            });

            return result;
        }

        private class ManifestData
        {
            public CompressedFile[] FilesToCompress { get; set; }
        }

        private class CompressedFile
        {
            public string Source { get; set; }

            public string InputSource { get; set; }

            public string Target { get; set; }
        }
    }
}
