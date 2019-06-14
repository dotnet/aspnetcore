# Microsoft.AspNetCore.Components.Forms

``` diff
+namespace Microsoft.AspNetCore.Components.Forms {
+    public class DataAnnotationsValidator : ComponentBase {
+        public DataAnnotationsValidator();
+        protected override void OnInit();
+    }
+    public sealed class EditContext {
+        public EditContext(object model);
+        public object Model { get; }
+        public event EventHandler<FieldChangedEventArgs> OnFieldChanged;
+        public event EventHandler<ValidationRequestedEventArgs> OnValidationRequested;
+        public event EventHandler<ValidationStateChangedEventArgs> OnValidationStateChanged;
+        public FieldIdentifier Field(string fieldName);
+        public IEnumerable<string> GetValidationMessages();
+        public IEnumerable<string> GetValidationMessages(FieldIdentifier fieldIdentifier);
+        public bool IsModified();
+        public bool IsModified(in FieldIdentifier fieldIdentifier);
+        public void MarkAsUnmodified();
+        public void MarkAsUnmodified(in FieldIdentifier fieldIdentifier);
+        public void NotifyFieldChanged(in FieldIdentifier fieldIdentifier);
+        public void NotifyValidationStateChanged();
+        public bool Validate();
+    }
+    public static class EditContextDataAnnotationsExtensions {
+        public static EditContext AddDataAnnotationsValidation(this EditContext editContext);
+    }
+    public static class EditContextExpressionExtensions {
+        public static IEnumerable<string> GetValidationMessages(this EditContext editContext, Expression<Func<object>> accessor);
+        public static bool IsModified(this EditContext editContext, Expression<Func<object>> accessor);
+    }
+    public static class EditContextFieldClassExtensions {
+        public static string FieldClass(this EditContext editContext, in FieldIdentifier fieldIdentifier);
+        public static string FieldClass<TField>(this EditContext editContext, Expression<Func<TField>> accessor);
+    }
+    public class EditForm : ComponentBase {
+        public EditForm();
+        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; private set; }
+        public RenderFragment<EditContext> ChildContent { get; private set; }
+        public EditContext EditContext { get; private set; }
+        public object Model { get; private set; }
+        public EventCallback<EditContext> OnInvalidSubmit { get; private set; }
+        public EventCallback<EditContext> OnSubmit { get; private set; }
+        public EventCallback<EditContext> OnValidSubmit { get; private set; }
+        protected override void BuildRenderTree(RenderTreeBuilder builder);
+        protected override void OnParametersSet();
+    }
+    public sealed class FieldChangedEventArgs {
+        public FieldIdentifier FieldIdentifier { get; }
+    }
+    public readonly struct FieldIdentifier {
+        public FieldIdentifier(object model, string fieldName);
+        public string FieldName { get; }
+        public object Model { get; }
+        public static FieldIdentifier Create<T>(Expression<Func<T>> accessor);
+        public override bool Equals(object obj);
+        public override int GetHashCode();
+    }
+    public abstract class InputBase<T> : ComponentBase {
+        protected InputBase();
+        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; private set; }
+        public string Class { get; private set; }
+        protected string CssClass { get; }
+        protected T CurrentValue { get; set; }
+        protected string CurrentValueAsString { get; set; }
+        protected EditContext EditContext { get; private set; }
+        protected string FieldClass { get; }
+        protected FieldIdentifier FieldIdentifier { get; private set; }
+        public string Id { get; private set; }
+        public T Value { get; private set; }
+        public EventCallback<T> ValueChanged { get; private set; }
+        public Expression<Func<T>> ValueExpression { get; private set; }
+        protected virtual string FormatValueAsString(T value);
+        public override Task SetParametersAsync(ParameterCollection parameters);
+        protected abstract bool TryParseValueFromString(string value, out T result, out string validationErrorMessage);
+    }
+    public class InputCheckbox : InputBase<bool> {
+        public InputCheckbox();
+        protected override void BuildRenderTree(RenderTreeBuilder builder);
+        protected override bool TryParseValueFromString(string value, out bool result, out string validationErrorMessage);
+    }
+    public class InputDate<T> : InputBase<T> {
+        public InputDate();
+        public string ParsingErrorMessage { get; private set; }
+        protected override void BuildRenderTree(RenderTreeBuilder builder);
+        protected override string FormatValueAsString(T value);
+        protected override bool TryParseValueFromString(string value, out T result, out string validationErrorMessage);
+    }
+    public class InputNumber<T> : InputBase<T> {
+        public InputNumber();
+        public string ParsingErrorMessage { get; private set; }
+        protected override void BuildRenderTree(RenderTreeBuilder builder);
+        protected override bool TryParseValueFromString(string value, out T result, out string validationErrorMessage);
+    }
+    public class InputSelect<T> : InputBase<T> {
+        public InputSelect();
+        public RenderFragment ChildContent { get; private set; }
+        protected override void BuildRenderTree(RenderTreeBuilder builder);
+        protected override bool TryParseValueFromString(string value, out T result, out string validationErrorMessage);
+    }
+    public class InputText : InputBase<string> {
+        public InputText();
+        protected override void BuildRenderTree(RenderTreeBuilder builder);
+        protected override bool TryParseValueFromString(string value, out string result, out string validationErrorMessage);
+    }
+    public class InputTextArea : InputBase<string> {
+        public InputTextArea();
+        protected override void BuildRenderTree(RenderTreeBuilder builder);
+        protected override bool TryParseValueFromString(string value, out string result, out string validationErrorMessage);
+    }
+    public class ValidationMessage<T> : ComponentBase, IDisposable {
+        public ValidationMessage();
+        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; private set; }
+        public Expression<Func<T>> For { get; private set; }
+        protected override void BuildRenderTree(RenderTreeBuilder builder);
+        protected override void OnParametersSet();
+        void System.IDisposable.Dispose();
+    }
+    public sealed class ValidationMessageStore {
+        public ValidationMessageStore(EditContext editContext);
+        public IEnumerable<string> this[FieldIdentifier fieldIdentifier] { get; }
+        public IEnumerable<string> this[Expression<Func<object>> accessor] { get; }
+        public void Add(in FieldIdentifier fieldIdentifier, string message);
+        public void AddRange(in FieldIdentifier fieldIdentifier, IEnumerable<string> messages);
+        public void Clear();
+        public void Clear(in FieldIdentifier fieldIdentifier);
+    }
+    public static class ValidationMessageStoreExpressionExtensions {
+        public static void Add(this ValidationMessageStore store, Expression<Func<object>> accessor, string message);
+        public static void AddRange(this ValidationMessageStore store, Expression<Func<object>> accessor, IEnumerable<string> messages);
+        public static void Clear(this ValidationMessageStore store, Expression<Func<object>> accessor);
+    }
+    public sealed class ValidationRequestedEventArgs
+    public sealed class ValidationStateChangedEventArgs
+    public class ValidationSummary : ComponentBase, IDisposable {
+        public ValidationSummary();
+        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; private set; }
+        protected override void BuildRenderTree(RenderTreeBuilder builder);
+        protected override void OnParametersSet();
+        void System.IDisposable.Dispose();
+    }
+}
```

