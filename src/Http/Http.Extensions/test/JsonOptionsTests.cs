// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.DotNet.RemoteExecutor;

namespace Microsoft.AspNetCore.Http.Extensions;

public class JsonOptionsTests
{
    [ConditionalFact]
    [RemoteExecutionSupported]
    public void DefaultSerializerOptions_SetsTypeInfoResolverEmptyResolver_WhenJsonIsReflectionEnabledByDefaultFalse()
    {
        // Diagnostic: log native libraries and threads in the testhost process
        // to help diagnose intermittent ECHILD FailFast (dotnet/runtime#33297)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            try
            {
                Console.WriteLine("=== DIAGNOSTIC: /proc/self/maps (native libraries) ===");
                foreach (var line in File.ReadLines("/proc/self/maps"))
                {
                    // Only log lines with .so files to reduce noise
                    if (line.Contains(".so"))
                    {
                        Console.WriteLine(line);
                    }
                }
                Console.WriteLine("=== DIAGNOSTIC: /proc/self/status ===");
                Console.WriteLine(File.ReadAllText("/proc/self/status"));
                Console.WriteLine("=== DIAGNOSTIC: threads ===");
                foreach (var dir in Directory.GetDirectories("/proc/self/task"))
                {
                    var tid = Path.GetFileName(dir);
                    var comm = File.ReadAllText(Path.Combine(dir, "comm")).Trim();
                    Console.WriteLine($"  tid={tid} comm={comm}");
                }
                Console.WriteLine("=== END DIAGNOSTIC ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Diagnostic failed: {ex.Message}");
            }
        }

        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", false.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            // Arrange
            var options = JsonOptions.DefaultSerializerOptions;

            // Assert
            Assert.NotNull(options.TypeInfoResolver);
            Assert.IsAssignableFrom<IJsonTypeInfoResolver>(options.TypeInfoResolver);
        }, options);
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void DefaultSerializerOptions_SetsTypeInfoResolverToDefault_WhenJsonIsReflectionEnabledByDefaultTrue()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", true.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            // Arrange
            var options = JsonOptions.DefaultSerializerOptions;

            // Assert
            Assert.NotNull(options.TypeInfoResolver);
            Assert.IsType<DefaultJsonTypeInfoResolver>(options.TypeInfoResolver);
        }, options);
    }
}
