// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.RenderTree
{
    // https://github.com/dotnet/arcade/pull/2033
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Explicit)]
    public readonly partial struct RenderTreeFrame
    {
        [System.Runtime.InteropServices.FieldOffsetAttribute(8)]
        public readonly int AttributeEventHandlerId;
        [System.Runtime.InteropServices.FieldOffsetAttribute(16)]
        public readonly string AttributeName;
        [System.Runtime.InteropServices.FieldOffsetAttribute(24)]
        public readonly object AttributeValue;
        [System.Runtime.InteropServices.FieldOffsetAttribute(12)]
        public readonly int ComponentId;
        [System.Runtime.InteropServices.FieldOffsetAttribute(16)]
        public readonly System.Action<object> ComponentReferenceCaptureAction;
        [System.Runtime.InteropServices.FieldOffsetAttribute(8)]
        public readonly int ComponentReferenceCaptureParentFrameIndex;
        [System.Runtime.InteropServices.FieldOffsetAttribute(8)]
        public readonly int ComponentSubtreeLength;
        [System.Runtime.InteropServices.FieldOffsetAttribute(16)]
        public readonly System.Type ComponentType;
        [System.Runtime.InteropServices.FieldOffsetAttribute(16)]
        public readonly string ElementName;
        [System.Runtime.InteropServices.FieldOffsetAttribute(24)]
        public readonly System.Action<Microsoft.AspNetCore.Components.ElementReference> ElementReferenceCaptureAction;
        [System.Runtime.InteropServices.FieldOffsetAttribute(16)]
        public readonly string ElementReferenceCaptureId;
        [System.Runtime.InteropServices.FieldOffsetAttribute(8)]
        public readonly int ElementSubtreeLength;
        [System.Runtime.InteropServices.FieldOffsetAttribute(4)]
        public readonly Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrameType FrameType;
        [System.Runtime.InteropServices.FieldOffsetAttribute(16)]
        public readonly string MarkupContent;
        [System.Runtime.InteropServices.FieldOffsetAttribute(8)]
        public readonly int RegionSubtreeLength;
        [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
        public readonly int Sequence;
        [System.Runtime.InteropServices.FieldOffsetAttribute(16)]
        public readonly string TextContent;
        public Microsoft.AspNetCore.Components.IComponent Component { get { throw null; } }
        public override string ToString() { throw null; }
    }
}

// Built-in components: https://github.com/aspnet/AspNetCore/issues/8825
namespace Microsoft.AspNetCore.Components
{
    public partial class AuthorizeView : Microsoft.AspNetCore.Components.AuthorizeViewCore
    {
        public AuthorizeView() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public string Policy { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public object Resource { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public string Roles { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; } }
        protected override Microsoft.AspNetCore.Authorization.IAuthorizeData[] GetAuthorizeData() { throw null; }
    }

    public abstract partial class AuthorizeViewCore : Microsoft.AspNetCore.Components.ComponentBase
    {
        public AuthorizeViewCore() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment<Microsoft.AspNetCore.Components.AuthenticationState> Authorized { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment Authorizing { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment<Microsoft.AspNetCore.Components.AuthenticationState> ChildContent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment<Microsoft.AspNetCore.Components.AuthenticationState> NotAuthorized { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; } }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.RenderTree.RenderTreeBuilder builder) { }
        protected abstract Microsoft.AspNetCore.Authorization.IAuthorizeData[] GetAuthorizeData();
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task OnParametersSetAsync() { throw null; }
    }

    public partial class CascadingAuthenticationState : Microsoft.AspNetCore.Components.ComponentBase, System.IDisposable
    {
        public CascadingAuthenticationState() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment ChildContent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; } }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.RenderTree.RenderTreeBuilder builder) { }
        protected override void OnInitialized() { }
        void System.IDisposable.Dispose() { }
    }

    public partial class CascadingValue<T> : Microsoft.AspNetCore.Components.IComponent
    {
        public CascadingValue() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment ChildContent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public bool IsFixed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public T Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        public void Attach(Microsoft.AspNetCore.Components.RenderHandle renderHandle) { }
        public System.Threading.Tasks.Task SetParametersAsync(Microsoft.AspNetCore.Components.ParameterCollection parameters) { throw null; }
    }

    public partial class PageDisplay : Microsoft.AspNetCore.Components.IComponent
    {
        public PageDisplay() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment AuthorizingContent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment<Microsoft.AspNetCore.Components.AuthenticationState> NotAuthorizedContent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public System.Type Page { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public System.Collections.Generic.IDictionary<string, object> PageParameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        public void Attach(Microsoft.AspNetCore.Components.RenderHandle renderHandle) { }
        public System.Threading.Tasks.Task SetParametersAsync(Microsoft.AspNetCore.Components.ParameterCollection parameters) { throw null; }
    }
}

namespace Microsoft.AspNetCore.Components.Routing
{
    public partial class NavLink : Microsoft.AspNetCore.Components.IComponent, System.IDisposable
    {
        public NavLink() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public string ActiveClass { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        [Microsoft.AspNetCore.Components.ParameterAttribute(CaptureUnmatchedValues = true)]
        public System.Collections.Generic.IReadOnlyDictionary<string, object> AdditionalAttributes { get; private set; }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public RenderFragment ChildContent { get; set; }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.Routing.NavLinkMatch Match { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        public void Attach(Microsoft.AspNetCore.Components.RenderHandle renderHandle) { }
        public void Dispose() { }
        public System.Threading.Tasks.Task SetParametersAsync(Microsoft.AspNetCore.Components.ParameterCollection parameters) { throw null; }
    }

    public partial class Router : Microsoft.AspNetCore.Components.IComponent, System.IDisposable
    {
        public Router() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public System.Reflection.Assembly AppAssembly { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment NotFoundContent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment<AuthenticationState> NotAuthorizedContent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment AuthorizingContent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        public void Attach(Microsoft.AspNetCore.Components.RenderHandle renderHandle) { }
        public void Dispose() { }
        protected virtual void Render(Microsoft.AspNetCore.Components.RenderTree.RenderTreeBuilder builder, System.Type handler, System.Collections.Generic.IDictionary<string, object> parameters) { }
        public System.Threading.Tasks.Task SetParametersAsync(Microsoft.AspNetCore.Components.ParameterCollection parameters) { throw null; }
    }
}
