// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Components
{
    // Metadata used for Components interactions with the tag helper system
    internal static class ComponentMetadata
    {
        private static readonly string MangledClassNamePrefix = "__generated__";

        // There's a bug in the 15.7 preview 1 Razor that prevents 'Kind' from being serialized
        // this affects both tooling and build. For now our workaround is to ignore 'Kind' and
        // use our own metadata entry to denote non-Component tag helpers.
        public static readonly string SpecialKindKey = "Components.IsSpecialKind";

        public static readonly string ImportsFileName = "_Imports.razor";

        public static string MangleClassName(string className)
        {
            if (string.IsNullOrEmpty(className))
            {
                return string.Empty;
            }

            return MangledClassNamePrefix + className;
        }

        public static bool IsMangledClass(string className)
        {
            if (string.IsNullOrEmpty(className))
            {
                return false;
            }

            return className.StartsWith(MangledClassNamePrefix, StringComparison.Ordinal);
        }

        public static class Common
        {
            public static readonly string OriginalAttributeName = "Common.OriginalAttributeName";

            public static readonly string DirectiveAttribute = "Common.DirectiveAttribute";

            public static readonly string AddAttributeMethodName = "Common.AddAttributeMethodName";
        }

        public static class Bind
        {
            public static readonly string RuntimeName = "Components.None";

            public readonly static string TagHelperKind = "Components.Bind";

            public readonly static string FallbackKey = "Components.Bind.Fallback";

            public readonly static string TypeAttribute = "Components.Bind.TypeAttribute";

            public readonly static string ValueAttribute = "Components.Bind.ValueAttribute";

            public readonly static string ChangeAttribute = "Components.Bind.ChangeAttribute";

            public readonly static string ExpressionAttribute = "Components.Bind.ExpressionAttribute";

            public readonly static string IsInvariantCulture = "Components.Bind.IsInvariantCulture";

            public readonly static string Format = "Components.Bind.Format";
        }

        public static class ChildContent
        {
            public static readonly string RuntimeName = "Components.None";

            public static readonly string TagHelperKind = "Components.ChildContent";

            public static readonly string ParameterNameBoundAttributeKind = "Components.ChildContentParameterName";

            /// <summary>
            /// The name of the synthesized attribute used to set a child content parameter.
            /// </summary>
            public static readonly string ParameterAttributeName = "Context";

            /// <summary>
            /// The default name of the child content parameter (unless set by a Context attribute).
            /// </summary>
            public static readonly string DefaultParameterName = "context";
        }

        public static class Component
        {
            public static readonly string ChildContentKey = "Components.ChildContent";

            public static readonly string ChildContentParameterNameKey = "Components.ChildContentParameterName";

            public static readonly string DelegateSignatureKey = "Components.DelegateSignature";

            public static readonly string EventCallbackKey = "Components.EventCallback";

            public static readonly string WeaklyTypedKey = "Components.IsWeaklyTyped";

            public static readonly string RuntimeName = "Components.IComponent";

            public readonly static string TagHelperKind = "Components.Component";

            public readonly static string GenericTypedKey = "Components.GenericTyped";

            public readonly static string TypeParameterKey = "Components.TypeParameter";

            public readonly static string TypeParameterIsCascadingKey = "Components.TypeParameterIsCascading";

            public readonly static string NameMatchKey = "Components.NameMatch";

            public readonly static string FullyQualifiedNameMatch = "Components.FullyQualifiedNameMatch";
        }

        public static class EventHandler
        {
            public static readonly string EventArgsType = "Components.EventHandler.EventArgs";

            public static readonly string RuntimeName = "Components.None";

            public readonly static string TagHelperKind = "Components.EventHandler";

            public static readonly string PreventRender = "Components.EventHandler.PreventRender";
        }

        public static class Key
        {
            public readonly static string TagHelperKind = "Components.Key";

            public static readonly string RuntimeName = "Components.None";
        }

        public static class Splat
        {
            public readonly static string TagHelperKind = "Components.Splat";

            public static readonly string RuntimeName = "Components.None";
        }

        public static class Ref
        {
            public readonly static string TagHelperKind = "Components.Ref";

            public static readonly string RuntimeName = "Components.None";
        }
    }
}
