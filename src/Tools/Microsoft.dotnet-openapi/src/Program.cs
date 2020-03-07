// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using Microsoft.DotNet.Openapi.Tools;

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
                using var httpClient = new HttpClientWrapper(new HttpClient());
                var application = new Application(
                    Directory.GetCurrentDirectory(),
                    httpClient,
                    outputWriter,
                    errorWriter);

                var result = application.Execute(args);

                return result;
            }
            catch (Exception ex)
            {
                errorWriter.Write("Unexpected error:");
                errorWriter.WriteLine(ex.ToString());
            }
            finally
            {
                var output = outputWriter.ToString();
                var error = errorWriter.ToString();

                outputWriter.Dispose();
                errorWriter.Dispose();

                Console.WriteLine(output);
                Console.Error.WriteLine(error);
            }

            return 1;
        }
    }
}
