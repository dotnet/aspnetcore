// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components
{
    public readonly partial struct ElementReference
    {
        internal static Microsoft.AspNetCore.Components.ElementReference CreateWithUniqueId() { throw null; }
    }
    public abstract partial class NavigationManager
    {
        internal static string NormalizeBaseUri(string baseUri) { throw null; }
    }
    public readonly partial struct RenderHandle
    {
        internal RenderHandle(Microsoft.AspNetCore.Components.RenderTree.Renderer renderer, int componentId) { throw null; }
    }
    internal partial class ComponentFactory
    {
        public static readonly Microsoft.AspNetCore.Components.ComponentFactory Instance;
        public ComponentFactory() { }
        public Microsoft.AspNetCore.Components.IComponent InstantiateComponent(System.IServiceProvider serviceProvider, System.Type componentType) { throw null; }
    }
    public readonly partial struct ParameterView
    {
        internal ParameterView(in Microsoft.AspNetCore.Components.Rendering.ParameterViewLifetime lifetime, Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrame[] frames, int ownerIndex) { throw null; }
        internal Microsoft.AspNetCore.Components.Rendering.ParameterViewLifetime Lifetime { get { throw null; } }
        internal void CaptureSnapshot(Microsoft.AspNetCore.Components.RenderTree.ArrayBuilder<Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrame> builder) { }
        internal bool DefinitelyEquals(Microsoft.AspNetCore.Components.ParameterView oldParameters) { throw null; }
        internal Microsoft.AspNetCore.Components.ParameterView WithCascadingParameters(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Components.CascadingParameterState> cascadingParameters) { throw null; }
    }
    internal static partial class RouteTableFactory
    {
        public static readonly System.Collections.Generic.IComparer<Microsoft.AspNetCore.Components.Routing.RouteEntry> RoutePrecedence;
        internal static Microsoft.AspNetCore.Components.Routing.RouteTable Create(System.Collections.Generic.Dictionary<System.Type, string[]> templatesByHandler) { throw null; }
        public static Microsoft.AspNetCore.Components.Routing.RouteTable Create(System.Collections.Generic.IEnumerable<System.Reflection.Assembly> assemblies) { throw null; }
        internal static Microsoft.AspNetCore.Components.Routing.RouteTable Create(System.Collections.Generic.IEnumerable<System.Type> componentTypes) { throw null; }
        internal static int RouteComparison(Microsoft.AspNetCore.Components.Routing.RouteEntry x, Microsoft.AspNetCore.Components.Routing.RouteEntry y) { throw null; }
    }
    internal partial interface IEventCallback
    {
        bool HasDelegate { get; }
        object UnpackForRenderTree();
    }
    internal partial interface ICascadingValueComponent
    {
        object CurrentValue { get; }
        bool CurrentValueIsFixed { get; }
        bool CanSupplyValue(System.Type valueType, string valueName);
        void Subscribe(Microsoft.AspNetCore.Components.Rendering.ComponentState subscriber);
        void Unsubscribe(Microsoft.AspNetCore.Components.Rendering.ComponentState subscriber);
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct CascadingParameterState
    {
        private readonly object _dummy;
        public CascadingParameterState(string localValueName, Microsoft.AspNetCore.Components.ICascadingValueComponent valueSupplier) { throw null; }
        public string LocalValueName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Components.ICascadingValueComponent ValueSupplier { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Components.CascadingParameterState> FindCascadingParameters(Microsoft.AspNetCore.Components.Rendering.ComponentState componentState) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Components.RenderTree
{
    public readonly partial struct RenderTreeEdit
    {
        internal static Microsoft.AspNetCore.Components.RenderTree.RenderTreeEdit PermutationListEnd() { throw null; }
        internal static Microsoft.AspNetCore.Components.RenderTree.RenderTreeEdit PermutationListEntry(int fromSiblingIndex, int toSiblingIndex) { throw null; }
        internal static Microsoft.AspNetCore.Components.RenderTree.RenderTreeEdit PrependFrame(int siblingIndex, int referenceFrameIndex) { throw null; }
        internal static Microsoft.AspNetCore.Components.RenderTree.RenderTreeEdit RemoveAttribute(int siblingIndex, string name) { throw null; }
        internal static Microsoft.AspNetCore.Components.RenderTree.RenderTreeEdit RemoveFrame(int siblingIndex) { throw null; }
        internal static Microsoft.AspNetCore.Components.RenderTree.RenderTreeEdit SetAttribute(int siblingIndex, int referenceFrameIndex) { throw null; }
        internal static Microsoft.AspNetCore.Components.RenderTree.RenderTreeEdit StepIn(int siblingIndex) { throw null; }
        internal static Microsoft.AspNetCore.Components.RenderTree.RenderTreeEdit StepOut() { throw null; }
        internal static Microsoft.AspNetCore.Components.RenderTree.RenderTreeEdit UpdateMarkup(int siblingIndex, int referenceFrameIndex) { throw null; }
        internal static Microsoft.AspNetCore.Components.RenderTree.RenderTreeEdit UpdateText(int siblingIndex, int referenceFrameIndex) { throw null; }
    }
    public readonly partial struct RenderBatch
    {
        internal RenderBatch(Microsoft.AspNetCore.Components.RenderTree.ArrayRange<Microsoft.AspNetCore.Components.RenderTree.RenderTreeDiff> updatedComponents, Microsoft.AspNetCore.Components.RenderTree.ArrayRange<Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrame> referenceFrames, Microsoft.AspNetCore.Components.RenderTree.ArrayRange<int> disposedComponentIDs, Microsoft.AspNetCore.Components.RenderTree.ArrayRange<ulong> disposedEventHandlerIDs) { throw null; }
    }
    internal static partial class ArrayBuilderExtensions
    {
        public static Microsoft.AspNetCore.Components.RenderTree.ArrayRange<T> ToRange<T>(this Microsoft.AspNetCore.Components.RenderTree.ArrayBuilder<T> builder) { throw null; }
        public static Microsoft.AspNetCore.Components.RenderTree.ArrayBuilderSegment<T> ToSegment<T>(this Microsoft.AspNetCore.Components.RenderTree.ArrayBuilder<T> builder, int fromIndexInclusive, int toIndexExclusive) { throw null; }
    }
    internal static partial class RenderTreeDiffBuilder
    {
        public const int SystemAddedAttributeSequenceNumber = -2147483648;
        public static Microsoft.AspNetCore.Components.RenderTree.RenderTreeDiff ComputeDiff(Microsoft.AspNetCore.Components.RenderTree.Renderer renderer, Microsoft.AspNetCore.Components.Rendering.RenderBatchBuilder batchBuilder, int componentId, Microsoft.AspNetCore.Components.RenderTree.ArrayRange<Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrame> oldTree, Microsoft.AspNetCore.Components.RenderTree.ArrayRange<Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrame> newTree) { throw null; }
        public static void DisposeFrames(Microsoft.AspNetCore.Components.Rendering.RenderBatchBuilder batchBuilder, Microsoft.AspNetCore.Components.RenderTree.ArrayRange<Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrame> frames) { }
    }
    internal partial class ArrayBuilder<T> : System.IDisposable
    {
        public ArrayBuilder(int minCapacity = 32, System.Buffers.ArrayPool<T> arrayPool = null) { }
        public T[] Buffer { get { throw null; } }
        public int Count { get { throw null; } }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public int Append(in T item) { throw null; }
        internal int Append(T[] source, int startIndex, int length) { throw null; }
        public void Clear() { }
        public void Dispose() { }
        public void InsertExpensive(int index, T value) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public void Overwrite(int index, in T value) { }
        public void RemoveLast() { }
    }
    internal partial class StackObjectPool<T> where T : class
    {
        public StackObjectPool(int maxPreservedItems, System.Func<T> instanceFactory) { }
        public T Get() { throw null; }
        public void Return(T instance) { }
    }
    public readonly partial struct RenderTreeDiff
    {
        internal RenderTreeDiff(int componentId, Microsoft.AspNetCore.Components.RenderTree.ArrayBuilderSegment<Microsoft.AspNetCore.Components.RenderTree.RenderTreeEdit> entries) { throw null; }
    }

    // https://github.com/dotnet/arcade/pull/2033
    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    public readonly partial struct RenderTreeFrame
    {
        [FieldOffset(0)] public readonly int Sequence;

        [FieldOffset(4)] public readonly RenderTreeFrameType FrameType;

        [FieldOffset(8)] public readonly int ElementSubtreeLength;

        [FieldOffset(16)] public readonly string ElementName;

        [FieldOffset(24)] public readonly object ElementKey;

        [FieldOffset(16)] public readonly string TextContent;

        [FieldOffset(8)] public readonly ulong AttributeEventHandlerId;

        [FieldOffset(16)] public readonly string AttributeName;

        [FieldOffset(24)] public readonly object AttributeValue;

        [FieldOffset(32)] public readonly string AttributeEventUpdatesAttributeName;

        [FieldOffset(8)] public readonly int ComponentSubtreeLength;

        [FieldOffset(12)] public readonly int ComponentId;

        [FieldOffset(16)] public readonly Type ComponentType;

        [FieldOffset(32)] public readonly object ComponentKey;

        public IComponent Component => null;

        [FieldOffset(8)] public readonly int RegionSubtreeLength;

        [FieldOffset(16)] public readonly string ElementReferenceCaptureId;

        [FieldOffset(24)] public readonly Action<ElementReference> ElementReferenceCaptureAction;

        [FieldOffset(8)] public readonly int ComponentReferenceCaptureParentFrameIndex;

        [FieldOffset(16)] public readonly Action<object> ComponentReferenceCaptureAction;

        [FieldOffset(16)] public readonly string MarkupContent;

        public override string ToString() => null;

        internal static RenderTreeFrame Element(int sequence, string elementName) { throw null; }
        internal static RenderTreeFrame Text(int sequence, string textContent) { throw null; }
        internal static RenderTreeFrame Markup(int sequence, string markupContent) { throw null; }
        internal static RenderTreeFrame Attribute(int sequence, string name, object value) { throw null; }
        internal static RenderTreeFrame ChildComponent(int sequence, Type componentType) { throw null; }
        internal static RenderTreeFrame PlaceholderChildComponentWithSubtreeLength(int subtreeLength) { throw null; }
        internal static RenderTreeFrame Region(int sequence) { throw null; }
        internal static RenderTreeFrame ElementReferenceCapture(int sequence, Action<ElementReference> elementReferenceCaptureAction) { throw null; }
        internal static RenderTreeFrame ComponentReferenceCapture(int sequence, Action<object> componentReferenceCaptureAction, int parentFrameIndex) { throw null; }
        internal RenderTreeFrame WithElementSubtreeLength(int elementSubtreeLength) { throw null; }
        internal RenderTreeFrame WithComponentSubtreeLength(int componentSubtreeLength) { throw null; }
        internal RenderTreeFrame WithAttributeSequence(int sequence) { throw null; }
        internal RenderTreeFrame WithComponent(ComponentState componentState) { throw null; }
        internal RenderTreeFrame WithAttributeEventHandlerId(ulong eventHandlerId) { throw null; }
        internal RenderTreeFrame WithAttributeValue(object attributeValue) { throw null; }
        internal RenderTreeFrame WithAttributeEventUpdatesAttributeName(string attributeUpdatesAttributeName) { throw null; }
        internal RenderTreeFrame WithRegionSubtreeLength(int regionSubtreeLength) { throw null; }
        internal RenderTreeFrame WithElementReferenceCaptureId(string elementReferenceCaptureId) { throw null; }
        internal RenderTreeFrame WithElementKey(object elementKey) { throw null; }
        internal RenderTreeFrame WithComponentKey(object componentKey) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Components.Rendering
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct ParameterViewLifetime
    {
        public static readonly ParameterViewLifetime Unbound = default;
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public ParameterViewLifetime(Microsoft.AspNetCore.Components.Rendering.RenderBatchBuilder owner) { throw null; }
        public void AssertNotExpired() { }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{_state,nq}")]
    internal partial class RendererSynchronizationContext : System.Threading.SynchronizationContext
    {
        public RendererSynchronizationContext() { }
        public event System.UnhandledExceptionEventHandler UnhandledException { add { } remove { } }
        public override System.Threading.SynchronizationContext CreateCopy() { throw null; }
        public System.Threading.Tasks.Task InvokeAsync(System.Action action) { throw null; }
        public System.Threading.Tasks.Task InvokeAsync(System.Func<System.Threading.Tasks.Task> asyncAction) { throw null; }
        public System.Threading.Tasks.Task<TResult> InvokeAsync<TResult>(System.Func<System.Threading.Tasks.Task<TResult>> asyncFunction) { throw null; }
        public System.Threading.Tasks.Task<TResult> InvokeAsync<TResult>(System.Func<TResult> function) { throw null; }
        public override void Post(System.Threading.SendOrPostCallback d, object state) { }
        public override void Send(System.Threading.SendOrPostCallback d, object state) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct RenderQueueEntry
    {
        public readonly ComponentState ComponentState;
        public readonly RenderFragment RenderFragment;
        public RenderQueueEntry(Microsoft.AspNetCore.Components.Rendering.ComponentState componentState, Microsoft.AspNetCore.Components.RenderFragment renderFragment) { throw null; }
    }
    internal partial class RenderBatchBuilder : System.IDisposable
    {
        public RenderBatchBuilder() { }
        public System.Collections.Generic.Dictionary<string, int> AttributeDiffSet { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Collections.Generic.Queue<int> ComponentDisposalQueue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Collections.Generic.Queue<Microsoft.AspNetCore.Components.Rendering.RenderQueueEntry> ComponentRenderQueue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Components.RenderTree.ArrayBuilder<int> DisposedComponentIds { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Components.RenderTree.ArrayBuilder<ulong> DisposedEventHandlerIds { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Components.RenderTree.ArrayBuilder<Microsoft.AspNetCore.Components.RenderTree.RenderTreeEdit> EditsBuffer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal Microsoft.AspNetCore.Components.RenderTree.StackObjectPool<System.Collections.Generic.Dictionary<object, Microsoft.AspNetCore.Components.Rendering.KeyedItemInfo>> KeyedItemInfoDictionaryPool { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public int ParameterViewValidityStamp { get { throw null; } }
        public Microsoft.AspNetCore.Components.RenderTree.ArrayBuilder<Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrame> ReferenceFramesBuffer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Components.RenderTree.ArrayBuilder<Microsoft.AspNetCore.Components.RenderTree.RenderTreeDiff> UpdatedComponentDiffs { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void ClearStateForCurrentBatch() { }
        public void Dispose() { }
        public void InvalidateParameterViews() { }
        public Microsoft.AspNetCore.Components.RenderTree.RenderBatch ToBatch() { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct KeyedItemInfo
    {
        public readonly int OldIndex;
        public readonly int NewIndex;
        public readonly int OldSiblingIndex;
        public readonly int NewSiblingIndex;
        public KeyedItemInfo(int oldIndex, int newIndex) { throw null; }
        public Microsoft.AspNetCore.Components.Rendering.KeyedItemInfo WithNewSiblingIndex(int newSiblingIndex) { throw null; }
        public Microsoft.AspNetCore.Components.Rendering.KeyedItemInfo WithOldSiblingIndex(int oldSiblingIndex) { throw null; }
    }
    internal partial class ComponentState : System.IDisposable
    {
        public ComponentState(Microsoft.AspNetCore.Components.RenderTree.Renderer renderer, int componentId, Microsoft.AspNetCore.Components.IComponent component, Microsoft.AspNetCore.Components.Rendering.ComponentState parentComponentState) { }
        public Microsoft.AspNetCore.Components.IComponent Component { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public int ComponentId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder CurrentRenderTree { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Components.Rendering.ComponentState ParentComponentState { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void Dispose() { }
        public void NotifyCascadingValueChanged() { }
        public System.Threading.Tasks.Task NotifyRenderCompletedAsync() { throw null; }
        public void RenderIntoBatch(Microsoft.AspNetCore.Components.Rendering.RenderBatchBuilder batchBuilder, Microsoft.AspNetCore.Components.RenderFragment renderFragment) { }
        public void SetDirectParameters(Microsoft.AspNetCore.Components.ParameterView parameters) { }
        public bool TryDisposeInBatch(Microsoft.AspNetCore.Components.Rendering.RenderBatchBuilder batchBuilder, out System.Exception exception) { throw null; }
    }
    internal partial class RenderTreeUpdater
    {
        public RenderTreeUpdater() { }
        public static void UpdateToMatchClientState(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder renderTreeBuilder, ulong eventHandlerId, object newFieldValue) { }
    }
}
namespace Microsoft.AspNetCore.Components.Routing
{
    internal partial class TemplateParser
    {
        public static readonly char[] InvalidParameterNameCharacters;
        public TemplateParser() { }
        internal static Microsoft.AspNetCore.Components.Routing.RouteTemplate ParseTemplate(string template) { throw null; }
    }
    internal partial class RouteContext
    {
        public RouteContext(string path) { }
        public System.Type Handler { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IReadOnlyDictionary<string, object> Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string[] Segments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("Handler = {Handler}, Template = {Template}")]
    internal partial class RouteEntry
    {
        public RouteEntry(Microsoft.AspNetCore.Components.Routing.RouteTemplate template, System.Type handler, string[] unusedRouteParameterNames) { }
        public System.Type Handler { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Components.Routing.RouteTemplate Template { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string[] UnusedRouteParameterNames { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal void Match(Microsoft.AspNetCore.Components.Routing.RouteContext context) { }
    }
    internal partial class RouteTable
    {
        public RouteTable(Microsoft.AspNetCore.Components.Routing.RouteEntry[] routes) { }
        public Microsoft.AspNetCore.Components.Routing.RouteEntry[] Routes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal void Route(Microsoft.AspNetCore.Components.Routing.RouteContext routeContext) { }
    }
    internal abstract partial class RouteConstraint
    {
        protected RouteConstraint() { }
        public abstract bool Match(string pathSegment, out object convertedValue);
        public static Microsoft.AspNetCore.Components.Routing.RouteConstraint Parse(string template, string segment, string constraint) { throw null; }
    }
    internal partial class TemplateSegment
    {
        public TemplateSegment(string template, string segment, bool isParameter) { }
        public Microsoft.AspNetCore.Components.Routing.RouteConstraint[] Constraints { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool IsParameter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool Match(string pathSegment, out object matchedParameterValue) { throw null; }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{TemplateText}")]
    internal partial class RouteTemplate
    {
        public RouteTemplate(string templateText, Microsoft.AspNetCore.Components.Routing.TemplateSegment[] segments) { }
        public Microsoft.AspNetCore.Components.Routing.TemplateSegment[] Segments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string TemplateText { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
}
