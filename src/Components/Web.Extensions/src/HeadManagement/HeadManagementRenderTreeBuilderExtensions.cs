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
