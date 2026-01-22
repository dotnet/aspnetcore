// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Certificates.Generation;

namespace Microsoft.AspNetCore.Internal.Tests;

internal sealed class TestCertificateManager : CertificateManager
{
    private readonly Dictionary<StoreKey, List<InMemoryCertificateEntry>> _stores = new();
    private readonly Dictionary<string, byte[]> _createdFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _createdDirectories = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<X509Certificate2> _correctedCertificates = new();
    private readonly List<X509Certificate2> _trustedCertificates = new();
    private readonly Dictionary<string, bool> _exportableByThumbprint = new(StringComparer.OrdinalIgnoreCase);

    public TestCertificateManager(
        IDictionary<StoreKey, IEnumerable<X509Certificate2>>? initialStores = null,
        string? subject = null,
        int? generatedVersion = null,
        int? minimumVersion = null)
        : base(
            subject ?? LocalhostHttpsDistinguishedName,
            generatedVersion ?? CurrentAspNetCoreCertificateVersion,
            minimumVersion ?? CurrentMinimumAspNetCoreCertificateVersion)
    {
        if (initialStores is null)
        {
            return;
        }

        foreach (var (storeKey, certificates) in initialStores)
        {
            foreach (var certificate in certificates)
            {
                AddCertificate(storeKey.StoreName, storeKey.StoreLocation, certificate);
            }
        }
    }

    public TrustLevel TrustResult { get; set; } = TrustLevel.Full;

    public Func<X509Certificate2, CheckCertificateStateResult>? CheckCertificateStateOverride { get; set; }

    public IReadOnlyDictionary<string, byte[]> CreatedFiles => _createdFiles;

    public IReadOnlyCollection<string> CreatedDirectories => _createdDirectories;

    public IReadOnlyList<X509Certificate2> CorrectedCertificates => _correctedCertificates;

    public IReadOnlyList<X509Certificate2> TrustedCertificates => _trustedCertificates;

    public void AddCertificate(StoreName storeName, StoreLocation storeLocation, X509Certificate2 certificate, bool? isExportable = null)
    {
        var entry = new InMemoryCertificateEntry(certificate, isExportable);
        var store = GetOrCreateStore(storeName, storeLocation);
        store.Add(entry);
        if (!string.IsNullOrEmpty(entry.Thumbprint))
        {
            _exportableByThumbprint[entry.Thumbprint] = entry.Exportable;
        }
    }

    public IReadOnlyList<X509Certificate2> GetStoreCertificates(StoreName storeName, StoreLocation storeLocation)
    {
        if (!_stores.TryGetValue(new StoreKey(storeName, storeLocation), out var entries))
        {
            return Array.Empty<X509Certificate2>();
        }

        return entries.Select(entry => entry.CreateCertificate()).ToArray();
    }

    public void RemoveStoreCertificates(StoreName storeName, StoreLocation storeLocation)
    {
        if (_stores.Remove(new StoreKey(storeName, storeLocation), out var entries))
        {
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.Thumbprint))
                {
                    _exportableByThumbprint.Remove(entry.Thumbprint);
                }
            }
        }
    }

    public bool TryGetCreatedFile(string path, out byte[] bytes)
    {
        if (_createdFiles.TryGetValue(path, out var value))
        {
            bytes = (byte[])value.Clone();
            return true;
        }

        bytes = Array.Empty<byte>();
        return false;
    }

    public X509Certificate2 CreateDevelopmentCertificateWithVersion(int generatedVersion, DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        var previousVersion = AspNetHttpsCertificateVersion;
        var previousMinimumVersion = MinimumAspNetHttpsCertificateVersion;
        if (generatedVersion < MinimumAspNetHttpsCertificateVersion)
        {
            MinimumAspNetHttpsCertificateVersion = generatedVersion;
        }

        AspNetHttpsCertificateVersion = generatedVersion;
        try
        {
            return CreateAspNetCoreHttpsDevelopmentCertificate(notBefore, notAfter);
        }
        finally
        {
            AspNetHttpsCertificateVersion = previousVersion;
            MinimumAspNetHttpsCertificateVersion = previousMinimumVersion;
        }
    }

    internal void ExportCertificateToMemory(X509Certificate2 certificate, string path, bool includePrivateKey, string? password, CertificateKeyExportFormat format)
    {
        if (Log.IsEnabled())
        {
            Log.ExportCertificateStart(GetDescription(certificate), path, includePrivateKey);
        }

        if (includePrivateKey && password is null)
        {
            Log.NoPasswordForCertificate();
        }

        var targetDirectoryPath = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(targetDirectoryPath))
        {
            Log.CreateExportCertificateDirectory(targetDirectoryPath);
            CreateDirectoryWithPermissions(targetDirectoryPath);
        }

        byte[] bytes;
        byte[] keyBytes;
        byte[]? pemEnvelope = null;
        RSA? key = null;

        try
        {
            if (includePrivateKey)
            {
                switch (format)
                {
                    case CertificateKeyExportFormat.Pfx:
                        bytes = certificate.Export(X509ContentType.Pkcs12, password);
                        break;
                    case CertificateKeyExportFormat.Pem:
                        key = certificate.GetRSAPrivateKey()!;

                        char[] pem;
                        if (password != null)
                        {
                            keyBytes = key.ExportEncryptedPkcs8PrivateKey(password, new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 100000));
                            pem = PemEncoding.Write("ENCRYPTED PRIVATE KEY", keyBytes);
                            pemEnvelope = Encoding.ASCII.GetBytes(pem);
                        }
                        else
                        {
                            keyBytes = key.ExportEncryptedPkcs8PrivateKey(string.Empty, new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 1));
                            pem = PemEncoding.Write("ENCRYPTED PRIVATE KEY", keyBytes);
                            key.Dispose();
                            key = RSA.Create();
                            key.ImportFromEncryptedPem(pem, string.Empty);
                            Array.Clear(keyBytes, 0, keyBytes.Length);
                            Array.Clear(pem, 0, pem.Length);
                            keyBytes = key.ExportPkcs8PrivateKey();
                            pem = PemEncoding.Write("PRIVATE KEY", keyBytes);
                            pemEnvelope = Encoding.ASCII.GetBytes(pem);
                        }

                        Array.Clear(keyBytes, 0, keyBytes.Length);
                        Array.Clear(pem, 0, pem.Length);

                        bytes = Encoding.ASCII.GetBytes(PemEncoding.Write("CERTIFICATE", certificate.Export(X509ContentType.Cert)));
                        break;
                    default:
                        throw new InvalidOperationException("Unknown format.");
                }
            }
            else
            {
                if (format == CertificateKeyExportFormat.Pem)
                {
                    bytes = Encoding.ASCII.GetBytes(PemEncoding.Write("CERTIFICATE", certificate.Export(X509ContentType.Cert)));
                }
                else
                {
                    bytes = certificate.Export(X509ContentType.Cert);
                }
            }
        }
        catch (Exception e) when (Log.IsEnabled())
        {
            Log.ExportCertificateError(e.ToString());
            throw;
        }
        finally
        {
            key?.Dispose();
        }

        try
        {
            Log.WriteCertificateToDisk(path);
            AddCreatedFile(path, bytes);
        }
        catch (Exception ex) when (Log.IsEnabled())
        {
            Log.WriteCertificateToDiskError(ex.ToString());
            throw;
        }
        finally
        {
            Array.Clear(bytes, 0, bytes.Length);
        }

        if (includePrivateKey && format == CertificateKeyExportFormat.Pem)
        {
            if (pemEnvelope is null)
            {
                throw new InvalidOperationException("Missing PEM key envelope.");
            }

            try
            {
                var keyPath = Path.ChangeExtension(path, ".key");
                Log.WritePemKeyToDisk(keyPath);
                AddCreatedFile(keyPath, pemEnvelope);
            }
            catch (Exception ex) when (Log.IsEnabled())
            {
                Log.WritePemKeyToDiskError(ex.ToString());
                throw;
            }
            finally
            {
                Array.Clear(pemEnvelope, 0, pemEnvelope.Length);
            }
        }
    }

    protected override void PopulateCertificatesFromStore(X509Store store, List<X509Certificate2> certificates, bool requireExportable)
    {
        if (!Enum.TryParse<StoreName>(store.Name, ignoreCase: true, out var storeName))
        {
            return;
        }

        var storeKey = new StoreKey(storeName, store.Location);
        if (!_stores.TryGetValue(storeKey, out var entries))
        {
            return;
        }

        foreach (var entry in entries)
        {
            certificates.Add(entry.CreateCertificate());
        }
    }

    protected override X509Certificate2 SaveCertificateCore(X509Certificate2 certificate, StoreName storeName, StoreLocation storeLocation)
    {
        AddCertificate(storeName, storeLocation, certificate, isExportable: true);
        return certificate;
    }

    protected override TrustLevel TrustCertificateCore(X509Certificate2 certificate)
    {
        _trustedCertificates.Add(certificate);
        AddCertificate(StoreName.Root, StoreLocation.CurrentUser, certificate, isExportable: true);
        return TrustResult;
    }

    public override TrustLevel GetTrustLevel(X509Certificate2 certificate)
    {
        return IsCertificateInStore(StoreName.Root, certificate) ? TrustLevel.Full : TrustLevel.None;
    }

    internal override bool IsExportable(X509Certificate2 c)
    {
        if (!string.IsNullOrEmpty(c.Thumbprint) && _exportableByThumbprint.TryGetValue(c.Thumbprint, out var exportable))
        {
            return exportable;
        }

        return c.HasPrivateKey;
    }

    protected override void RemoveCertificateFromTrustedRoots(X509Certificate2 certificate)
    {
        RemoveFromStores(StoreName.Root, certificate);
    }

    protected override IList<X509Certificate2> GetCertificatesToRemove(StoreName storeName, StoreLocation storeLocation)
    {
        if (!_stores.TryGetValue(new StoreKey(storeName, storeLocation), out var entries))
        {
            return Array.Empty<X509Certificate2>();
        }

        return entries.Select(entry => entry.CreateCertificate()).ToArray();
    }

    protected override void CreateDirectoryWithPermissions(string directoryPath)
    {
        _createdDirectories.Add(directoryPath);
    }

    protected override void RemoveCertificateFromUserStoreCore(X509Certificate2 certificate)
    {
        RemoveFromStore(StoreName.My, StoreLocation.CurrentUser, certificate);
    }

    internal override CheckCertificateStateResult CheckCertificateState(X509Certificate2 candidate)
    {
        return CheckCertificateStateOverride?.Invoke(candidate) ?? new CheckCertificateStateResult(success: true, failureMessage: null);
    }

    internal override void CorrectCertificateState(X509Certificate2 candidate)
    {
        _correctedCertificates.Add(candidate);
    }

    private List<InMemoryCertificateEntry> GetOrCreateStore(StoreName storeName, StoreLocation storeLocation)
    {
        var key = new StoreKey(storeName, storeLocation);
        if (!_stores.TryGetValue(key, out var entries))
        {
            entries = new List<InMemoryCertificateEntry>();
            _stores[key] = entries;
        }

        return entries;
    }

    private void AddCreatedFile(string path, byte[] bytes)
    {
        _createdFiles[path] = (byte[])bytes.Clone();
    }

    private void RemoveFromStores(StoreName storeName, X509Certificate2 certificate)
    {
        foreach (var key in _stores.Keys.Where(key => key.StoreName == storeName).ToArray())
        {
            RemoveFromStore(key.StoreName, key.StoreLocation, certificate);
        }
    }

    private bool IsCertificateInStore(StoreName storeName, X509Certificate2 certificate)
    {
        foreach (var entry in _stores.Where(pair => pair.Key.StoreName == storeName).SelectMany(pair => pair.Value))
        {
            if (string.Equals(entry.SerialNumber, certificate.SerialNumber, StringComparison.OrdinalIgnoreCase) ||
                certificate.RawDataMemory.Span.SequenceEqual(entry.CertBytes))
            {
                return true;
            }
        }

        return false;
    }

    private void RemoveFromStore(StoreName storeName, StoreLocation storeLocation, X509Certificate2 certificate)
    {
        if (!_stores.TryGetValue(new StoreKey(storeName, storeLocation), out var entries))
        {
            return;
        }

        for (var i = entries.Count - 1; i >= 0; i--)
        {
            var entry = entries[i];
            if (string.Equals(entry.SerialNumber, certificate.SerialNumber, StringComparison.OrdinalIgnoreCase) ||
                certificate.RawDataMemory.Span.SequenceEqual(entry.CertBytes))
            {
                entries.RemoveAt(i);
                if (!string.IsNullOrEmpty(entry.Thumbprint))
                {
                    _exportableByThumbprint.Remove(entry.Thumbprint);
                }
            }
        }
    }

    internal readonly record struct StoreKey(StoreName StoreName, StoreLocation StoreLocation);

    private sealed class InMemoryCertificateEntry
    {
        public InMemoryCertificateEntry(X509Certificate2 certificate, bool? isExportable)
        {
            SerialNumber = certificate.SerialNumber;
            Thumbprint = certificate.Thumbprint ?? string.Empty;
            Exportable = (isExportable ?? certificate.HasPrivateKey) && certificate.HasPrivateKey;
            CertBytes = certificate.Export(X509ContentType.Cert);
            if (Exportable)
            {
                PfxBytes = certificate.Export(X509ContentType.Pkcs12);
            }
        }

        public string SerialNumber { get; }

        public string Thumbprint { get; }

        public bool Exportable { get; }

        public byte[] CertBytes { get; }

        public byte[]? PfxBytes { get; }

        public X509Certificate2 CreateCertificate()
        {
            if (PfxBytes != null)
            {
                try
                {
                    return new X509Certificate2(PfxBytes, (string?)null, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
                }
                catch (PlatformNotSupportedException)
                {
                    return new X509Certificate2(PfxBytes, (string?)null, X509KeyStorageFlags.Exportable);
                }
            }

            return new X509Certificate2(CertBytes);
        }
    }
}
