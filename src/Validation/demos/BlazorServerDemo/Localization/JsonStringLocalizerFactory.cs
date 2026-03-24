// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Localization;

namespace BlazorServerDemo.Localization;

/// <summary>
/// An <see cref="IStringLocalizerFactory"/> that reads translations from a <c>wwwroot/translations.json</c> file.
/// </summary>
public sealed class JsonStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly Lazy<ConcurrentDictionary<string, Dictionary<string, string>>> _translations;

    public JsonStringLocalizerFactory(IWebHostEnvironment env)
    {
        _translations = new Lazy<ConcurrentDictionary<string, Dictionary<string, string>>>(() =>
        {
            var filePath = Path.Combine(env.WebRootPath, "translations.json");
            if (!File.Exists(filePath))
            {
                return new ConcurrentDictionary<string, Dictionary<string, string>>();
            }

            var json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);

            return data is not null
                ? new ConcurrentDictionary<string, Dictionary<string, string>>(data, StringComparer.OrdinalIgnoreCase)
                : new ConcurrentDictionary<string, Dictionary<string, string>>();
        });
    }

    public IStringLocalizer Create(Type resourceSource)
    {
        return new JsonStringLocalizer(_translations.Value);
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        return new JsonStringLocalizer(_translations.Value);
    }
}
