// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.E2ETesting;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;

public abstract class ServerFixture : IDisposable
{
    private static readonly Lazy<Dictionary<string, string>> _projects = new Lazy<Dictionary<string, string>>(FindProjects);

    public Uri RootUri => _rootUriInitializer.Value;

    private readonly Lazy<Uri> _rootUriInitializer;

    public ServerFixture()
    {
        _rootUriInitializer = new Lazy<Uri>(() =>
        {
            var uri = new Uri(StartAndGetRootUri());
            if (E2ETestOptions.Instance.SauceTest)
            {
                uri = new UriBuilder(uri.Scheme, E2ETestOptions.Instance.Sauce.HostName, uri.Port).Uri;
            }

            return uri;
        });
    }

    public abstract void Dispose();

    protected abstract string StartAndGetRootUri();

    private static Dictionary<string, string> FindProjects()
    {
        return typeof(ServerFixture).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(m => m.Key.StartsWith("TestAssemblyApplication[", StringComparison.Ordinal))
            .ToDictionary(m =>
                m.Key.Replace("TestAssemblyApplication", "").TrimStart('[').TrimEnd(']'),
                m => m.Value);
    }

    public static string FindSampleOrTestSitePath(string projectName)
    {
        var projects = _projects.Value;
        if (projects.TryGetValue(projectName, out var dir))
        {
            return dir;
        }

        throw new ArgumentException($"Cannot find a sample or test site with name '{projectName}'.");
    }

    protected static void RunInBackgroundThread(Action action)
    {
        var isDone = new ManualResetEvent(false);

        ExceptionDispatchInfo edi = null;
        new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                edi = ExceptionDispatchInfo.Capture(ex);
            }

            isDone.Set();
        }).Start();

        if (!isDone.WaitOne(TimeSpan.FromSeconds(10)))
        {
            throw new TimeoutException("Timed out waiting for: " + action);
        }

        if (edi != null)
        {
            throw edi.SourceException;
        }
    }
}
