// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection.Managed;

namespace Microsoft.AspNetCore.DataProtection.NuGetIntegrationTests;

/// <summary>
/// Downloads a released NuGet package (and its Cryptography.Internal dependency) once,
/// then creates ManagedAuthenticatedEncryptor instances from any TFM in the package
/// via reflection in isolated AssemblyLoadContexts.
/// Implements <see cref="IAsyncLifetime"/> for xUnit's <c>IClassFixture</c>.
/// </summary>
public sealed class NuGetEncryptorFactory : IAsyncLifetime, IDisposable
{
    public const string DefaultPackageVersion = "9.0.15";

    private const string NuGetBaseUrl = "https://api.nuget.org/v3-flatcontainer";
    private const string DataProtectionPackageId = "Microsoft.AspNetCore.DataProtection";
    private const string CryptoInternalPackageId = "Microsoft.AspNetCore.Cryptography.Internal";

    private readonly string _packageVersion;
    private readonly string _tempDir;

    // Per-TFM directories containing DataProtection + Cryptography.Internal DLLs
    private readonly Dictionary<string, string> _tfmDirs = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Parameterless constructor used by xUnit's IClassFixture.</summary>
    public NuGetEncryptorFactory() : this(DefaultPackageVersion) { }

    internal NuGetEncryptorFactory(string packageVersion)
    {
        _packageVersion = packageVersion;
        _tempDir = Path.Combine(Path.GetTempPath(), $"dp-nuget-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    /// <summary>
    /// Downloads and extracts NuGet packages. Called once by xUnit when used as IClassFixture.
    /// </summary>
    public async Task InitializeAsync()
    {
        var dpLibDir = await DownloadAndExtractPackage(DataProtectionPackageId);
        var cryptoLibDir = await DownloadAndExtractPackage(CryptoInternalPackageId);

        // For each TFM in the DataProtection package, stage a directory with both DLLs
        foreach (var tfmDir in Directory.GetDirectories(dpLibDir))
        {
            var tfm = Path.GetFileName(tfmDir)!;
            var stageDir = Path.Combine(_tempDir, "staged", tfm);
            Directory.CreateDirectory(stageDir);

            // Copy DataProtection DLL
            foreach (var dll in Directory.GetFiles(tfmDir, "*.dll"))
            {
                File.Copy(dll, Path.Combine(stageDir, Path.GetFileName(dll)));
            }

            // Copy matching Cryptography.Internal DLL (same TFM if available, else best match)
            var cryptoTfmDir = FindBestMatchingTfmDir(cryptoLibDir, tfm);
            if (cryptoTfmDir is not null)
            {
                foreach (var dll in Directory.GetFiles(cryptoTfmDir, "*.dll"))
                {
                    var dest = Path.Combine(stageDir, Path.GetFileName(dll));
                    if (!File.Exists(dest))
                    {
                        File.Copy(dll, dest);
                    }
                }
            }

            _tfmDirs[tfm] = stageDir;
        }
    }

    public Task DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns the TFM folder names available (e.g. "net9.0", "net462").
    /// </summary>
    public string[] GetAvailableTargetFrameworks()
    {
        EnsureInitialized();
        return [.. _tfmDirs.Keys];
    }

    /// <summary>
    /// Creates a ManagedAuthenticatedEncryptor from the specified TFM's DLL, loaded in an
    /// isolated AssemblyLoadContext with proper dependency resolution.
    /// Uses the same key material (all-zero 512-bit key) for cross-version testing.
    /// </summary>
    public NuGetEncryptorWrapper CreateEncryptor(string? targetFramework = null)
    {
        EnsureInitialized();

        var tfm = targetFramework ?? PickBestModernTfm();
        if (!_tfmDirs.TryGetValue(tfm, out var stageDir))
        {
            throw new InvalidOperationException($"TFM '{tfm}' not available. Available: {string.Join(", ", _tfmDirs.Keys)}");
        }

        var dllPath = Path.Combine(stageDir, $"{DataProtectionPackageId}.dll");
        if (!File.Exists(dllPath))
        {
            throw new FileNotFoundException($"No DLL found at {dllPath}");
        }

        return CreateEncryptorFromDirectory(stageDir, dllPath, $"NuGet-{_packageVersion}-{tfm}");
    }

    /// <summary>
    /// Creates a ManagedAuthenticatedEncryptor from the source-built net462 DLL (the #else path).
    /// The DLL is loaded via ALC from the local build artifacts directory.
    /// </summary>
    public NuGetEncryptorWrapper CreateSourceBuiltNetFxEncryptor()
    {
        // Walk up from test output to find the DataProtection net462 build output.
        // Test output:     artifacts/bin/Microsoft.AspNetCore.DataProtection.NuGetIntegrationTests/Debug/net11.0/
        // Source output:   artifacts/bin/Microsoft.AspNetCore.DataProtection/Debug/net462/
        var testAssemblyDir = Path.GetDirectoryName(typeof(NuGetEncryptorFactory).Assembly.Location)!;
        var artifactsBinDir = Path.GetFullPath(Path.Combine(testAssemblyDir, "..", "..", ".."));
        var netFxDir = Path.Combine(artifactsBinDir, "Microsoft.AspNetCore.DataProtection", "Debug", "net462");
        var dllPath = Path.Combine(netFxDir, $"{DataProtectionPackageId}.dll");

        if (!File.Exists(dllPath))
        {
            throw new FileNotFoundException(
                $"Source-built net462 DLL not found at {dllPath}. " +
                $"Ensure the DataProtection project is built for net462.");
        }

        return CreateEncryptorFromDirectory(netFxDir, dllPath, "SourceBuilt-net462");
    }

    private static NuGetEncryptorWrapper CreateEncryptorFromDirectory(string directory, string dllPath, string alcName)
    {
        var alc = new DirectoryAssemblyLoadContext(directory, alcName);
        var assembly = alc.LoadFromAssemblyPath(dllPath);

        var secretType = assembly.GetType(typeof(Secret).FullName!)
            ?? throw new InvalidOperationException($"Cannot find {nameof(Secret)} in assembly ({alcName})");
        var encryptorType = assembly.GetType(typeof(ManagedAuthenticatedEncryptor).FullName!)
            ?? throw new InvalidOperationException($"Cannot find {nameof(ManagedAuthenticatedEncryptor)} in assembly ({alcName})");

        var encryptorCtor = encryptorType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(c => c.GetParameters().Length == 5)
            ?? throw new InvalidOperationException($"Cannot find {nameof(ManagedAuthenticatedEncryptor)} 5-param constructor ({alcName})");

        var genRandomImplType = assembly.GetType(typeof(ManagedGenRandomImpl).FullName!)
            ?? throw new InvalidOperationException($"Cannot find {nameof(ManagedGenRandomImpl)} in assembly ({alcName})");
        var genRandomInstance = genRandomImplType.GetField("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
            ?? throw new InvalidOperationException($"Cannot find {nameof(ManagedGenRandomImpl)}.Instance ({alcName})");

        var secretCtor = secretType.GetConstructor([typeof(byte[])])
            ?? throw new InvalidOperationException($"Cannot find Secret(byte[]) constructor ({alcName})");
        var secret = secretCtor.Invoke([new byte[512 / 8]]);

        Func<SymmetricAlgorithm> symFactory = Aes.Create;
        Func<KeyedHashAlgorithm> hmacFactory = () => new HMACSHA256();

        var encryptor = encryptorCtor.Invoke([secret, symFactory, 256 / 8, hmacFactory, genRandomInstance]);
        return new NuGetEncryptorWrapper(encryptor, encryptorType);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private void EnsureInitialized()
    {
        if (_tfmDirs.Count == 0)
        {
            throw new InvalidOperationException("Call InitializeAsync() before using the factory.");
        }
    }

    private string PickBestModernTfm()
    {
        return _tfmDirs.Keys
            .Where(t => t.StartsWith("net", StringComparison.Ordinal)
                     && !t.StartsWith("netstandard", StringComparison.Ordinal)
                     && !t.StartsWith("net4", StringComparison.Ordinal))
            .OrderByDescending(t => t)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("No suitable TFM found in NuGet package");
    }

    private async Task<string> DownloadAndExtractPackage(string packageId)
    {
        var extractDir = Path.Combine(_tempDir, packageId);
        var nupkgPath = Path.Combine(_tempDir, $"{packageId}.{_packageVersion}.nupkg");

        using var http = new HttpClient();
        var url = $"{NuGetBaseUrl}/{packageId.ToLowerInvariant()}/{_packageVersion}/{packageId.ToLowerInvariant()}.{_packageVersion}.nupkg";
        var bytes = await http.GetByteArrayAsync(url);
        await File.WriteAllBytesAsync(nupkgPath, bytes);
        ZipFile.ExtractToDirectory(nupkgPath, extractDir);

        var libDir = Path.Combine(extractDir, "lib");
        if (!Directory.Exists(libDir))
        {
            throw new InvalidOperationException($"No lib/ folder found in {packageId} {_packageVersion}");
        }

        return libDir;
    }

    /// <summary>
    /// Find the best matching TFM directory for a dependency package.
    /// Exact match first, then fall back to compatible TFMs.
    /// </summary>
    private static string? FindBestMatchingTfmDir(string libDir, string targetTfm)
    {
        var exact = Path.Combine(libDir, targetTfm);
        if (Directory.Exists(exact))
        {
            return exact;
        }

        // For netX.0 targets, try netstandard2.0 as fallback
        var netstandard = Path.Combine(libDir, "netstandard2.0");
        if (Directory.Exists(netstandard))
        {
            return netstandard;
        }

        // Return first available TFM as last resort
        return Directory.GetDirectories(libDir).FirstOrDefault();
    }

    /// <summary>
    /// AssemblyLoadContext that resolves dependencies from a directory before falling back
    /// to the default context. This is needed when loading older NuGet DLLs that depend on
    /// matching versions of Cryptography.Internal (which has different API surface per major version).
    /// </summary>
    private sealed class DirectoryAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly string _directory;

        public DirectoryAssemblyLoadContext(string directory, string name) : base(name, isCollectible: true)
        {
            _directory = directory;
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var candidate = Path.Combine(_directory, $"{assemblyName.Name}.dll");
            if (File.Exists(candidate))
            {
                return LoadFromAssemblyPath(candidate);
            }

            // Fall back to default context for framework assemblies
            return null;
        }
    }
}

/// <summary>
/// Wraps a ManagedAuthenticatedEncryptor loaded from a NuGet assembly,
/// calling Encrypt/Decrypt via reflection to avoid type identity conflicts.
/// </summary>
public sealed class NuGetEncryptorWrapper(object encryptor, Type encryptorType)
{
    private readonly object _encryptor = encryptor;
    private readonly MethodInfo _encrypt = encryptorType.GetMethod("Encrypt", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Cannot find Encrypt method");
    private readonly MethodInfo _decrypt = encryptorType.GetMethod("Decrypt", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Cannot find Decrypt method");

    public byte[] Encrypt(ArraySegment<byte> plaintext, ArraySegment<byte> aad)
        => (byte[])_encrypt.Invoke(_encryptor, [plaintext, aad])!;

    public byte[] Decrypt(ArraySegment<byte> ciphertext, ArraySegment<byte> aad)
        => (byte[])_decrypt.Invoke(_encryptor, [ciphertext, aad])!;
}
