# Microsoft.AspNetCore.Components.RenderTree

``` diff
+namespace Microsoft.AspNetCore.Components.RenderTree {
+    public readonly struct ArrayRange<T> {
+        public readonly int Count;
+        public readonly T[] Array;
+        public ArrayRange(T[] array, int count);
+        public ArrayRange<T> Clone();
+    }
+    public class RenderTreeBuilder {
+        public const string ChildContent = "ChildContent";
+        public RenderTreeBuilder(Renderer renderer);
+        public void AddAttribute(int sequence, in RenderTreeFrame frame);
+        public void AddAttribute(int sequence, string name, EventCallback value);
+        public void AddAttribute(int sequence, string name, Action value);
+        public void AddAttribute(int sequence, string name, Action<UIEventArgs> value);
+        public void AddAttribute(int sequence, string name, bool value);
+        public void AddAttribute(int sequence, string name, Func<UIEventArgs, Task> value);
+        public void AddAttribute(int sequence, string name, Func<Task> value);
+        public void AddAttribute(int sequence, string name, MulticastDelegate value);
+        public void AddAttribute(int sequence, string name, object value);
+        public void AddAttribute(int sequence, string name, string value);
+        public void AddAttribute<T>(int sequence, string name, EventCallback<T> value);
+        public void AddComponentReferenceCapture(int sequence, Action<object> componentReferenceCaptureAction);
+        public void AddContent(int sequence, MarkupString markupContent);
+        public void AddContent(int sequence, RenderFragment fragment);
+        public void AddContent(int sequence, object textContent);
+        public void AddContent(int sequence, string textContent);
+        public void AddContent<T>(int sequence, RenderFragment<T> fragment, T value);
+        public void AddElementReferenceCapture(int sequence, Action<ElementRef> elementReferenceCaptureAction);
+        public void AddMarkupContent(int sequence, string markupContent);
+        public void AddMultipleAttributes(int sequence, IEnumerable<KeyValuePair<string, object>> attributes);
+        public void Clear();
+        public void CloseComponent();
+        public void CloseElement();
+        public ArrayRange<RenderTreeFrame> GetFrames();
+        public void OpenComponent(int sequence, Type componentType);
+        public void OpenComponent<TComponent>(int sequence) where TComponent : IComponent;
+        public void OpenElement(int sequence, string elementName);
+        public void SetKey(object value);
+        public void SetUpdatesAttributeName(string updatesAttributeName);
+    }
+    public readonly struct RenderTreeDiff {
+        public readonly ArraySegment<RenderTreeEdit> Edits;
+        public readonly int ComponentId;
+    }
+    public readonly struct RenderTreeEdit {
+        [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
+        public readonly RenderTreeEditType Type;
+        [System.Runtime.InteropServices.FieldOffsetAttribute(8)]
+        public readonly int MoveToSiblingIndex;
+        [System.Runtime.InteropServices.FieldOffsetAttribute(8)]
+        public readonly int ReferenceFrameIndex;
+        [System.Runtime.InteropServices.FieldOffsetAttribute(4)]
+        public readonly int SiblingIndex;
+        [System.Runtime.InteropServices.FieldOffsetAttribute(16)]
+        public readonly string RemovedAttributeName;
+    }
+    public enum RenderTreeEditType {
+        PermutationListEnd = 10,
+        PermutationListEntry = 9,
+        PrependFrame = 1,
+        RemoveAttribute = 4,
+        RemoveFrame = 2,
+        SetAttribute = 3,
+        StepIn = 6,
+        StepOut = 7,
+        UpdateMarkup = 8,
+        UpdateText = 5,
+    }
+    public readonly struct RenderTreeFrame {
+        [System.Runtime.InteropServices.FieldOffsetAttribute(4)]
+        public readonly RenderTreeFrameType FrameType;
+        [System.Runtime.InteropServices.FieldOffsetAttribute(24)]
+        public readonly Action<ElementRef> ElementReferenceCaptureAction;
+        [System.Runtime.InteropServices.FieldOffsetAttribute(16)]
+        public readonly Action<object> ComponentReferenceCaptureAction;
+        [System.Runtime.InteropServices.FieldOffsetAttribute(8)]
+        public readonly int AttributeEventHandlerId;
+        [System.Runtime.InteropServices.FieldOffsetAttribute(12)]
+        public readonly int ComponentId;
+        [System.Runtime.InteropServices.FieldOffsetAttribute(8)]
+        public readonly int ComponentReferenceCaptureParentFrameIndex;
+        [System.Runtime.InteropServices.FieldOffsetAttribute(8)]
+        public readonly int ComponentSubtreeLength;
+        [System.Runtime.InteropServices.FieldOffsetAttribute(8)]
+        public readonly int ElementSubtreeLength;
+        [System.Runtime.InteropServices.FieldOffsetAttribute(8)]
+        public readonly int RegionSubtreeLength;
+        [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
+        public readonly int Sequence;
+        [System.Runtime.InteropServices.FieldOffsetAttribute(24)]
+        public readonly object AttributeValue;
+        [System.Runtime.InteropServices.FieldOffsetAttribute(16)]
+        public readonly string AttributeName;
+        [System.Runtime.InteropServices.FieldOffsetAttribute(16)]
+        public readonly string ElementName;
+        [System.Runtime.InteropServices.FieldOffsetAttribute(16)]
+        public readonly string ElementReferenceCaptureId;
+        [System.Runtime.InteropServices.FieldOffsetAttribute(16)]
+        public readonly string MarkupContent;
+        [System.Runtime.InteropServices.FieldOffsetAttribute(16)]
+        public readonly string TextContent;
+        [System.Runtime.InteropServices.FieldOffsetAttribute(16)]
+        public readonly Type ComponentType;
+        public IComponent Component { get; }
+        public override string ToString();
+    }
+    public enum RenderTreeFrameType {
+        Attribute = 3,
+        Component = 4,
+        ComponentReferenceCapture = 7,
+        Element = 1,
+        ElementReferenceCapture = 6,
+        Markup = 8,
+        None = 0,
+        Region = 5,
+        Text = 2,
+    }
+}
```

