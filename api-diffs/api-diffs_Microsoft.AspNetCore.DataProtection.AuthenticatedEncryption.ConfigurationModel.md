# Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel

``` diff
 namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel {
     public abstract class AlgorithmConfiguration {
         protected AlgorithmConfiguration();
         public abstract IAuthenticatedEncryptorDescriptor CreateNewDescriptor();
     }
     public sealed class AuthenticatedEncryptorConfiguration : AlgorithmConfiguration, IInternalAlgorithmConfiguration {
         public AuthenticatedEncryptorConfiguration();
         public EncryptionAlgorithm EncryptionAlgorithm { get; set; }
         public ValidationAlgorithm ValidationAlgorithm { get; set; }
         public override IAuthenticatedEncryptorDescriptor CreateNewDescriptor();
     }
     public sealed class AuthenticatedEncryptorDescriptor : IAuthenticatedEncryptorDescriptor {
         public AuthenticatedEncryptorDescriptor(AuthenticatedEncryptorConfiguration configuration, ISecret masterKey);
         public XmlSerializedDescriptorInfo ExportToXml();
     }
     public sealed class AuthenticatedEncryptorDescriptorDeserializer : IAuthenticatedEncryptorDescriptorDeserializer {
         public AuthenticatedEncryptorDescriptorDeserializer();
         public IAuthenticatedEncryptorDescriptor ImportFromXml(XElement element);
     }
     public sealed class CngCbcAuthenticatedEncryptorConfiguration : AlgorithmConfiguration, IInternalAlgorithmConfiguration {
         public CngCbcAuthenticatedEncryptorConfiguration();
         public string EncryptionAlgorithm { get; set; }
         public int EncryptionAlgorithmKeySize { get; set; }
         public string EncryptionAlgorithmProvider { get; set; }
         public string HashAlgorithm { get; set; }
         public string HashAlgorithmProvider { get; set; }
         public override IAuthenticatedEncryptorDescriptor CreateNewDescriptor();
     }
     public sealed class CngCbcAuthenticatedEncryptorDescriptor : IAuthenticatedEncryptorDescriptor {
         public CngCbcAuthenticatedEncryptorDescriptor(CngCbcAuthenticatedEncryptorConfiguration configuration, ISecret masterKey);
         public XmlSerializedDescriptorInfo ExportToXml();
     }
     public sealed class CngCbcAuthenticatedEncryptorDescriptorDeserializer : IAuthenticatedEncryptorDescriptorDeserializer {
         public CngCbcAuthenticatedEncryptorDescriptorDeserializer();
         public IAuthenticatedEncryptorDescriptor ImportFromXml(XElement element);
     }
     public sealed class CngGcmAuthenticatedEncryptorConfiguration : AlgorithmConfiguration, IInternalAlgorithmConfiguration {
         public CngGcmAuthenticatedEncryptorConfiguration();
         public string EncryptionAlgorithm { get; set; }
         public int EncryptionAlgorithmKeySize { get; set; }
         public string EncryptionAlgorithmProvider { get; set; }
         public override IAuthenticatedEncryptorDescriptor CreateNewDescriptor();
     }
     public sealed class CngGcmAuthenticatedEncryptorDescriptor : IAuthenticatedEncryptorDescriptor {
         public CngGcmAuthenticatedEncryptorDescriptor(CngGcmAuthenticatedEncryptorConfiguration configuration, ISecret masterKey);
         public XmlSerializedDescriptorInfo ExportToXml();
     }
     public sealed class CngGcmAuthenticatedEncryptorDescriptorDeserializer : IAuthenticatedEncryptorDescriptorDeserializer {
         public CngGcmAuthenticatedEncryptorDescriptorDeserializer();
         public IAuthenticatedEncryptorDescriptor ImportFromXml(XElement element);
     }
     public interface IAuthenticatedEncryptorDescriptor {
         XmlSerializedDescriptorInfo ExportToXml();
     }
     public interface IAuthenticatedEncryptorDescriptorDeserializer {
         IAuthenticatedEncryptorDescriptor ImportFromXml(XElement element);
     }
     public sealed class ManagedAuthenticatedEncryptorConfiguration : AlgorithmConfiguration, IInternalAlgorithmConfiguration {
         public ManagedAuthenticatedEncryptorConfiguration();
         public int EncryptionAlgorithmKeySize { get; set; }
         public Type EncryptionAlgorithmType { get; set; }
         public Type ValidationAlgorithmType { get; set; }
         public override IAuthenticatedEncryptorDescriptor CreateNewDescriptor();
     }
     public sealed class ManagedAuthenticatedEncryptorDescriptor : IAuthenticatedEncryptorDescriptor {
         public ManagedAuthenticatedEncryptorDescriptor(ManagedAuthenticatedEncryptorConfiguration configuration, ISecret masterKey);
         public XmlSerializedDescriptorInfo ExportToXml();
     }
     public sealed class ManagedAuthenticatedEncryptorDescriptorDeserializer : IAuthenticatedEncryptorDescriptorDeserializer {
         public ManagedAuthenticatedEncryptorDescriptorDeserializer();
         public IAuthenticatedEncryptorDescriptor ImportFromXml(XElement element);
     }
     public static class XmlExtensions {
         public static void MarkAsRequiresEncryption(this XElement element);
     }
     public sealed class XmlSerializedDescriptorInfo {
         public XmlSerializedDescriptorInfo(XElement serializedDescriptorElement, Type deserializerType);
         public Type DeserializerType { get; }
         public XElement SerializedDescriptorElement { get; }
     }
 }
```

