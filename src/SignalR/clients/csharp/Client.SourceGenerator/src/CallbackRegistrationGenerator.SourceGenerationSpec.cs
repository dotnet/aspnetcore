// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.SignalR.Client.SourceGenerator
{
    internal partial class CallbackRegistrationGenerator
    {
        public class SourceGenerationSpec
        {
            public List<TypeSpec> Types = new();
        }

        public class TypeSpec
        {
            public string TypeName;
            public List<MethodSpec> Methods = new();
            public Location CallSite;
            public string FullyQualifiedTypeName;
        }

        public class MethodSpec
        {
            public string Name;
            public List<ArgumentSpec> Arguments = new();
            public SupportClassification Support;
            public string? SupportHint;
        }

        public enum SupportClassification
        {
            Supported,
            UnsupportedReturnType
        }

        public class ArgumentSpec
        {
            public string Name;
            public string FullyQualifiedTypeName;
        }
    }
}
