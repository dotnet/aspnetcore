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
        public readonly System.Action<Microsoft.AspNetCore.Components.ElementRef> ElementReferenceCaptureAction;
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
        public void Configure(Microsoft.AspNetCore.Components.RenderHandle renderHandle) { }
        public System.Threading.Tasks.Task SetParametersAsync(Microsoft.AspNetCore.Components.ParameterCollection parameters) { throw null; }
    }
}

namespace Microsoft.AspNetCore.Components.Forms
{
    public partial class DataAnnotationsValidator : Microsoft.AspNetCore.Components.ComponentBase
    {
        public DataAnnotationsValidator() { }
        protected override void OnInit() { }
    }

    public partial class EditForm : Microsoft.AspNetCore.Components.ComponentBase
    {
        public EditForm() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment<Microsoft.AspNetCore.Components.Forms.EditContext> ChildContent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.Forms.EditContext EditContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public object Model { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Forms.EditContext> OnInvalidSubmit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Forms.EditContext> OnSubmit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Forms.EditContext> OnValidSubmit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.RenderTree.RenderTreeBuilder builder) { }
        protected override void OnParametersSet() { }
    }

    public abstract partial class InputBase<T> : Microsoft.AspNetCore.Components.ComponentBase
    {
        protected InputBase() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public string Class { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        protected string CssClass { get { throw null; } }
        protected T CurrentValue { get { throw null; } set { } }
        protected string CurrentValueAsString { get { throw null; } set { } }
        protected Microsoft.AspNetCore.Components.Forms.EditContext EditContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        protected string FieldClass { get { throw null; } }
        protected Microsoft.AspNetCore.Components.Forms.FieldIdentifier FieldIdentifier { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public string Id { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public T Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.EventCallback<T> ValueChanged { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public System.Linq.Expressions.Expression<System.Func<T>> ValueExpression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        protected virtual string FormatValueAsString(T value) { throw null; }
        public override System.Threading.Tasks.Task SetParametersAsync(Microsoft.AspNetCore.Components.ParameterCollection parameters) { throw null; }
        protected abstract bool TryParseValueFromString(string value, out T result, out string validationErrorMessage);
    }

    public partial class InputCheckbox : Microsoft.AspNetCore.Components.Forms.InputBase<bool>
    {
        public InputCheckbox() { }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.RenderTree.RenderTreeBuilder builder) { }
        protected override bool TryParseValueFromString(string value, out bool result, out string validationErrorMessage) { throw null; }
    }

    public partial class InputDate<T> : Microsoft.AspNetCore.Components.Forms.InputBase<T>
    {
        public InputDate() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public string ParsingErrorMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.RenderTree.RenderTreeBuilder builder) { }
        protected override string FormatValueAsString(T value) { throw null; }
        protected override bool TryParseValueFromString(string value, out T result, out string validationErrorMessage) { throw null; }
    }

    public partial class InputNumber<T> : Microsoft.AspNetCore.Components.Forms.InputBase<T>
    {
        public InputNumber() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public string ParsingErrorMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.RenderTree.RenderTreeBuilder builder) { }
        protected override bool TryParseValueFromString(string value, out T result, out string validationErrorMessage) { throw null; }
    }

    public partial class InputSelect<T> : Microsoft.AspNetCore.Components.Forms.InputBase<T>
    {
        public InputSelect() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment ChildContent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.RenderTree.RenderTreeBuilder builder) { }
        protected override bool TryParseValueFromString(string value, out T result, out string validationErrorMessage) { throw null; }
    }

    public partial class InputText : Microsoft.AspNetCore.Components.Forms.InputBase<string>
    {
        public InputText() { }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.RenderTree.RenderTreeBuilder builder) { }
        protected override bool TryParseValueFromString(string value, out string result, out string validationErrorMessage) { throw null; }
    }

    public partial class InputTextArea : Microsoft.AspNetCore.Components.Forms.InputBase<string>
    {
        public InputTextArea() { }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.RenderTree.RenderTreeBuilder builder) { }
        protected override bool TryParseValueFromString(string value, out string result, out string validationErrorMessage) { throw null; }
    }

    public partial class ValidationMessage<T> : Microsoft.AspNetCore.Components.ComponentBase, System.IDisposable
    {
        public ValidationMessage() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public System.Linq.Expressions.Expression<System.Func<T>> For { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.RenderTree.RenderTreeBuilder builder) { }
        protected override void OnParametersSet() { }
        void System.IDisposable.Dispose() { }
    }

    public partial class ValidationSummary : Microsoft.AspNetCore.Components.ComponentBase, System.IDisposable
    {
        public ValidationSummary() { }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.RenderTree.RenderTreeBuilder builder) { }
        protected override void OnParametersSet() { }
        void System.IDisposable.Dispose() { }
    }
}

namespace Microsoft.AspNetCore.Components.Layouts
{
    public partial class LayoutDisplay : Microsoft.AspNetCore.Components.IComponent
    {
        public LayoutDisplay() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public System.Type Page { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public System.Collections.Generic.IDictionary<string, object> PageParameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        public void Configure(Microsoft.AspNetCore.Components.RenderHandle renderHandle) { }
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
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.Routing.NavLinkMatch Match { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        public void Configure(Microsoft.AspNetCore.Components.RenderHandle renderHandle) { }
        public void Dispose() { }
        public System.Threading.Tasks.Task SetParametersAsync(Microsoft.AspNetCore.Components.ParameterCollection parameters) { throw null; }
    }

    public partial class Router : Microsoft.AspNetCore.Components.IComponent, System.IDisposable
    {
        public Router() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public System.Reflection.Assembly AppAssembly { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public System.Type FallbackComponent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } private set { throw null; }}
        public void Configure(Microsoft.AspNetCore.Components.RenderHandle renderHandle) { }
        public void Dispose() { }
        protected virtual void Render(Microsoft.AspNetCore.Components.RenderTree.RenderTreeBuilder builder, System.Type handler, System.Collections.Generic.IDictionary<string, object> parameters) { }
        public System.Threading.Tasks.Task SetParametersAsync(Microsoft.AspNetCore.Components.ParameterCollection parameters) { throw null; }
    }
}