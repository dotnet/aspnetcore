using System.Diagnostics;
using System.Net;
using System.Text.Json;

var outputRoot = Environment.GetEnvironmentVariable("PROBE_OUTPUT_ROOT")
    ?? throw new InvalidOperationException("PROBE_OUTPUT_ROOT is required.");
var buildOutput = Environment.GetEnvironmentVariable("PROBE_BUILD_OUTPUT")
    ?? throw new InvalidOperationException("PROBE_BUILD_OUTPUT is required.");
var cacheMarker = Environment.GetEnvironmentVariable("PROBE_CACHE_MARKER")
    ?? throw new InvalidOperationException("PROBE_CACHE_MARKER is required.");
var controlId = Environment.GetEnvironmentVariable("PROBE_HOST_CONTROL_ID")
    ?? throw new InvalidOperationException("PROBE_HOST_CONTROL_ID is required.");
var protectedMarkerPath = Environment.GetEnvironmentVariable("PROBE_PROTECTED_MARKER_PATH")
    ?? throw new InvalidOperationException("PROBE_PROTECTED_MARKER_PATH is required.");
if (!File.Exists(buildOutput) || !File.Exists(cacheMarker))
{
    throw new InvalidOperationException("The approved build output or offline asset cache marker is missing.");
}
Directory.CreateDirectory(outputRoot);
await File.WriteAllTextAsync(Path.Combine(outputRoot, "writer-effect.txt"), "writer effect\n");

using var child = Process.Start(new ProcessStartInfo("dotnet", "--info")
{
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false,
}) ?? throw new InvalidOperationException("Could not start the owned child process.");
await child.WaitForExitAsync();
await File.WriteAllTextAsync(
    Path.Combine(outputRoot, "child-process.txt"),
    await child.StandardOutput.ReadToEndAsync());

var builder = WebApplication.CreateSlimBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:5077");
var app = builder.Build();
app.MapGet("/probe", () => "loopback-ok");
await app.StartAsync();

using var client = new HttpClient();
var response = await client.GetStringAsync("http://127.0.0.1:5077/probe");
await File.WriteAllTextAsync(Path.Combine(outputRoot, "loopback.txt"), response);
var protectedCredentialAbsent = Environment.GetEnvironmentVariable("SYNTHETIC_HOST_CREDENTIAL") is null;
var protectedMarkerAbsent = !File.Exists(protectedMarkerPath);
if (!protectedCredentialAbsent || !protectedMarkerAbsent)
{
    throw new InvalidOperationException("Protected host markers were visible in the child.");
}

var unrelatedHostEndpoint = Environment.GetEnvironmentVariable("UNRELATED_HOST_ENDPOINT")
    ?? throw new InvalidOperationException("UNRELATED_HOST_ENDPOINT is required.");
var unrelatedHostEndpointUnreachable = false;
try
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
    await client.GetAsync(unrelatedHostEndpoint, timeout.Token);
}
catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
{
    unrelatedHostEndpointUnreachable = true;
}
if (!unrelatedHostEndpointUnreachable)
{
    throw new InvalidOperationException("The unrelated host endpoint was reachable.");
}

await app.StopAsync();

await File.WriteAllTextAsync(
    Path.Combine(outputRoot, "host-effect-receipt.json"),
    JsonSerializer.Serialize(new
    {
        writerEffectObserved = true,
        buildOutputObserved = true,
        cacheWriteObserved = true,
        ownedChildObserved = child.ExitCode == 0,
        ownedChildExited = child.HasExited,
        containerLoopbackReachable = response == "loopback-ok",
        protectedCredentialAbsent = true,
        protectedMarkerAbsent = true,
        unrelatedHostEndpointUnreachable = true,
        controlId,
        protectedMarkerPath,
        unrelatedHostEndpoint,
    }));
