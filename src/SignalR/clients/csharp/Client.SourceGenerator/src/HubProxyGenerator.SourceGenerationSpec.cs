using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.SignalR.Client.SourceGenerator
{
    internal partial class HubProxyGenerator
    {
        public class SourceGenerationSpec
        {
            public List<ClassSpec> Classes = new();
        }

        public class ClassSpec
        {
            public string FullyQualifiedInterfaceTypeName;
            public string InterfaceTypeName;
            public string ClassTypeName;
            public List<MethodSpec> Methods = new();
            public Location CallSite;
            public string FullyQualifiedClassTypeName =>
                $"Microsoft.AspNetCore.SignalR.Client.SourceGenerated.{ClassTypeName}";
        }

        public class MethodSpec
        {
            public string Name;
            public string FullyQualifiedReturnTypeName;
            public List<ArgumentSpec> Arguments = new();
            public SupportClassification Support;
            public StreamSpec Stream;
            public string? InnerReturnTypeName;
            public bool IsReturnTypeValueTask => FullyQualifiedReturnTypeName.StartsWith("System.Threading.Tasks.ValueTask", StringComparison.Ordinal);
        }

        [Flags]
        public enum StreamSpec
        {
            None = 0,
            ClientToServer = 1,
            ServerToClient = 2,
            AsyncEnumerable = 4,
            Bidirectional = ClientToServer | ServerToClient
        }

        public enum SupportClassification
        {
            Supported,
            UnsupportedReturnType,
            MultipleClientToServerStreams,
            StreamTypeMismatch
        }

        public class ArgumentSpec
        {
            public string Name;
            public string FullyQualifiedTypeName;
        }
    }
}
