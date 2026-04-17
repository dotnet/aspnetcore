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
/// Downloads a released NuGet package once, loads the ManagedAuthenticatedEncryptor
/// from it via reflection in an isolated AssemblyLoadContext, and creates encryptors
/// that use the same key material as current source code for cross-version testing.
/// </summary>
internal sealed class NuGetEncryptorFactory : IDisposable
{
    private const string NuGetBaseUrl = "https://api.nuget.org/v3-flatcontainer";
    private const string NuGetPackageId = "Microsoft.AspNetCore.DataProtection";

    private readonly string _tempDir;
    private readonly string _packageVersion;

    private Assembly? _nugetAssembly;
    private Type? _encryptorType;
    private Type? _secretType;
    private ConstructorInfo? _encryptorCtor;
    private object? _genRandomInstance;

    public NuGetEncryptorFactory(string packageVersion)
    {
        _packageVersion = packageVersion;
        _tempDir = Path.Combine(Path.GetTempPath(), $"dp-nuget-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    /// <summary>
    /// Downloads and extracts the NuGet package, loading the DLL into an isolated ALC.
    /// Call once before creating encryptors.
    /// </summary>
    public async Task InitializeAsync()
    {
        var dllPath = await DownloadAndExtractNuGetDll();
        var alc = new AssemblyLoadContext($"NuGet-{_packageVersion}", isCollectible: true);
        _nugetAssembly = alc.LoadFromAssemblyPath(dllPath);

        // Resolve types using typeof().FullName for refactor safety
        _secretType = _nugetAssembly.GetType(typeof(Secret).FullName!)
            ?? throw new InvalidOperationException($"Cannot find {nameof(Secret)} type in NuGet assembly");
        _encryptorType = _nugetAssembly.GetType(typeof(ManagedAuthenticatedEncryptor).FullName!)
            ?? throw new InvalidOperationException($"Cannot find {nameof(ManagedAuthenticatedEncryptor)} type in NuGet assembly");

        _encryptorCtor = _encryptorType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(c => c.GetParameters().Length == 5)
            ?? throw new InvalidOperationException($"Cannot find {nameof(ManagedAuthenticatedEncryptor)} 5-param constructor");

        var genRandomImplType = _nugetAssembly.GetType(typeof(ManagedGenRandomImpl).FullName!)
            ?? throw new InvalidOperationException($"Cannot find {nameof(ManagedGenRandomImpl)} type in NuGet assembly");
        _genRandomInstance = genRandomImplType.GetField("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
            ?? throw new InvalidOperationException($"Cannot find {nameof(ManagedGenRandomImpl)}.Instance");
    }

    /// <summary>
    /// Creates a ManagedAuthenticatedEncryptor from the downloaded NuGet assembly
    /// using reflection and the same key material (all-zero 512-bit key).
    /// </summary>
    public NuGetEncryptorWrapper CreateEncryptor()
    {
        if (_nugetAssembly is null)
        {
            throw new InvalidOperationException("Call InitializeAsync() before creating encryptors.");
        }

        var secretCtor = _secretType!.GetConstructor([typeof(byte[])])
            ?? throw new InvalidOperationException("Cannot find Secret(byte[]) constructor");
        var secret = secretCtor.Invoke([new byte[512 / 8]]);

        Func<SymmetricAlgorithm> symFactory = Aes.Create;
        Func<KeyedHashAlgorithm> hmacFactory = () => new HMACSHA256();

        var encryptor = _encryptorCtor!.Invoke([secret, symFactory, 256 / 8, hmacFactory, _genRandomInstance!]);
        return new NuGetEncryptorWrapper(encryptor, _encryptorType!);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
        }
    }

    private async Task<string> DownloadAndExtractNuGetDll()
    {
        var nupkgPath = Path.Combine(_tempDir, $"{NuGetPackageId}.{_packageVersion}.nupkg");
        var extractDir = Path.Combine(_tempDir, "extracted");

        using var http = new HttpClient();
        var url = $"{NuGetBaseUrl}/{NuGetPackageId.ToLowerInvariant()}/{_packageVersion}/{NuGetPackageId.ToLowerInvariant()}.{_packageVersion}.nupkg";
        var bytes = await http.GetByteArrayAsync(url);
        await File.WriteAllBytesAsync(nupkgPath, bytes);
        ZipFile.ExtractToDirectory(nupkgPath, extractDir);

        // Prefer the highest netX.0 TFM (not netstandard, not net4xx)
        var libDir = Path.Combine(extractDir, "lib");
        var bestTfm = Directory.GetDirectories(libDir)
            .Select(d => Path.GetFileName(d)!)
            .Where(t => t.StartsWith("net", StringComparison.Ordinal)
                     && !t.StartsWith("netstandard", StringComparison.Ordinal)
                     && !t.StartsWith("net4", StringComparison.Ordinal))
            .OrderByDescending(t => t)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("No suitable TFM found in NuGet package");

        var dllPath = Path.Combine(libDir, bestTfm, $"{NuGetPackageId}.dll");
        if (!File.Exists(dllPath))
        {
            throw new FileNotFoundException($"Expected DLL at {dllPath}");
        }

        return dllPath;
    }
}

/// <summary>
/// Wraps a ManagedAuthenticatedEncryptor loaded from a NuGet assembly,
/// calling Encrypt/Decrypt via reflection to avoid type identity conflicts.
/// </summary>
internal sealed class NuGetEncryptorWrapper(object encryptor, Type encryptorType)
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
