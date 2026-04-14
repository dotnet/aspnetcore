// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.Localization;

namespace BlazorNet11.Localization;

public class JsonStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly Lazy<JsonStringLocalizer> _localizer;

    public JsonStringLocalizerFactory(IWebHostEnvironment env)
    {
        _localizer = new Lazy<JsonStringLocalizer>(() =>
        {
            var translations = new Dictionary<string, Dictionary<string, string>>();
            var resourcesPath = Path.Combine(env.ContentRootPath, "Resources");

            foreach (var file in Directory.GetFiles(resourcesPath, "translations.*.json"))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var culture = fileName.Split('.').Last();
                var json = File.ReadAllText(file);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict is not null)
                {
                    translations[culture] = dict;
                }
            }

            return new JsonStringLocalizer(translations);
        });
    }

    public IStringLocalizer Create(Type resourceSource) => _localizer.Value;
    public IStringLocalizer Create(string baseName, string location) => _localizer.Value;
}
