// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Components;

// Constants for method names used in code-generation
// Keep these in sync with the actual definitions
internal static class ComponentsApi
{
    public const string AssemblyName = "Microsoft.AspNetCore.Components";

    public const string AddMultipleAttributesTypeFullName = "global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<string, object>>";

    public static class ComponentBase
    {
        public const string Namespace = "Microsoft.AspNetCore.Components";
        public const string FullTypeName = Namespace + ".ComponentBase";
        public const string MetadataName = FullTypeName;

        public const string BuildRenderTree = nameof(BuildRenderTree);
    }

    public static class ParameterAttribute
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Components.ParameterAttribute";
        public const string MetadataName = FullTypeName;
    }

    public static class LayoutAttribute
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Components.LayoutAttribute";
    }

    public static class InjectAttribute
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Components.InjectAttribute";
    }

    public static class IComponent
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Components.IComponent";

        public const string MetadataName = FullTypeName;
    }

    public static class IDictionary
    {
        public const string MetadataName = "System.Collection.IDictionary`2";
    }

    public static class RenderFragment
    {
        public const string Namespace = "Microsoft.AspNetCore.Components";
        public const string FullTypeName = Namespace + ".RenderFragment";
        public const string MetadataName = FullTypeName;
    }

    public static class RenderFragmentOfT
    {
        public const string Namespace = "Microsoft.AspNetCore.Components";
        public const string FullTypeName = Namespace + ".RenderFragment<>";
        public const string MetadataName = Namespace + ".RenderFragment`1";
    }

    public static class RenderTreeBuilder
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder";

        public const string BuilderParameter = "__builder";

        public const string OpenElement = nameof(OpenElement);

        public const string CloseElement = nameof(CloseElement);

        public const string OpenComponent = nameof(OpenComponent);

        public const string CloseComponent = nameof(CloseComponent);

        public const string AddMarkupContent = nameof(AddMarkupContent);

        public const string AddContent = nameof(AddContent);

        public const string AddAttribute = nameof(AddAttribute);

        public const string AddMultipleAttributes = nameof(AddMultipleAttributes);

        public const string AddElementReferenceCapture = nameof(AddElementReferenceCapture);

        public const string AddComponentReferenceCapture = nameof(AddComponentReferenceCapture);

        public const string Clear = nameof(Clear);

        public const string GetFrames = nameof(GetFrames);

        public const string ChildContent = nameof(ChildContent);

        public const string SetKey = nameof(SetKey);

        public const string SetUpdatesAttributeName = nameof(SetUpdatesAttributeName);

        public const string AddEventPreventDefaultAttribute = nameof(AddEventPreventDefaultAttribute);

        public const string AddEventStopPropagationAttribute = nameof(AddEventStopPropagationAttribute);
    }

    public static class RuntimeHelpers
    {
        public const string TypeCheck = "global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck";
        public const string CreateInferredEventCallback = "global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.CreateInferredEventCallback";
    }

    public static class RouteAttribute
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Components.RouteAttribute";
    }

    public static class BindElementAttribute
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Components.BindElementAttribute";
    }

    public static class BindInputElementAttribute
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Components.BindInputElementAttribute";
    }

    public static class EventHandlerAttribute
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Components.EventHandlerAttribute";
    }

    public static class ElementReference
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Components.ElementReference";
    }

    public static class EventCallback
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Components.EventCallback";
        public const string MetadataName = FullTypeName;

        public const string FactoryAccessor = FullTypeName + ".Factory";
    }

    public static class EventCallbackOfT
    {
        public const string MetadataName = "Microsoft.AspNetCore.Components.EventCallback`1";
    }

    public static class EventCallbackFactory
    {
        public const string CreateMethod = "Create";
        public const string CreateBinderMethod = "CreateBinder";
    }

    public static class BindConverter
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Components.BindConverter";
        public const string FormatValue = "Microsoft.AspNetCore.Components.BindConverter.FormatValue";
    }

    public static class CascadingTypeParameterAttribute
    {
        public const string MetadataName = "Microsoft.AspNetCore.Components.CascadingTypeParameterAttribute";
    }
}
