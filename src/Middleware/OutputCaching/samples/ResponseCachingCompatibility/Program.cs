// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ResponseCachingCompatibility;

// Usage: ResponseCachingCompatibility [scenario] [compare|responsecaching|outputcaching|default|...]
// Without arguments, the scenario and the mode are selected interactively.

var scenarios = typeof(Scenario).Assembly.GetTypes()
    .Where(type => type.IsSubclassOf(typeof(Scenario)) && !type.IsAbstract)
    .OrderBy(type => type.Name)
    .Select(type => (Scenario)Activator.CreateInstance(type)!)
    .ToArray();

var scenario = Select(
    "Available scenarios:",
    scenarios,
    s => $"{s.Name,-28} {s.Description}",
    s => s.Name,
    args.ElementAtOrDefault(0));

Mode[] modes =
[
    new("compare", "Send the scenario requests to every application and compare the responses", null),
    .. scenario.Applications.Select(application => new Mode(application.Key, $"Serve '{application.Name}' on http://localhost:5000", application)),
];

var mode = Select(
    $"{scenario.Name}: select a mode (Enter for compare):",
    modes,
    m => $"{m.Name,-28} {m.Description}",
    m => m.Name,
    args.ElementAtOrDefault(1),
    defaultValue: modes[0]);

if (mode.Application is null)
{
    await ScenarioRunner.CompareAsync(scenario);
}
else
{
    await ScenarioRunner.ServeAsync(mode.Application);
}

static T Select<T>(string title, IReadOnlyList<T> items, Func<T, string> display, Func<T, string> name, string? argument, T? defaultValue = null) where T : class
{
    if (argument is not null)
    {
        return items.FirstOrDefault(item => string.Equals(name(item), argument, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Unknown value '{argument}'. Available values: {string.Join(", ", items.Select(name))}.");
    }

    while (true)
    {
        Console.WriteLine(title);
        for (var i = 0; i < items.Count; i++)
        {
            Console.WriteLine($"  #{i + 1} {display(items[i])}");
        }
        Console.Write("Enter the number or name: ");

        var input = Console.ReadLine()?.Trim();
        Console.WriteLine();

        if (string.IsNullOrEmpty(input) && defaultValue is not null)
        {
            return defaultValue;
        }

        if (int.TryParse(input, out var index) && index >= 1 && index <= items.Count)
        {
            return items[index - 1];
        }

        var match = items.FirstOrDefault(item => string.Equals(name(item), input, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
        {
            return match;
        }

        Console.WriteLine($"'{input}' is not a valid selection.");
    }
}

internal sealed record Mode(string Name, string Description, ScenarioApplication? Application);
