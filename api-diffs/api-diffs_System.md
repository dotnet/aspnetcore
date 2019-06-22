# System

``` diff
 namespace System {
     public sealed class ApplicationIdentity : ISerializable {
         public ApplicationIdentity(string applicationIdentityFullName);
         public string CodeBase { get; }
         public string FullName { get; }
         void System.Runtime.Serialization.ISerializable.GetObjectData(SerializationInfo info, StreamingContext context);
         public override string ToString();
     }
 }
```

