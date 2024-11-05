// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Xml.Linq;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.Extensions.Logging;
using Microsoft.Web.Administration;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

public static class Helpers
{
    private static readonly TimeSpan RetryRequestDelay = TimeSpan.FromMilliseconds(10);
    private static readonly int RetryRequestCount = 10;

    public static string GetInProcessTestSitesName()
    {
        return DeployerSelector.IsNewShimTest ? "InProcessNewShimWebSite" : "InProcessWebSite";
    }

    public static async Task AssertStarts(this IISDeploymentResult deploymentResult, string path = "/HelloWorld")
    {
        // Sometimes the site is not ready, so retry until its actually started and ready
        var response = await deploymentResult.HttpClient.RetryRequestAsync(path, r => r.IsSuccessStatusCode);
        var responseText = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            Assert.Equal("Hello World", responseText);
        }
        else
        {
            throw new Exception($"Server not started successfully, recieved non success status code, responseText: {responseText}");
        }
    }

    public static async Task StressLoad(HttpClient httpClient, string path, Action<HttpResponseMessage> action)
    {
        async Task RunRequests()
        {
            var connection = new HttpClient()
            {
                BaseAddress = httpClient.BaseAddress,
                Timeout = TimeSpan.FromSeconds(200),
            };

            for (int j = 0; j < 10; j++)
            {
                var response = await connection.GetAsync(path);
                action(response);
            }
        }

        List<Task> tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(RunRequests));
        }

        await Task.WhenAll(tasks);
    }

    public static string GetFrebFolder(string folder, IISDeploymentResult result)
    {
        if (result.DeploymentParameters.ServerType == ServerType.IISExpress)
        {
            return Path.Combine(folder, result.DeploymentParameters.SiteName);
        }
        else
        {
            return Path.Combine(folder, "W3SVC1");
        }
    }

    public static void CopyFiles(DirectoryInfo source, DirectoryInfo target, ILogger logger)
    {
        foreach (DirectoryInfo directoryInfo in source.GetDirectories())
        {
            if (directoryInfo.FullName != target.FullName)
            {
                CopyFiles(directoryInfo, target.CreateSubdirectory(directoryInfo.Name), logger);
            }
        }
        logger?.LogDebug($"Processing {target.FullName}");
        foreach (FileInfo fileInfo in source.GetFiles())
        {
            logger?.LogDebug($"  Copying {fileInfo.Name}");
            var destFileName = Path.Combine(target.FullName, fileInfo.Name);
            fileInfo.CopyTo(destFileName);
        }
    }

    public static void ModifyWebConfig(this DeploymentResult deploymentResult, Action<XElement> action)
    {
        var webConfigPath = Path.Combine(deploymentResult.ContentRoot, "web.config");
        var document = XDocument.Load(webConfigPath);
        action(document.Root);
        document.Save(webConfigPath);
    }

    public static Task<HttpResponseMessage> RetryRequestAsync(this HttpClient client, string uri, Func<HttpResponseMessage, bool> predicate)
    {
        return RetryRequestAsync(client, uri, message => Task.FromResult(predicate(message)));
    }

    public static async Task<HttpResponseMessage> RetryRequestAsync(this HttpClient client, string uri, Func<HttpResponseMessage, Task<bool>> predicate)
    {
        HttpResponseMessage response = await client.GetAsync(uri);
        var delay = RetryRequestDelay;
        for (var i = 0; i < RetryRequestCount && !await predicate(response); i++)
        {
            // Keep retrying until app_offline is present.
            response = await client.GetAsync(uri);
            await Task.Delay(delay);
            // This will max out at a 5 second delay
            delay *= 2;
        }

        if (!await predicate(response))
        {
            throw new InvalidOperationException($"Didn't get response that satisfies predicate after {RetryRequestCount} retries");
        }

        return response;
    }

    public static Task Retry(Func<Task> func, TimeSpan maxTime)
    {
        return Retry(func, (int)(maxTime.TotalMilliseconds / 200), 200);
    }

    public static async Task Retry(Func<Task> func, int attempts, int msDelay)
    {
        var exceptions = new List<Exception>();

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            try
            {
                await func();
                return;
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
            await Task.Delay(msDelay);
        }

        throw new AggregateException(exceptions);
    }

    public static void AssertWorkerProcessStop(this IISDeploymentResult deploymentResult, int? timeout = null)
    {
        var hostProcess = deploymentResult.HostProcess;
        Assert.True(hostProcess.WaitForExit(timeout ?? (int)TimeoutExtensions.DefaultTimeoutValue.TotalMilliseconds));

        if (deploymentResult.DeploymentParameters.ServerType == ServerType.IISExpress)
        {
            Assert.Equal(0, hostProcess.ExitCode);
        }
    }

    public static async Task AssertRecycledAsync(this IISDeploymentResult deploymentResult, Func<Task> verificationAction = null)
    {
        if (deploymentResult.DeploymentParameters.HostingModel != HostingModel.InProcess)
        {
            throw new NotSupportedException();
        }

        deploymentResult.AssertWorkerProcessStop();
        if (deploymentResult.DeploymentParameters.ServerType == ServerType.IIS)
        {
            verificationAction = verificationAction ?? (() => deploymentResult.AssertStarts());
            await verificationAction();
        }
    }

    // Don't use with IISExpress, recycle isn't a valid operation
    public static void Recycle(string appPoolName)
    {
        using var serverManager = new ServerManager();
        var appPool = serverManager.ApplicationPools.FirstOrDefault(ap => ap.Name == appPoolName);
        Assert.NotNull(appPool);
        appPool.Recycle();
    }

    public static IEnumerable<object[]> ToTheoryData<T>(this Dictionary<string, T> dictionary)
    {
        return dictionary.Keys.Select(k => new[] { k });
    }

    public static string GetExpectedLogName(IISDeploymentResult deploymentResult, string logFolderPath)
    {
        var startTime = deploymentResult.HostProcess.StartTime.ToUniversalTime();

        if (deploymentResult.DeploymentParameters.HostingModel == HostingModel.InProcess)
        {
            return Path.Combine(logFolderPath, $"std_{startTime.Year}{startTime.Month:D2}" +
            $"{startTime.Day:D2}{startTime.Hour:D2}" +
            $"{startTime.Minute:D2}{startTime.Second:D2}_" +
            $"{deploymentResult.HostProcess.Id}.log");
        }
        else
        {
            return Directory.GetFiles(logFolderPath).Single();
        }
    }

    public static void ModifyFrameworkVersionInRuntimeConfig(IISDeploymentResult deploymentResult)
    {
        var path = Path.Combine(deploymentResult.ContentRoot, "InProcessWebSite.runtimeconfig.json");
        dynamic depsFileContent = JsonConvert.DeserializeObject(File.ReadAllText(path));
        depsFileContent["runtimeOptions"]["framework"]["version"] = "2.9.9";
        var output = JsonConvert.SerializeObject(depsFileContent);
        File.WriteAllText(path, output);
    }

    public static void AllowNoLogs(this IISDeploymentResult deploymentResult)
    {
        File.AppendAllText(
            Path.Combine(deploymentResult.DeploymentParameters.PublishedApplicationRootPath, "aspnetcore-debug.log"),
            "Running test allowed log file to be empty." + Environment.NewLine);
    }

    public static string ReadAllTextFromFile(string filename, ILogger logger)
    {
        try
        {
            return File.ReadAllText(filename);
        }
        catch (Exception ex)
        {
            // check if there is a dotnet.exe, iisexpress.exe, or w3wp.exe processes still running.
            var hostingProcesses = Process.GetProcessesByName("dotnet")
                .Concat(Process.GetProcessesByName("iisexpress"))
                .Concat(Process.GetProcessesByName("w3wp"));

            logger.LogError($"Could not read file content. Exception message {ex.Message}");
            logger.LogError("Current hosting exes running:");

            foreach (var hostingProcess in hostingProcesses)
            {
                logger.LogError($"{hostingProcess.ProcessName} pid: {hostingProcess.Id} hasExited: {hostingProcess.HasExited.ToString()}");
            }
            throw;
        }
    }

    public static string CreateEmptyApplication(XElement config, string contentRoot)
    {
        var siteElement = config
            .RequiredElement("system.applicationHost")
            .RequiredElement("sites")
            .RequiredElement("site");

        var application = siteElement
            .RequiredElement("application");

        var rootApplicationDirectory = new DirectoryInfo(contentRoot + "rootApp");
        rootApplicationDirectory.Create();

        File.WriteAllText(Path.Combine(rootApplicationDirectory.FullName, "web.config"), "<configuration></configuration>");

        var rootApplication = new XElement(application);
        rootApplication.SetAttributeValue("path", "/");
        rootApplication.RequiredElement("virtualDirectory")
            .SetAttributeValue("physicalPath", rootApplicationDirectory.FullName);

        siteElement.Add(rootApplication);

        return rootApplicationDirectory.FullName;
    }
}
