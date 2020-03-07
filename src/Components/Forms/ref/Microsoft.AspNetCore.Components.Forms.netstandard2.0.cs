// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Forms
{
    public partial class DataAnnotationsValidator : Microsoft.AspNetCore.Components.ComponentBase
    {
        public DataAnnotationsValidator() { }
        protected override void OnInitialized() { }
    }
    public sealed partial class EditContext
    {
        public EditContext(object model) { }
        public object Model { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public event System.EventHandler<Microsoft.AspNetCore.Components.Forms.FieldChangedEventArgs> OnFieldChanged { add { } remove { } }
        public event System.EventHandler<Microsoft.AspNetCore.Components.Forms.ValidationRequestedEventArgs> OnValidationRequested { add { } remove { } }
        public event System.EventHandler<Microsoft.AspNetCore.Components.Forms.ValidationStateChangedEventArgs> OnValidationStateChanged { add { } remove { } }
        public Microsoft.AspNetCore.Components.Forms.FieldIdentifier Field(string fieldName) { throw null; }
        public System.Collections.Generic.IEnumerable<string> GetValidationMessages() { throw null; }
        public System.Collections.Generic.IEnumerable<string> GetValidationMessages(Microsoft.AspNetCore.Components.Forms.FieldIdentifier fieldIdentifier) { throw null; }
        public System.Collections.Generic.IEnumerable<string> GetValidationMessages(System.Linq.Expressions.Expression<System.Func<object>> accessor) { throw null; }
        public bool IsModified() { throw null; }
        public bool IsModified(in Microsoft.AspNetCore.Components.Forms.FieldIdentifier fieldIdentifier) { throw null; }
        public bool IsModified(System.Linq.Expressions.Expression<System.Func<object>> accessor) { throw null; }
        public void MarkAsUnmodified() { }
        public void MarkAsUnmodified(in Microsoft.AspNetCore.Components.Forms.FieldIdentifier fieldIdentifier) { }
        public void NotifyFieldChanged(in Microsoft.AspNetCore.Components.Forms.FieldIdentifier fieldIdentifier) { }
        public void NotifyValidationStateChanged() { }
        public bool Validate() { throw null; }
    }
    public static partial class EditContextDataAnnotationsExtensions
    {
        public static Microsoft.AspNetCore.Components.Forms.EditContext AddDataAnnotationsValidation(this Microsoft.AspNetCore.Components.Forms.EditContext editContext) { throw null; }
    }
    public sealed partial class FieldChangedEventArgs : System.EventArgs
    {
        public FieldChangedEventArgs(in Microsoft.AspNetCore.Components.Forms.FieldIdentifier fieldIdentifier) { }
        public Microsoft.AspNetCore.Components.Forms.FieldIdentifier FieldIdentifier { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct FieldIdentifier : System.IEquatable<Microsoft.AspNetCore.Components.Forms.FieldIdentifier>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public FieldIdentifier(object model, string fieldName) { throw null; }
        public string FieldName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public object Model { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public static Microsoft.AspNetCore.Components.Forms.FieldIdentifier Create<TField>(System.Linq.Expressions.Expression<System.Func<TField>> accessor) { throw null; }
        public bool Equals(Microsoft.AspNetCore.Components.Forms.FieldIdentifier otherIdentifier) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public sealed partial class ValidationMessageStore
    {
        public ValidationMessageStore(Microsoft.AspNetCore.Components.Forms.EditContext editContext) { }
        public System.Collections.Generic.IEnumerable<string> this[Microsoft.AspNetCore.Components.Forms.FieldIdentifier fieldIdentifier] { get { throw null; } }
        public System.Collections.Generic.IEnumerable<string> this[System.Linq.Expressions.Expression<System.Func<object>> accessor] { get { throw null; } }
        public void Add(in Microsoft.AspNetCore.Components.Forms.FieldIdentifier fieldIdentifier, System.Collections.Generic.IEnumerable<string> messages) { }
        public void Add(in Microsoft.AspNetCore.Components.Forms.FieldIdentifier fieldIdentifier, string message) { }
        public void Add(System.Linq.Expressions.Expression<System.Func<object>> accessor, System.Collections.Generic.IEnumerable<string> messages) { }
        public void Add(System.Linq.Expressions.Expression<System.Func<object>> accessor, string message) { }
        public void Clear() { }
        public void Clear(in Microsoft.AspNetCore.Components.Forms.FieldIdentifier fieldIdentifier) { }
        public void Clear(System.Linq.Expressions.Expression<System.Func<object>> accessor) { }
    }
    public sealed partial class ValidationRequestedEventArgs : System.EventArgs
    {
        public static readonly new Microsoft.AspNetCore.Components.Forms.ValidationRequestedEventArgs Empty;
        public ValidationRequestedEventArgs() { }
    }
    public sealed partial class ValidationStateChangedEventArgs : System.EventArgs
    {
        public static readonly new Microsoft.AspNetCore.Components.Forms.ValidationStateChangedEventArgs Empty;
        public ValidationStateChangedEventArgs() { }
    }
}
