// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Components
{
    // Constants for method names used in code-generation
    // Keep these in sync with the actual definitions
    internal static class ComponentsApi
    {
        public static readonly string AssemblyName = "Microsoft.AspNetCore.Components";

        public static readonly string AddMultipleAttributesTypeFullName = "global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<string, object>>";

        public static class ComponentBase
        {
            public static readonly string Namespace = "Microsoft.AspNetCore.Components";
            public static readonly string FullTypeName = Namespace + ".ComponentBase";
            public static readonly string MetadataName = FullTypeName;

            public static readonly string BuildRenderTree = nameof(BuildRenderTree);
        }

        public static class ParameterAttribute
        {
            public static readonly string FullTypeName = "Microsoft.AspNetCore.Components.ParameterAttribute";
            public static readonly string MetadataName = FullTypeName;
        }

        public static class LayoutAttribute
        {
            public static readonly string FullTypeName = "Microsoft.AspNetCore.Components.LayoutAttribute";
        }

        public static class InjectAttribute
        {
            public static readonly string FullTypeName = "Microsoft.AspNetCore.Components.InjectAttribute";
        }

        public static class IComponent
        {
            public static readonly string FullTypeName = "Microsoft.AspNetCore.Components.IComponent";

            public static readonly string MetadataName = FullTypeName;
        }

        public static class IDictionary
        {
            public static readonly string MetadataName = "System.Collection.IDictionary`2";
        }

        public static class RenderFragment
        {
            public static readonly string Namespace = "Microsoft.AspNetCore.Components";
            public static readonly string FullTypeName = Namespace + ".RenderFragment";
            public static readonly string MetadataName = FullTypeName;
        }

        public static class RenderFragmentOfT
        {
            public static readonly string Namespace = "Microsoft.AspNetCore.Components";
            public static readonly string FullTypeName = Namespace + ".RenderFragment<>";
            public static readonly string MetadataName = Namespace + ".RenderFragment`1";
        }

        public static class RenderTreeBuilder
        {
            public static readonly string FullTypeName = "Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder";

            public static readonly string BuilderParameter = "__builder";

            public static readonly string OpenElement = nameof(OpenElement);

            public static readonly string CloseElement = nameof(CloseElement);

            public static readonly string OpenComponent = nameof(OpenComponent);

            public static readonly string CloseComponent = nameof(CloseComponent);

            public static readonly string AddMarkupContent = nameof(AddMarkupContent);

            public static readonly string AddContent = nameof(AddContent);

            public static readonly string AddAttribute = nameof(AddAttribute);

            public static readonly string AddMultipleAttributes = nameof(AddMultipleAttributes);

            public static readonly string AddElementReferenceCapture = nameof(AddElementReferenceCapture);

            public static readonly string AddComponentReferenceCapture = nameof(AddComponentReferenceCapture);

            public static readonly string Clear = nameof(Clear);

            public static readonly string GetFrames = nameof(GetFrames);

            public static readonly string ChildContent = nameof(ChildContent);

            public static readonly string SetKey = nameof(SetKey);

            public static readonly string SetUpdatesAttributeName = nameof(SetUpdatesAttributeName);

            public static readonly string AddEventPreventDefaultAttribute = nameof(AddEventPreventDefaultAttribute);

            public static readonly string AddEventStopPropagationAttribute = nameof(AddEventStopPropagationAttribute);
        }

        public static class RuntimeHelpers
        {
            public static readonly string TypeCheck = "Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck";
            public static readonly string CreateInferredEventCallback = "Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.CreateInferredEventCallback";
        }

        public static class RouteAttribute
        {
            public static readonly string FullTypeName = "Microsoft.AspNetCore.Components.RouteAttribute";
        }

        public static class BindElementAttribute
        {
            public static readonly string FullTypeName = "Microsoft.AspNetCore.Components.BindElementAttribute";
        }

        public static class BindInputElementAttribute
        {
            public static readonly string FullTypeName = "Microsoft.AspNetCore.Components.BindInputElementAttribute";
        }

        public static class EventHandlerAttribute
        {
            public static readonly string FullTypeName = "Microsoft.AspNetCore.Components.EventHandlerAttribute";
        }

        public static class ElementReference
        {
            public static readonly string FullTypeName = "Microsoft.AspNetCore.Components.ElementReference";
        }

        public static class EventCallback
        {
            public static readonly string FullTypeName = "Microsoft.AspNetCore.Components.EventCallback";
            public static readonly string MetadataName = FullTypeName;

            public static readonly string FactoryAccessor = FullTypeName + ".Factory";
        }

        public static class EventCallbackOfT
        {
            public static readonly string MetadataName = "Microsoft.AspNetCore.Components.EventCallback`1";
        }

        public static class EventCallbackFactory
        {
            public static readonly string CreateMethod = "Create";
            public static readonly string CreateBinderMethod = "CreateBinder";
        }

        public static class BindConverter
        {
            public static readonly string FullTypeName = "Microsoft.AspNetCore.Components.BindConverter";
            public static readonly string FormatValue = "Microsoft.AspNetCore.Components.BindConverter.FormatValue";
        }

        public static class CascadingTypeParameterAttribute
        {
            public static readonly string MetadataName = "Microsoft.AspNetCore.Components.CascadingTypeParameterAttribute";
        }
    }
}
