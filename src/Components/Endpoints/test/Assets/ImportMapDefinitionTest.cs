// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Endpoints.Assets;

public class ImportMapDefinitionTest
{
    [Fact]
    public void CanCreate_Basic_ImportMapDefinition()
    {
        // Arrange
        var expectedJson = """
            {
              "imports": {
                "jquery": "https://cdn.example.com/jquery.js"
              }
            }
            """.Replace("\r\n", "\n");

        var importMapDefinition = new ImportMapDefinition(
            new Dictionary<string, string>
            {
                { "jquery", "https://cdn.example.com/jquery.js" },
            },
            null,
            null
            );

        // Assert
        Assert.Equal(expectedJson, importMapDefinition.ToJson().Replace("\r\n", "\n"));
    }

    [Fact]
    public void CanCreate_Scoped_ImportMapDefinition()
    {
        // Arrange
        var expectedJson = """
            {
              "scopes": {
                "/scoped/": {
                  "jquery": "https://cdn.example.com/jquery.js"
                }
              }
            }
            """.Replace("\r\n", "\n");

        var importMapDefinition = new ImportMapDefinition(
            null,
            new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["/scoped/"] = new Dictionary<string, string>
                {
                    { "jquery", "https://cdn.example.com/jquery.js" },
                }
            },
            null);

        // Assert
        Assert.Equal(expectedJson, importMapDefinition.ToJson().Replace("\r\n", "\n"));
    }

    [Fact]
    public void CanCreate_ImportMap_WithIntegrity()
    {
        // Arrange
        var expectedJson = """
            {
              "imports": {
                "jquery": "https://cdn.example.com/jquery.js"
              },
              "integrity": {
                "https://cdn.example.com/jquery.js": "sha384-abc123"
              }
            }
            """.Replace("\r\n", "\n");

        var importMapDefinition = new ImportMapDefinition(
            new Dictionary<string, string>
            {
                { "jquery", "https://cdn.example.com/jquery.js" },
            },
            null,
            new Dictionary<string, string>
            {
                { "https://cdn.example.com/jquery.js", "sha384-abc123" },
            });

        // Assert
        Assert.Equal(expectedJson, importMapDefinition.ToJson().Replace("\r\n", "\n"));
    }

    [Fact]
    public void CanBuildImportMap_FromResourceCollection()
    {
        // Arrange
        var resourceAssetCollection = new ResourceAssetCollection(
            [
                new ResourceAsset(
                    "jquery.fingerprint.js",
                    [
                        new ResourceAssetProperty("integrity", "sha384-abc123"),
                        new ResourceAssetProperty("label", "jquery.js"),
                    ])
            ]);

        var expectedJson = """
            {
              "imports": {
                "./jquery.js": "./jquery.fingerprint.js"
              },
              "integrity": {
                "jquery.fingerprint.js": "sha384-abc123"
              }
            }
            """.Replace("\r\n", "\n");

        // Act
        var importMapDefinition = ImportMapDefinition.FromResourceCollection(resourceAssetCollection);

        // Assert
        Assert.Equal(expectedJson, importMapDefinition.ToJson().Replace("\r\n", "\n"));
    }

    [Fact]
    public void CanCombine_ImportMaps()
    {
        // Arrange
        var firstImportMap = new ImportMapDefinition(
            new Dictionary<string, string>
            {
                { "jquery", "https://cdn.example.com/jquery.js" },
            },
            new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["/legacy/"] = new Dictionary<string, string>
                {
                    { "jquery", "https://legacy.example.com/jquery.js" },
                }
            },
            new Dictionary<string, string>
            {
                { "https://cdn.example.com/jquery.js", "sha384-abc123" },
            });

        var secondImportMap = new ImportMapDefinition(
            new Dictionary<string, string>
            {
                { "react", "https://cdn.example.com/react.js" },
                { "jquery", "https://updated.example.com/jquery.js" }
            },
            new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["/scoped/"] = new Dictionary<string, string>
                {
                    { "jquery", "https://cdn.example.com/jquery.js" },
                },
                ["/legacy/"] = new Dictionary<string, string>
                {
                    { "jquery", "https://updated.example.com/jquery.js" },
                }
            },
            new Dictionary<string, string>
            {
                { "https://cdn.example.com/react.js", "sha384-def456" },
            });

        var expectedImportMap = """
            {
              "imports": {
                "jquery": "https://updated.example.com/jquery.js",
                "react": "https://cdn.example.com/react.js"
              },
              "scopes": {
                "/legacy/": {
                  "jquery": "https://updated.example.com/jquery.js"
                },
                "/scoped/": {
                  "jquery": "https://cdn.example.com/jquery.js"
                }
              },
              "integrity": {
                "https://cdn.example.com/jquery.js": "sha384-abc123",
                "https://cdn.example.com/react.js": "sha384-def456"
              }
            }
            """.Replace("\r\n", "\n");

        // Act
        var combinedImportMap = ImportMapDefinition.Combine(firstImportMap, secondImportMap);

        // Assert
        Assert.Equal(expectedImportMap, combinedImportMap.ToJson().Replace("\r\n", "\n"));
    }
}
