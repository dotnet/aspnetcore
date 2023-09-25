using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;

namespace BlazorWeb_CSharp.Components.Identity;

public static class PageLayoutTypeExtensions
{
    public static bool PageHasLayout<TLayout>(this HttpContext? httpContext) where TLayout : LayoutComponentBase
    {
        var pageTypeMetadata = httpContext?.GetEndpoint()?.Metadata.GetMetadata<ComponentTypeMetadata>();
        return pageTypeMetadata is not null && ComponentTypeHasLayout<TLayout>(pageTypeMetadata.Type);
    }

    private static bool ComponentTypeHasLayout<TLayout>(Type componentType) where TLayout : LayoutComponentBase
    {
        var layoutType = componentType.GetCustomAttribute<LayoutAttribute>()?.LayoutType;
        if (layoutType is null)
        {
            return false;
        }
        else if (layoutType.IsAssignableTo(typeof(TLayout)))
        {
            return true;
        }
        else
        {
            return ComponentTypeHasLayout<TLayout>(layoutType);
        }
    }
}
