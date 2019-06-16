# Microsoft.AspNetCore.DataProtection

``` diff
 namespace Microsoft.AspNetCore.DataProtection {
     public static class DataProtectionAdvancedExtensions {
         public static byte[] Protect(this ITimeLimitedDataProtector protector, byte[] plaintext, TimeSpan lifetime);
         public static string Protect(this ITimeLimitedDataProtector protector, string plaintext, DateTimeOffset expiration);
         public static string Protect(this ITimeLimitedDataProtector protector, string plaintext, TimeSpan lifetime);
         public static ITimeLimitedDataProtector ToTimeLimitedDataProtector(this IDataProtector protector);
         public static string Unprotect(this ITimeLimitedDataProtector protector, string protectedData, out DateTimeOffset expiration);
     }
     public static class DataProtectionBuilderExtensions {
         public static IDataProtectionBuilder AddKeyEscrowSink(this IDataProtectionBuilder builder, IKeyEscrowSink sink);
         public static IDataProtectionBuilder AddKeyEscrowSink(this IDataProtectionBuilder builder, Func<IServiceProvider, IKeyEscrowSink> factory);
         public static IDataProtectionBuilder AddKeyEscrowSink<TImplementation>(this IDataProtectionBuilder builder) where TImplementation : class, IKeyEscrowSink;
         public static IDataProtectionBuilder AddKeyManagementOptions(this IDataProtectionBuilder builder, Action<KeyManagementOptions> setupAction);
         public static IDataProtectionBuilder DisableAutomaticKeyGeneration(this IDataProtectionBuilder builder);
         public static IDataProtectionBuilder PersistKeysToFileSystem(this IDataProtectionBuilder builder, DirectoryInfo directory);
         public static IDataProtectionBuilder PersistKeysToRegistry(this IDataProtectionBuilder builder, RegistryKey registryKey);
         public static IDataProtectionBuilder ProtectKeysWithCertificate(this IDataProtectionBuilder builder, X509Certificate2 certificate);
         public static IDataProtectionBuilder ProtectKeysWithCertificate(this IDataProtectionBuilder builder, string thumbprint);
         public static IDataProtectionBuilder ProtectKeysWithDpapi(this IDataProtectionBuilder builder);
         public static IDataProtectionBuilder ProtectKeysWithDpapi(this IDataProtectionBuilder builder, bool protectToLocalMachine);
         public static IDataProtectionBuilder ProtectKeysWithDpapiNG(this IDataProtectionBuilder builder);
         public static IDataProtectionBuilder ProtectKeysWithDpapiNG(this IDataProtectionBuilder builder, string protectionDescriptorRule, DpapiNGProtectionDescriptorFlags flags);
         public static IDataProtectionBuilder SetApplicationName(this IDataProtectionBuilder builder, string applicationName);
         public static IDataProtectionBuilder SetDefaultKeyLifetime(this IDataProtectionBuilder builder, TimeSpan lifetime);
         public static IDataProtectionBuilder UnprotectKeysWithAnyCertificate(this IDataProtectionBuilder builder, params X509Certificate2[] certificates);
         public static IDataProtectionBuilder UseCryptographicAlgorithms(this IDataProtectionBuilder builder, AuthenticatedEncryptorConfiguration configuration);
         public static IDataProtectionBuilder UseCustomCryptographicAlgorithms(this IDataProtectionBuilder builder, CngCbcAuthenticatedEncryptorConfiguration configuration);
         public static IDataProtectionBuilder UseCustomCryptographicAlgorithms(this IDataProtectionBuilder builder, CngGcmAuthenticatedEncryptorConfiguration configuration);
         public static IDataProtectionBuilder UseCustomCryptographicAlgorithms(this IDataProtectionBuilder builder, ManagedAuthenticatedEncryptorConfiguration configuration);
         public static IDataProtectionBuilder UseEphemeralDataProtectionProvider(this IDataProtectionBuilder builder);
     }
     public static class DataProtectionCommonExtensions {
         public static IDataProtector CreateProtector(this IDataProtectionProvider provider, IEnumerable<string> purposes);
         public static IDataProtector CreateProtector(this IDataProtectionProvider provider, string purpose, params string[] subPurposes);
         public static IDataProtectionProvider GetDataProtectionProvider(this IServiceProvider services);
         public static IDataProtector GetDataProtector(this IServiceProvider services, IEnumerable<string> purposes);
         public static IDataProtector GetDataProtector(this IServiceProvider services, string purpose, params string[] subPurposes);
         public static string Protect(this IDataProtector protector, string plaintext);
         public static string Unprotect(this IDataProtector protector, string protectedData);
     }
     public class DataProtectionOptions {
         public DataProtectionOptions();
         public string ApplicationDiscriminator { get; set; }
     }
     public static class DataProtectionProvider {
         public static IDataProtectionProvider Create(DirectoryInfo keyDirectory);
         public static IDataProtectionProvider Create(DirectoryInfo keyDirectory, Action<IDataProtectionBuilder> setupAction);
         public static IDataProtectionProvider Create(DirectoryInfo keyDirectory, Action<IDataProtectionBuilder> setupAction, X509Certificate2 certificate);
         public static IDataProtectionProvider Create(DirectoryInfo keyDirectory, X509Certificate2 certificate);
         public static IDataProtectionProvider Create(string applicationName);
         public static IDataProtectionProvider Create(string applicationName, X509Certificate2 certificate);
     }
     public static class DataProtectionUtilityExtensions {
         public static string GetApplicationUniqueIdentifier(this IServiceProvider services);
     }
     public sealed class EphemeralDataProtectionProvider : IDataProtectionProvider {
         public EphemeralDataProtectionProvider();
         public EphemeralDataProtectionProvider(ILoggerFactory loggerFactory);
         public IDataProtector CreateProtector(string purpose);
     }
     public interface IDataProtectionBuilder {
         IServiceCollection Services { get; }
     }
     public interface IDataProtectionProvider {
         IDataProtector CreateProtector(string purpose);
     }
     public interface IDataProtector : IDataProtectionProvider {
         byte[] Protect(byte[] plaintext);
         byte[] Unprotect(byte[] protectedData);
     }
     public interface IPersistedDataProtector : IDataProtectionProvider, IDataProtector {
         byte[] DangerousUnprotect(byte[] protectedData, bool ignoreRevocationErrors, out bool requiresMigration, out bool wasRevoked);
     }
     public interface ISecret : IDisposable {
         int Length { get; }
         void WriteSecretIntoBuffer(ArraySegment<byte> buffer);
     }
     public interface ITimeLimitedDataProtector : IDataProtectionProvider, IDataProtector {
         new ITimeLimitedDataProtector CreateProtector(string purpose);
         byte[] Protect(byte[] plaintext, DateTimeOffset expiration);
         byte[] Unprotect(byte[] protectedData, out DateTimeOffset expiration);
     }
     public sealed class Secret : IDisposable, ISecret {
         public Secret(ISecret secret);
         public Secret(ArraySegment<byte> value);
         public unsafe Secret(byte* secret, int secretLength);
         public Secret(byte[] value);
         public int Length { get; }
         public void Dispose();
         public static Secret Random(int numBytes);
         public void WriteSecretIntoBuffer(ArraySegment<byte> buffer);
         public unsafe void WriteSecretIntoBuffer(byte* buffer, int bufferLength);
     }
 }
```

