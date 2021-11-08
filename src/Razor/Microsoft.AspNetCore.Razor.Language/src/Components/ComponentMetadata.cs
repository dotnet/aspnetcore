// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Components;

// Metadata used for Components interactions with the tag helper system
internal static class ComponentMetadata
{
    private static readonly StringSegment MangledClassNamePrefix = "__generated__";

    // There's a bug in the 15.7 preview 1 Razor that prevents 'Kind' from being serialized
    // this affects both tooling and build. For now our workaround is to ignore 'Kind' and
    // use our own metadata entry to denote non-Component tag helpers.
    public const string SpecialKindKey = "Components.IsSpecialKind";

    public const string ImportsFileName = "_Imports.razor";

    public static string MangleClassName(string className)
    {
        if (string.IsNullOrEmpty(className))
        {
            return string.Empty;
        }

        return MangledClassNamePrefix + className;
    }

    public static bool IsMangledClass(StringSegment className)
    {
        return className.StartsWith(MangledClassNamePrefix, StringComparison.Ordinal);
    }

    public static class Common
    {
        public const string OriginalAttributeName = "Common.OriginalAttributeName";

        public const string DirectiveAttribute = "Common.DirectiveAttribute";

        public const string AddAttributeMethodName = "Common.AddAttributeMethodName";
    }

    public static class Bind
    {
        public const string RuntimeName = "Components.None";

        public const string TagHelperKind = "Components.Bind";

        public const string FallbackKey = "Components.Bind.Fallback";

        public const string TypeAttribute = "Components.Bind.TypeAttribute";

        public const string ValueAttribute = "Components.Bind.ValueAttribute";

        public const string ChangeAttribute = "Components.Bind.ChangeAttribute";

        public const string ExpressionAttribute = "Components.Bind.ExpressionAttribute";

        public const string IsInvariantCulture = "Components.Bind.IsInvariantCulture";

        public const string Format = "Components.Bind.Format";
    }

    public static class ChildContent
    {
        public const string RuntimeName = "Components.None";

        public const string TagHelperKind = "Components.ChildContent";

        public const string ParameterNameBoundAttributeKind = "Components.ChildContentParameterName";

        /// <summary>
        /// The name of the synthesized attribute used to set a child content parameter.
        /// </summary>
        public const string ParameterAttributeName = "Context";

        /// <summary>
        /// The default name of the child content parameter (unless set by a Context attribute).
        /// </summary>
        public const string DefaultParameterName = "context";
    }

    public static class Component
    {
        public const string ChildContentKey = "Components.ChildContent";

        public const string ChildContentParameterNameKey = "Components.ChildContentParameterName";

        public const string DelegateSignatureKey = "Components.DelegateSignature";

        public const string EventCallbackKey = "Components.EventCallback";

        public const string WeaklyTypedKey = "Components.IsWeaklyTyped";

        public const string RuntimeName = "Components.IComponent";

        public const string TagHelperKind = "Components.Component";

        public const string GenericTypedKey = "Components.GenericTyped";

        public const string TypeParameterKey = "Components.TypeParameter";

        public const string TypeParameterIsCascadingKey = "Components.TypeParameterIsCascading";

        public const string TypeParameterConstraintsKey = "Component.TypeParameterConstraints";

        public const string NameMatchKey = "Components.NameMatch";

        public const string FullyQualifiedNameMatch = "Components.FullyQualifiedNameMatch";
    }

    public static class EventHandler
    {
        public const string EventArgsType = "Components.EventHandler.EventArgs";

        public const string RuntimeName = "Components.None";

        public const string TagHelperKind = "Components.EventHandler";
    }

    public static class Key
    {
        public const string TagHelperKind = "Components.Key";

        public const string RuntimeName = "Components.None";
    }

    public static class Splat
    {
        public const string TagHelperKind = "Components.Splat";

        public const string RuntimeName = "Components.None";
    }

    public static class Ref
    {
        public const string TagHelperKind = "Components.Ref";

        public const string RuntimeName = "Components.None";
    }
}
