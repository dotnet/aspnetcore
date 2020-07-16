// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    internal static class HeadManagementRenderTreeBuilderExtensions
    {
        public static void BuildHeadElementComment<TElement>(this RenderTreeBuilder builder, int sequence, TElement element)
        {
            builder.AddMarkupContent(sequence, $"<!--Head:{JsonSerializer.Serialize(element, JsonSerializerOptionsProvider.Options)}-->");
        }
    }
}
