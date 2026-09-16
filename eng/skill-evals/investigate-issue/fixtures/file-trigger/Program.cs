using System.Diagnostics;
using System.Text.Json;

if (args is ["--produce", var producedPath])
{
    var temporaryPath = producedPath + ".tmp";
    await File.WriteAllTextAsync(temporaryPath, "trigger-active");
    File.Move(temporaryPath, producedPath);
    return;
}

var outputRoot = args.Single();
Directory.CreateDirectory(outputRoot);

static async Task<bool> ObserveTriggerAsync(string root, Func<Task>? produce)
{
    var triggerPath = Path.Combine(root, "trigger.txt");
    var observed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    using var watcher = new FileSystemWatcher(root, "trigger.txt")
    {
        EnableRaisingEvents = true,
    };

    async void Observe(object? sender, FileSystemEventArgs eventArgs)
    {
        try
        {
            if (File.Exists(triggerPath))
            {
                var contents = await File.ReadAllTextAsync(triggerPath);
                if (contents.Contains("trigger-active", StringComparison.Ordinal))
                {
                    observed.TrySetResult(true);
                }
            }
        }
        catch (IOException exception)
        {
            observed.TrySetException(exception);
        }
    }

    watcher.Created += Observe;
    watcher.Renamed += Observe;
    if (produce is not null)
    {
        await produce();
    }

    try
    {
        return await observed.Task.WaitAsync(TimeSpan.FromSeconds(2));
    }
    catch (TimeoutException)
    {
        return false;
    }
}

var positiveRoot = Path.Combine(outputRoot, "positive");
Directory.CreateDirectory(positiveRoot);
var triggerPath = Path.Combine(positiveRoot, "trigger.txt");
var executable = Environment.ProcessPath
    ?? throw new InvalidOperationException("The current executable path is unavailable.");
var positiveObserved = await ObserveTriggerAsync(positiveRoot, async () =>
{
    var startInfo = new ProcessStartInfo(executable)
    {
        UseShellExecute = false,
    };
    if (Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
    {
        startInfo.ArgumentList.Add(Environment.GetCommandLineArgs()[0]);
    }
    startInfo.ArgumentList.Add("--produce");
    startInfo.ArgumentList.Add(triggerPath);
    using var producer = Process.Start(startInfo)
        ?? throw new InvalidOperationException("The trigger producer did not start.");
    await producer.WaitForExitAsync();
    if (producer.ExitCode != 0)
    {
        throw new InvalidOperationException("The trigger producer failed.");
    }
});
if (!positiveObserved)
{
    throw new InvalidOperationException("The real producer/reload path was not observed.");
}

var negativeRoot = Path.Combine(outputRoot, "trigger-absent-control");
Directory.CreateDirectory(negativeRoot);
var negativeObserved = await ObserveTriggerAsync(negativeRoot, produce: null);
if (negativeObserved)
{
    throw new InvalidOperationException("The trigger-absent control incorrectly observed a reload.");
}

await File.WriteAllTextAsync(
    Path.Combine(outputRoot, "file-trigger-receipt.json"),
    JsonSerializer.Serialize(new
    {
        producerExecuted = true,
        triggerObserved = true,
        reloadObserved = true,
        triggerAbsentControlRan = true,
        triggerAbsentControlObservedNoReload = true,
    }));
