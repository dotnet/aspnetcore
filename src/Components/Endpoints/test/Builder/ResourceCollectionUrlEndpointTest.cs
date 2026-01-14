// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints;

public class ResourceCollectionUrlEndpointTest
{
    [Fact]
    public void ComputeFingerprintSuffix_IncludesIntegrityForNonFingerprintedAssets()
    {
        // Arrange - Create a collection with non-fingerprinted assets that have integrity hashes
        ResourceAsset[] resources =
        [
            // Non-fingerprinted asset with integrity (simulates WasmFingerprintAssets=false scenario)
            new("/_framework/MyApp.dll",
            [
                new("integrity", "sha256-ABC123")
            ]),
            new("/_framework/System.dll",
            [
                new("integrity", "sha256-XYZ789")
            ])
        ];
        var collection = new ResourceAssetCollection(resources);

        // Act
        var fingerprint1 = ResourceCollectionUrlEndpoint.ComputeFingerprintSuffix(collection);

        // Arrange - Change the integrity of one asset (simulates content change between builds)
        resources =
        [
            new("/_framework/MyApp.dll",
            [
                new("integrity", "sha256-CHANGED")
            ]),
            new("/_framework/System.dll",
            [
                new("integrity", "sha256-XYZ789")
            ])
        ];
        collection = new ResourceAssetCollection(resources);

        // Act
        var fingerprint2 = ResourceCollectionUrlEndpoint.ComputeFingerprintSuffix(collection);

        // Assert - Fingerprints should be different because integrity changed
        Assert.NotEqual(fingerprint1, fingerprint2);
    }

    [Fact]
    public void ComputeFingerprintSuffix_DoesNotIncludeIntegrityForFingerprintedAssets()
    {
        // Arrange - Create a collection with fingerprinted assets (have label property)
        ResourceAsset[] resources =
        [
            // Fingerprinted asset with label (simulates WasmFingerprintAssets=true scenario)
            new("/_framework/MyApp.ABC123.dll",
            [
                new("label", "MyApp.dll"),
                new("integrity", "sha256-ABC123")
            ]),
            new("/_framework/System.XYZ789.dll",
            [
                new("label", "System.dll"),
                new("integrity", "sha256-XYZ789")
            ])
        ];
        var collection = new ResourceAssetCollection(resources);

        // Act
        var fingerprint1 = ResourceCollectionUrlEndpoint.ComputeFingerprintSuffix(collection);

        // Arrange - Change the integrity (but not the URL) of one asset
        // For fingerprinted assets, the URL already contains the hash, so we don't need to include integrity
        resources =
        [
            new("/_framework/MyApp.ABC123.dll",
            [
                new("label", "MyApp.dll"),
                new("integrity", "sha256-CHANGED")
            ]),
            new("/_framework/System.XYZ789.dll",
            [
                new("label", "System.dll"),
                new("integrity", "sha256-XYZ789")
            ])
        ];
        collection = new ResourceAssetCollection(resources);

        // Act
        var fingerprint2 = ResourceCollectionUrlEndpoint.ComputeFingerprintSuffix(collection);

        // Assert - Fingerprints should be the same because for fingerprinted assets,
        // the URL (not integrity) is what matters, and the URL didn't change
        Assert.Equal(fingerprint1, fingerprint2);
    }

    [Fact]
    public void ComputeFingerprintSuffix_HandlesMixedAssets()
    {
        // Arrange - Mix of fingerprinted and non-fingerprinted assets
        ResourceAsset[] resources =
        [
            // Fingerprinted asset
            new("/_framework/MyApp.ABC123.dll",
            [
                new("label", "MyApp.dll"),
                new("integrity", "sha256-ABC123")
            ]),
            // Non-fingerprinted asset
            new("/_framework/custom.js",
            [
                new("integrity", "sha256-CUSTOM")
            ])
        ];
        var collection = new ResourceAssetCollection(resources);

        // Act
        var fingerprint1 = ResourceCollectionUrlEndpoint.ComputeFingerprintSuffix(collection);

        // Arrange - Change only the non-fingerprinted asset's integrity
        resources =
        [
            // Fingerprinted asset (same as before)
            new("/_framework/MyApp.ABC123.dll",
            [
                new("label", "MyApp.dll"),
                new("integrity", "sha256-ABC123")
            ]),
            // Non-fingerprinted asset with changed integrity
            new("/_framework/custom.js",
            [
                new("integrity", "sha256-MODIFIED")
            ])
        ];
        collection = new ResourceAssetCollection(resources);

        // Act
        var fingerprint2 = ResourceCollectionUrlEndpoint.ComputeFingerprintSuffix(collection);

        // Assert - Fingerprints should be different because non-fingerprinted asset's integrity changed
        Assert.NotEqual(fingerprint1, fingerprint2);
    }

    [Fact]
    public void ComputeFingerprintSuffix_HandlesAssetsWithNoProperties()
    {
        // Arrange
        ResourceAsset[] resources =
        [
            new("/_framework/file1.dll", null),
            new("/_framework/file2.dll", [])
        ];
        var collection = new ResourceAssetCollection(resources);

        // Act & Assert - Should not throw
        var fingerprint = ResourceCollectionUrlEndpoint.ComputeFingerprintSuffix(collection);
        Assert.NotNull(fingerprint);
        Assert.StartsWith(".", fingerprint);
    }

    [Fact]
    public void ComputeFingerprintSuffix_ChangesWhenNonFingerprintedAssetIntegrityChanges()
    {
        // This test simulates the original bug scenario:
        // When WasmFingerprintAssets=false, changing a file (e.g., Counter.razor or wwwroot/styles.css)
        // updates its integrity hash but not its URL. The resource-collection fingerprint must change
        // to prevent serving stale cached resource-collection.js with outdated integrity values.

        // Arrange - Initial state: non-fingerprinted asset with original integrity
        var collection1 = new ResourceAssetCollection(
        [
            new("/_framework/app.styles.css",
            [
                new("integrity", "sha256-OriginalHash123456")
            ])
        ]);

        // Act - Compute initial fingerprint
        var fingerprintSuffix1 = ResourceCollectionUrlEndpoint.ComputeFingerprintSuffix(collection1);

        // Arrange - Simulate content change: same URL, different integrity (e.g., after modifying styles.css)
        var collection2 = new ResourceAssetCollection(
        [
            new("/_framework/app.styles.css",
            [
                new("integrity", "sha256-ModifiedHash789012")
            ])
        ]);

        // Act - Compute fingerprint after content change
        var fingerprintSuffix2 = ResourceCollectionUrlEndpoint.ComputeFingerprintSuffix(collection2);

        // Assert - Fingerprint must be different to ensure browser fetches updated resource-collection.js
        Assert.NotEqual(fingerprintSuffix1, fingerprintSuffix2);
    }
}
