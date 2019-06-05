// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ZipManifestGenerator
{
    class Program
    {
        private static void PrintUsage()
        {
            Console.WriteLine(@"
Usage: <ZIP> <OUTPUT>

<ZIP>      A file path or URL to the ZIP file.
<OUTPUT>   The output file path for the ZIP manifest file.");
        }

        private const int ZipDoesNotExist = 404;

        public static async Task<int> Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Invalid arguments");
                PrintUsage();
                return 1;
            }

            var zipPath = args[0];
            var manifestOutputPath = args[1];

            var shouldCleanupZip = false;

            if (zipPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                shouldCleanupZip = true;
                var url = zipPath;
                Console.WriteLine($"log: Downloading {url}");
                zipPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".zip");
                var response = await new HttpClient().GetAsync(url);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    Console.Error.WriteLine($"Could not find {url}.");
                    return ZipDoesNotExist;
                }

                response.EnsureSuccessStatusCode();

                using (var outStream = File.Create(zipPath))
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    await responseStream.CopyToAsync(outStream);
                }
            }

            try
            {
                Console.WriteLine($"log: Generating manifest in {manifestOutputPath}");

                using (var zipStream = File.OpenRead(zipPath))
                using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Read))
                using (var manifest = File.Create(manifestOutputPath))
                using (var writer = new StreamWriter(manifest))
                {
                    foreach (var file in zip.Entries.OrderBy(_ => _.FullName))
                    {
                        writer.WriteLine(file.FullName.Replace("/", "\\"));
                    }
                }
            }
            finally
            {
                if (shouldCleanupZip)
                {
                    File.Delete(zipPath);
                }
            }

            return 0;
        }
    }
}
