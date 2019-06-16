# Microsoft.AspNetCore.DataProtection.XmlEncryption

``` diff
 namespace Microsoft.AspNetCore.DataProtection.XmlEncryption {
     public class CertificateResolver : ICertificateResolver {
         public CertificateResolver();
         public virtual X509Certificate2 ResolveCertificate(string thumbprint);
     }
     public sealed class CertificateXmlEncryptor : IInternalCertificateXmlEncryptor, IXmlEncryptor {
         public CertificateXmlEncryptor(X509Certificate2 certificate, ILoggerFactory loggerFactory);
         public CertificateXmlEncryptor(string thumbprint, ICertificateResolver certificateResolver, ILoggerFactory loggerFactory);
         public EncryptedXmlInfo Encrypt(XElement plaintextElement);
     }
     public enum DpapiNGProtectionDescriptorFlags {
         MachineKey = 32,
         NamedDescriptor = 1,
         None = 0,
     }
     public sealed class DpapiNGXmlDecryptor : IXmlDecryptor {
         public DpapiNGXmlDecryptor();
         public DpapiNGXmlDecryptor(IServiceProvider services);
         public XElement Decrypt(XElement encryptedElement);
     }
     public sealed class DpapiNGXmlEncryptor : IXmlEncryptor {
         public DpapiNGXmlEncryptor(string protectionDescriptorRule, DpapiNGProtectionDescriptorFlags flags, ILoggerFactory loggerFactory);
         public EncryptedXmlInfo Encrypt(XElement plaintextElement);
     }
     public sealed class DpapiXmlDecryptor : IXmlDecryptor {
         public DpapiXmlDecryptor();
         public DpapiXmlDecryptor(IServiceProvider services);
         public XElement Decrypt(XElement encryptedElement);
     }
     public sealed class DpapiXmlEncryptor : IXmlEncryptor {
         public DpapiXmlEncryptor(bool protectToLocalMachine, ILoggerFactory loggerFactory);
         public EncryptedXmlInfo Encrypt(XElement plaintextElement);
     }
     public sealed class EncryptedXmlDecryptor : IInternalEncryptedXmlDecryptor, IXmlDecryptor {
         public EncryptedXmlDecryptor();
         public EncryptedXmlDecryptor(IServiceProvider services);
         public XElement Decrypt(XElement encryptedElement);
     }
     public sealed class EncryptedXmlInfo {
         public EncryptedXmlInfo(XElement encryptedElement, Type decryptorType);
         public Type DecryptorType { get; }
         public XElement EncryptedElement { get; }
     }
     public interface ICertificateResolver {
         X509Certificate2 ResolveCertificate(string thumbprint);
     }
     public interface IXmlDecryptor {
         XElement Decrypt(XElement encryptedElement);
     }
     public interface IXmlEncryptor {
         EncryptedXmlInfo Encrypt(XElement plaintextElement);
     }
     public sealed class NullXmlDecryptor : IXmlDecryptor {
         public NullXmlDecryptor();
         public XElement Decrypt(XElement encryptedElement);
     }
     public sealed class NullXmlEncryptor : IXmlEncryptor {
         public NullXmlEncryptor();
         public NullXmlEncryptor(IServiceProvider services);
         public EncryptedXmlInfo Encrypt(XElement plaintextElement);
     }
 }
```

