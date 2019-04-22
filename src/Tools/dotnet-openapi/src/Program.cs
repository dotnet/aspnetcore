// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.DotNet.OpenApi
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var outputWriter = new StringWriter();
            var errorWriter = new StringWriter();

            DebugMode.HandleDebugSwitch(ref args);

            try
            {
                var application = new Application(
                    Directory.GetCurrentDirectory(),
                    DownloadAsync,
                    outputWriter,
                    errorWriter);

                var result = application.Execute(args);

                return result;
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine("Unexpected error:");
                Console.Error.WriteLine(ex.ToString());
            }
            finally
            {
                var output = outputWriter.ToString();
                var error = errorWriter.ToString();

                outputWriter.Dispose();
                errorWriter.Dispose();

                Console.Write(output);
                Console.Error.Write(error);
            }

            return 1;
        }

        public static async Task<Stream> DownloadAsync(string url)
        {
            using (var client = new HttpClient())
            {
                return await client.GetStreamAsync(url);
            }
        }
    }
}
