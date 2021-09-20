// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.SignalR.Client.SourceGenerator
{
    internal partial class CallbackRegistrationGenerator
    {
        public class SourceGenerationSpec
        {
            public string? RegisterCallbackProviderNamespace;
            public string? RegisterCallbackProviderClassName;
            public string? RegisterCallbackProviderMethodName;
            public string? RegisterCallbackProviderTypeParameterName;
            public string? RegisterCallbackProviderConnectionParameterName;
            public string? RegisterCallbackProviderProviderParameterName;
            public string? RegisterCallbackProviderMethodAccessibility;
            public string? RegisterCallbackProviderClassAccessibility;
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
