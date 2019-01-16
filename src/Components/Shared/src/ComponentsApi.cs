// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Shared
{
    // Constants for method names used in code-generation
    // Keep these in sync with the actual definitions
    internal static class ComponentsApi
    {
        public static readonly string AssemblyName = "Microsoft.AspNetCore.Components";

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
            public static readonly string FullTypeName = "Microsoft.AspNetCore.Components.Layouts.LayoutAttribute";
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
            public static readonly string FullTypeName = "Microsoft.AspNetCore.Components.RenderTree.RenderTreeBuilder";

            public static readonly string OpenElement = nameof(OpenElement);

            public static readonly string CloseElement = nameof(CloseElement);

            public static readonly string OpenComponent = nameof(OpenComponent);

            public static readonly string CloseComponent = nameof(CloseComponent);

            public static readonly string AddMarkupContent = nameof(AddMarkupContent);

            public static readonly string AddContent = nameof(AddContent);

            public static readonly string AddAttribute = nameof(AddAttribute);

            public static readonly string AddElementReferenceCapture = nameof(AddElementReferenceCapture);

            public static readonly string AddComponentReferenceCapture = nameof(AddComponentReferenceCapture);

            public static readonly string Clear = nameof(Clear);

            public static readonly string GetFrames = nameof(GetFrames);

            public static readonly string ChildContent = nameof(ChildContent);
        }

        public static class RuntimeHelpers
        {
            public static readonly string TypeCheck = "Microsoft.AspNetCore.Components.RuntimeHelpers.TypeCheck";
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

        public static class BindMethods
        {
            public static readonly string FullTypeName = "Microsoft.AspNetCore.Components.BindMethods";

            public static readonly string GetValue = "Microsoft.AspNetCore.Components.BindMethods.GetValue";

            public static readonly string GetEventHandlerValue = "Microsoft.AspNetCore.Components.BindMethods.GetEventHandlerValue";

            public static readonly string SetValueHandler = "Microsoft.AspNetCore.Components.BindMethods.SetValueHandler";
        }

        public static class EventHandlerAttribute
        {
            public static readonly string FullTypeName = "Microsoft.AspNetCore.Components.EventHandlerAttribute";
        }

        public static class ElementRef
        {
            public static readonly string FullTypeName = "Microsoft.AspNetCore.Components.ElementRef";
        }
    }
}
