// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

/// <summary>
/// Options for starting an app via <see cref="ServerFixture{TTestAssembly}.StartServerAsync{TApp}"/>.
/// These options form part of the deduplication key: calling <c>StartServerAsync</c>
/// twice with the same app name and same options returns the same instance.
/// </summary>
public class ServerStartOptions
{
    /// <summary>
    /// Additional environment variables to pass to the app process.
    /// Applied on top of manifest-defined variables (overrides them).
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; } = new();

    /// <summary>
    /// Readiness timeout in milliseconds. Default 60s covers WASM apps
    /// that need to download the .NET runtime on first load.
    /// </summary>
    public int ReadinessTimeoutMs { get; set; } = 60_000;

    internal string? ServiceOverrideTypeName { get; private set; }

    internal string? ServiceOverrideMethodName { get; private set; }

    /// <summary>
    /// Registers a static method that overrides DI services in the app process.
    /// The method must have signature: <c>static void MethodName(IServiceCollection services)</c>.
    /// This is the cross-process equivalent of WAF's <c>ConfigureTestServices</c>.
    /// </summary>
    /// <param name="type">The type containing the static override method.</param>
    /// <param name="methodName">The name of the static method.</param>
    public void ConfigureServices(Type type, string methodName)
    {
        ServiceOverrideTypeName = type.AssemblyQualifiedName;
        ServiceOverrideMethodName = methodName;
    }

    /// <summary>
    /// Registers a static method that overrides DI services in the app process.
    /// The method must have signature: <c>static void MethodName(IServiceCollection services)</c>.
    /// </summary>
    /// <typeparam name="T">The type containing the static override method.</typeparam>
    /// <param name="methodName">The name of the static method.</param>
    public void ConfigureServices<T>(string methodName) => ConfigureServices(typeof(T), methodName);
}
