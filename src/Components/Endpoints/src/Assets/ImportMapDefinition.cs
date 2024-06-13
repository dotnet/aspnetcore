// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Represents the contents of a <c><script type="importmap"></script></c> element that defines the import map
/// for module scripts in the application.
/// </summary>
/// <remarks>
/// The import map is a JSON object that defines the mapping of module import specifiers to URLs.
/// <see cref="ImportMapDefinition"/> instances are expensive to create, so it is recommended to cache them if
/// you are creating an additional instance.
/// </remarks>
public sealed class ImportMapDefinition
{
    private Dictionary<string, string>? _imports;
    private Dictionary<string, IReadOnlyDictionary<string, string>>? _scopes;
    private Dictionary<string, string>? _integrity;
    private string? _json;

    /// <summary>
    /// Initializes a new instance of <see cref="ImportMapDefinition"/>."/> with the specified imports, scopes, and integrity.
    /// </summary>
    /// <param name="imports">The unscoped imports defined in the import map.</param>
    /// <param name="scopes">The scoped imports defined in the import map.</param>
    /// <param name="integrity">The integrity for the imports defined in the import map.</param>
    /// <remarks>
    /// The <paramref name="imports"/>, <paramref name="scopes"/>, and <paramref name="integrity"/> parameters
    /// will be copied into the new instance. The original collections will not be modified.
    /// </remarks>
    public ImportMapDefinition(
        IReadOnlyDictionary<string, string>? imports,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? scopes,
        IReadOnlyDictionary<string, string>? integrity)
    {
        _imports = imports?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        _integrity = integrity?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        _scopes = scopes?.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToDictionary(scopeKvp => scopeKvp.Key, scopeKvp => scopeKvp.Value) as IReadOnlyDictionary<string, string>);
    }

    private ImportMapDefinition()
    {
    }

    /// <summary>
    /// Creates an import map from a <see cref="ResourceAssetCollection"/>.
    /// </summary>
    /// <param name="assets">The collection of assets to create the import map from.</param>
    /// <returns>The import map.</returns>
    public static ImportMapDefinition FromResourceCollection(ResourceAssetCollection assets)
    {
        var importMap = new ImportMapDefinition();
        foreach (var asset in assets)
        {
            if (!(asset.Url.EndsWith(".mjs", StringComparison.OrdinalIgnoreCase) ||
                asset.Url.EndsWith(".js", StringComparison.OrdinalIgnoreCase)) ||
                asset.Properties == null)
            {
                continue;
            }

            var (integrity, label) = GetAssetProperties(asset);
            if (integrity != null)
            {
                importMap._integrity ??= [];
                importMap._integrity[asset.Url] = integrity;
            }

            if (label != null)
            {
                importMap._imports ??= [];
                importMap._imports[$"./{label}"] = $"./{asset.Url}";
            }
        }

        return importMap;
    }

    private static (string? integrity, string? label) GetAssetProperties(ResourceAsset asset)
    {
        string? integrity = null;
        string? label = null;
        for (var i = 0; i < asset.Properties!.Count; i++)
        {
            var property = asset.Properties[i];
            if (string.Equals(property.Name, "integrity", StringComparison.OrdinalIgnoreCase))
            {
                integrity = property.Value;
            }
            else if (string.Equals(property.Name, "label", StringComparison.OrdinalIgnoreCase))
            {
                label = property.Value;
            }

            if (integrity != null && label != null)
            {
                return (integrity, label);
            }
        }

        return (integrity, label);
    }

    /// <summary>
    /// Combines one or more import maps into a single import map.
    /// </summary>
    /// <param name="sources">The list of import maps to combine.</param>
    /// <returns>
    /// A new import map that is the combination of all the input import maps with their
    /// entries applied in order.
    /// </returns>
    public static ImportMapDefinition Combine(params ImportMapDefinition[] sources)
    {
        var importMap = new ImportMapDefinition();
        foreach (var item in sources)
        {
            if (item.Imports != null)
            {
                importMap._imports ??= [];
                foreach (var (key, value) in item.Imports)
                {
                    importMap._imports[key] = value;
                }
            }

            if (item.Scopes != null)
            {
                importMap._scopes ??= [];
                foreach (var (key, value) in item.Scopes)
                {
                    if (importMap._scopes.TryGetValue(key, out var existingScope) && existingScope != null)
                    {
                        foreach (var (scopeKey, scopeValue) in value)
                        {
                            ((Dictionary<string, string>)importMap._scopes[key])[scopeKey] = scopeValue;
                        }
                    }
                    else
                    {
                        importMap._scopes[key] = new Dictionary<string, string>(value);
                    }
                }
            }

            if (item.Integrity != null)
            {
                importMap._integrity ??= [];
                foreach (var (key, value) in item.Integrity)
                {
                    importMap._integrity[key] = value;
                }
            }
        }

        return importMap;
    }

    // Example:
    // "imports": {
    //   "triangle": "./module/shapes/triangle.js",
    //   "pentagram": "https://example.com/shapes/pentagram.js"
    // }
    /// <summary>
    /// Gets the unscoped imports defined in the import map.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Imports { get => _imports; }

    // Example:
    // {
    //   "imports": {
    //     "triangle": "./module/shapes/triangle.js"
    //   },
    //   "scopes": {
    //     "/modules/myshapes/": {
    //       "triangle": "https://example.com/modules/myshapes/triangle.js"
    //     }
    //   }
    // }
    /// <summary>
    /// Gets the scoped imports defined in the import map.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? Scopes { get => _scopes; }

    // Example:
    // <script type="importmap">
    // {
    //   "imports": {
    //     "triangle": "./module/shapes/triangle.js"
    //   },
    //   "integrity": {
    //     "./module/shapes/triangle.js": "sha256-..."
    //   }
    // }
    // </script>
    /// <summary>
    /// Gets the integrity properties defined in the import map.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Integrity { get => _integrity; }

    internal string ToJson()
    {
        _json ??= JsonSerializer.Serialize(this, ImportMapSerializerContext.CustomEncoder.Options);
        return _json;
    }

    /// <inheritdoc />
    public override string ToString() => ToJson();
}
