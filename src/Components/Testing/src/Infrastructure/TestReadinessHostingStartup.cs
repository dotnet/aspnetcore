// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

/// <summary>
/// <see cref="IHostingStartup"/> that registers all E2E test infrastructure
/// services into the Blazor app under test. Injected via the
/// <c>ASPNETCORE_HOSTINGSTARTUPASSEMBLIES</c> environment variable.
/// </summary>
/// <remarks>
/// <para>Registers:</para>
/// <list type="bullet">
///   <item>Readiness notification service that POSTs to the test harness when the app starts</item>
///   <item>Parent PID watcher for auto-termination on test crash</item>
///   <item><see cref="TestSessionContext"/> + <see cref="TestLockProvider"/> + middleware for deterministic async state control</item>
///   <item>Static method service overrides (when <c>E2E_TEST_SERVICES_TYPE</c> + <c>E2E_TEST_SERVICES_METHOD</c> are set)</item>
/// </list>
/// <para>
/// The <c>[assembly: HostingStartup]</c> attribute is NOT in this file — it is emitted by the
/// source generator into the consuming test assembly.
/// </para>
/// </remarks>
public class TestReadinessHostingStartup : IHostingStartup
{
    /// <inheritdoc />
    public void Configure(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddHostedService<ReadinessNotificationService>();
            services.AddHostedService<ParentProcessWatcher>();

            // Test infrastructure: session context, lock provider, and middleware.
            // Always registered; no-op when tests don't use them.
            services.AddScoped<TestSessionContext>();
            services.AddSingleton<TestLockProvider>();
            services.AddTransient<IStartupFilter, TestInfrastructureStartupFilter>();
        });

        // Wire up service overrides if the env vars are set
        var overrideTypeName = Environment.GetEnvironmentVariable("E2E_TEST_SERVICES_TYPE");
        var overrideMethodName = Environment.GetEnvironmentVariable("E2E_TEST_SERVICES_METHOD");

        if (!string.IsNullOrEmpty(overrideTypeName) && !string.IsNullOrEmpty(overrideMethodName))
        {
            // Try the source-generated resolver first (no reflection on the target method)
            Action<IServiceCollection>? configureServices = TryResolveViaGeneratedResolver(overrideTypeName, overrideMethodName);

            // Fall back to reflection if the resolver didn't handle it
            if (configureServices is null)
            {
                var type = Type.GetType(overrideTypeName)
                    ?? throw new InvalidOperationException(
                        $"Could not resolve service override type '{overrideTypeName}'. " +
                        "Ensure the type name is assembly-qualified and the assembly is loadable.");

                var method = type.GetMethod(overrideMethodName,
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        null, [typeof(IServiceCollection)], null)
                    ?? throw new InvalidOperationException(
                        $"Could not find static method '{overrideMethodName}(IServiceCollection)' on type '{type.FullName}'.");

                configureServices = services =>
                {
                    method.Invoke(null, [services]);
                };
            }

            DiagnosticListener.AllListeners.Subscribe(
                new TestServiceOverrideObserver(configureServices));
        }
    }

    static Action<IServiceCollection>? TryResolveViaGeneratedResolver(
        string overrideTypeName, string overrideMethodName)
    {
        try
        {
            // The generated resolver lives in the test assembly, which is identified
            // by ASPNETCORE_HOSTINGSTARTUPASSEMBLIES (set by ServerInstance).
            var assemblyName = Environment.GetEnvironmentVariable("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES");
            if (string.IsNullOrEmpty(assemblyName))
            {
                return null;
            }

            Assembly? testAssembly = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == assemblyName)
                {
                    testAssembly = asm;
                    break;
                }
            }

            if (testAssembly is null)
            {
                return null;
            }

            // The source generator emits this well-known type in every test assembly
            var resolverType = testAssembly.GetType("Microsoft.AspNetCore.Components.Testing.Generated.ServiceOverrideResolver");
            if (resolverType is null)
            {
                return null;
            }

            var resolver = (IE2EServiceOverrideResolver)Activator.CreateInstance(resolverType)!;
            return resolver.TryResolve(overrideTypeName, overrideMethodName);
        }
        catch
        {
            // If anything goes wrong with the generated resolver, fall back to reflection
            return null;
        }
    }
}
