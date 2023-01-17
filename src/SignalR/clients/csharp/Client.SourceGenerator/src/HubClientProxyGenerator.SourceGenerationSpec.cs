// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.SignalR.Client.SourceGenerator;

internal partial class HubClientProxyGenerator
{
    public sealed class SourceGenerationSpec
    {
        public string? SetterNamespace;
        public string? SetterClassName;
        public string? SetterMethodName;
        public string? SetterTypeParameterName;
        public string? SetterHubConnectionParameterName;
        public string? SetterProviderParameterName;
        public string? SetterMethodAccessibility;
        public string? SetterClassAccessibility;
        public List<TypeSpec> Types = new();
    }

    public sealed class TypeSpec
    {
        public string TypeName;
        public List<MethodSpec> Methods = new();
        public Location CallSite;
        public string FullyQualifiedTypeName;
    }

    public sealed class MethodSpec
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

    public sealed class ArgumentSpec
    {
        public string Name;
        public string FullyQualifiedTypeName;
    }
}
