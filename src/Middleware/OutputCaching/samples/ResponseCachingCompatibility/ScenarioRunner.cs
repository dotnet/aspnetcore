// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.Net.Http;

namespace ResponseCachingCompatibility;

internal static class ScenarioRunner
{
    private const string ServeUrl = "http://localhost:5000";

    public static async Task ServeAsync(ScenarioApplication application)
    {
        await using var app = application.Create(CreateBuilder(ServeUrl));
        await app.StartAsync();

        Console.WriteLine($"{application.Name} listening on {ServeUrl}. Press Ctrl+C to stop.");
        await app.WaitForShutdownAsync();
    }

    public static async Task CompareAsync(Scenario scenario)
    {
        var applications = scenario.Applications
            .Select((application, index) => (application.Name, Url: $"http://localhost:{5101 + index}", application.Create))
            .ToArray();

        var apps = new List<WebApplication>();
        try
        {
            foreach (var application in applications)
            {
                var app = application.Create(CreateBuilder(application.Url));
                apps.Add(app);
                await app.StartAsync();
            }

            Console.WriteLine($"{scenario.Name}: {scenario.Description}");
            Console.WriteLine();

            var descriptionWidth = scenario.Steps.Max(step => step.Description.Length) + 2;
            const int columnWidth = 32;

            Console.WriteLine("Step".PadRight(descriptionWidth) + string.Concat(applications.Select(a => a.Name.PadRight(columnWidth))));

            // Cookies are set by the steps, not remembered from previous responses.
            using var client = new HttpClient(new SocketsHttpHandler { UseCookies = false });
            foreach (var step in scenario.Steps)
            {
                await Task.Delay(TimeSpan.FromSeconds(step.DelaySeconds));

                var line = step.Description.PadRight(descriptionWidth);
                foreach (var application in applications)
                {
                    line += (await SendStepAsync(client, application.Url, step)).PadRight(columnWidth);
                }
                Console.WriteLine(line);
            }
        }
        finally
        {
            foreach (var app in apps)
            {
                await app.DisposeAsync();
            }
        }
    }

    private static async Task<string> SendStepAsync(HttpClient client, string baseUrl, Step step)
    {
        if (step.Parallel <= 1)
        {
            return await SendAsync(client, baseUrl, step);
        }

        // Each endpoint run returns a different response, so the number of distinct responses is the number of runs.
        var responses = await Task.WhenAll(Enumerable.Range(0, step.Parallel).Select(_ => SendAsync(client, baseUrl, step, includeAge: false)));
        var distinctResponses = responses.Distinct().Count();
        return distinctResponses == 1 ? "1 distinct response" : $"{distinctResponses} distinct responses";
    }

    private static async Task<string> SendAsync(HttpClient client, string baseUrl, Step step, bool includeAge = true)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, baseUrl + step.Path);
        step.ConfigureRequest?.Invoke(request);

        using var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return $"{(int)response.StatusCode} {response.ReasonPhrase}";
        }

        var body = await ReadBodyAsync(response);

        return includeAge && response.Headers.Age is { } age ? $"{body} (Age: {age.TotalSeconds}s)" : body;
    }

    // The client doesn't decompress automatically, so the body is decompressed here and the encoding is shown.
    private static async Task<string> ReadBodyAsync(HttpResponseMessage response)
    {
        var encoding = response.Content.Headers.ContentEncoding.FirstOrDefault();
        await using var content = await response.Content.ReadAsStreamAsync();
        await using Stream decoded = encoding switch
        {
            "gzip" => new GZipStream(content, CompressionMode.Decompress),
            "br" => new BrotliStream(content, CompressionMode.Decompress),
            _ => content,
        };

        using var reader = new StreamReader(decoded);
        var body = await reader.ReadToEndAsync();
        return encoding is null ? body : $"{body} [{encoding}]";
    }

    private static WebApplicationBuilder CreateBuilder(string url)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls(url);
        return builder;
    }
}
