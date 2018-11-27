// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal static class TagHelperDescriptorExtensions
    {
        public static bool IsBindTagHelper(this TagHelperDescriptor tagHelper)
        {
            if (tagHelper == null)
            {
                throw new ArgumentNullException(nameof(tagHelper));
            }
            
            return 
                tagHelper.Metadata.TryGetValue(BlazorMetadata.SpecialKindKey, out var kind) && 
                string.Equals(BlazorMetadata.Bind.TagHelperKind, kind);
        }

        public static bool IsFallbackBindTagHelper(this TagHelperDescriptor tagHelper)
        {
            if (tagHelper == null)
            {
                throw new ArgumentNullException(nameof(tagHelper));
            }

            return
                tagHelper.IsBindTagHelper() &&
                tagHelper.Metadata.TryGetValue(BlazorMetadata.Bind.FallbackKey, out var fallback) &&
                string.Equals(bool.TrueString, fallback);
        }

        public static bool IsGenericTypedComponent(this TagHelperDescriptor tagHelper)
        {
            if (tagHelper == null)
            {
                throw new ArgumentNullException(nameof(tagHelper));
            }

            return
                IsComponentTagHelper(tagHelper) &&
                tagHelper.Metadata.TryGetValue(BlazorMetadata.Component.GenericTypedKey, out var value) &&
                string.Equals(bool.TrueString, value);
        }

        public static bool IsInputElementBindTagHelper(this TagHelperDescriptor tagHelper)
        {
            if (tagHelper == null)
            {
                throw new ArgumentNullException(nameof(tagHelper));
            }

            return
                tagHelper.IsBindTagHelper() &&
                tagHelper.TagMatchingRules.Count == 1 &&
                string.Equals("input", tagHelper.TagMatchingRules[0].TagName);
        }

        public static bool IsInputElementFallbackBindTagHelper(this TagHelperDescriptor tagHelper)
        {
            if (tagHelper == null)
            {
                throw new ArgumentNullException(nameof(tagHelper));
            }

            return
                tagHelper.IsInputElementBindTagHelper() &&
                !tagHelper.Metadata.ContainsKey(BlazorMetadata.Bind.TypeAttribute);
        }

        public static string GetValueAttributeName(this TagHelperDescriptor tagHelper)
        {
            if (tagHelper == null)
            {
                throw new ArgumentNullException(nameof(tagHelper));
            }

            tagHelper.Metadata.TryGetValue(BlazorMetadata.Bind.ValueAttribute, out var result);
            return result;
        }

        public static string GetChangeAttributeName(this TagHelperDescriptor tagHelper)
        {
            if (tagHelper == null)
            {
                throw new ArgumentNullException(nameof(tagHelper));
            }

            tagHelper.Metadata.TryGetValue(BlazorMetadata.Bind.ChangeAttribute, out var result);
            return result;
        }

        public static bool IsChildContentTagHelper(this TagHelperDescriptor tagHelper)
        {
            if (tagHelper == null)
            {
                throw new ArgumentNullException(nameof(tagHelper));
            }

            return
                tagHelper.Metadata.TryGetValue(BlazorMetadata.SpecialKindKey, out var value) &&
                string.Equals(value, BlazorMetadata.ChildContent.TagHelperKind, StringComparison.Ordinal);
        }

        public static bool IsComponentTagHelper(this TagHelperDescriptor tagHelper)
        {
            if (tagHelper == null)
            {
                throw new ArgumentNullException(nameof(tagHelper));
            }

            return !tagHelper.Metadata.ContainsKey(BlazorMetadata.SpecialKindKey);
        }

        public static bool IsEventHandlerTagHelper(this TagHelperDescriptor tagHelper)
        {
            if (tagHelper == null)
            {
                throw new ArgumentNullException(nameof(tagHelper));
            }

            return
                tagHelper.Metadata.TryGetValue(BlazorMetadata.SpecialKindKey, out var kind) &&
                string.Equals(BlazorMetadata.EventHandler.TagHelperKind, kind);
        }

        public static bool IsRefTagHelper(this TagHelperDescriptor tagHelper)
        {
            if (tagHelper == null)
            {
                throw new ArgumentNullException(nameof(tagHelper));
            }

            return
                tagHelper.Metadata.TryGetValue(BlazorMetadata.SpecialKindKey, out var kind) &&
                string.Equals(BlazorMetadata.Ref.TagHelperKind, kind);
        }

        public static string GetEventArgsType(this TagHelperDescriptor tagHelper)
        {
            if (tagHelper == null)
            {
                throw new ArgumentNullException(nameof(tagHelper));
            }

            tagHelper.Metadata.TryGetValue(BlazorMetadata.EventHandler.EventArgsType, out var result);
            return result;
        }

        /// <summary>
        /// Gets the set of component attributes that can accept child content (<c>RenderFragment</c> or <c>RenderFragment{T}</c>).
        /// </summary>
        /// <param name="tagHelper">The <see cref="TagHelperDescriptor"/>.</param>
        /// <returns>The child content attributes</returns>
        public static IEnumerable<BoundAttributeDescriptor> GetChildContentProperties(this TagHelperDescriptor tagHelper)
        {
            if (tagHelper == null)
            {
                throw new ArgumentNullException(nameof(tagHelper));
            }

            for (var i = 0; i < tagHelper.BoundAttributes.Count; i++)
            {
                var attribute = tagHelper.BoundAttributes[i];
                if (attribute.IsChildContentProperty())
                {
                    yield return attribute;
                }
            }
        }

        /// <summary>
        /// Gets the set of component attributes that represent generic type parameters of the component type.
        /// </summary>
        /// <param name="tagHelper">The <see cref="TagHelperDescriptor"/>.</param>
        /// <returns>The type parameter attributes</returns>
        public static IEnumerable<BoundAttributeDescriptor> GetTypeParameters(this TagHelperDescriptor tagHelper)
        {
            if (tagHelper == null)
            {
                throw new ArgumentNullException(nameof(tagHelper));
            }

            for (var i = 0; i < tagHelper.BoundAttributes.Count; i++)
            {
                var attribute = tagHelper.BoundAttributes[i];
                if (attribute.IsTypeParameterProperty())
                {
                    yield return attribute;
                }
            }
        }
    }
}
