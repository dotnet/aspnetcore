// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net.Http;
using Microsoft.DotNet.Openapi.Tools;

namespace Microsoft.DotNet.OpenApi;

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
