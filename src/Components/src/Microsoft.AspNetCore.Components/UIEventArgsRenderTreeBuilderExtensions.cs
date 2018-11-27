// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Extensions methods on <see cref="RenderTreeBuilder"/> for event handlers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These methods enable method-group to delegate conversion for delegates and methods that accept
    /// types derived from <see cref="UIEventArgs"/>.
    /// </para>
    /// <para>
    /// This enhances the programming experience for using event handlers with the render tree builder
    /// in components written in pure C#. These extension methods make it possible to write code like:
    /// <code>
    /// builder.AddAttribute(0, "onkeypress", MyKeyPressHandler);
    /// </code>
    /// Where <c>void MyKeyPressHandler(UIKeyboardEventArgs e)</c> is a method defined in the same class.
    /// In this example, the author knows that the <c>onclick</c> event is associated with the
    /// <see cref="UIKeyboardEventArgs"/> event args type. The component author is responsible for 
    /// providing a delegate that matches the expected event args type, an error will result in a failure
    /// at runtime.
    /// </para>
    /// <para>
    /// When a component is authored in Razor (.cshtml), the Razor code generator will maintain a mapping
    /// between event names and event arg types that can be used to generate more strongly typed code.
    /// Generated code for the same case will look like:
    /// <code>
    /// builder.AddAttribute(0, "onkeypress", BindMethods.GetEventHandlerValue&lt;UIKeyboardEventArgs&gt;(MyKeyPressHandler));
    /// </code>
    /// </para>
    /// </remarks>
    public static class UIEventArgsRenderTreeBuilderExtensions
    {
        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Action{UIChangeEventArgs}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Action<UIChangeEventArgs> value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Func{UIChangeEventArgs, Task}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Func<UIChangeEventArgs, Task> value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Action{UIDragEventArgs}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Action<UIDragEventArgs> value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Func{UIDragEventArgs, Task}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Func<UIDragEventArgs, Task> value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Action{UIClipboardEventArgs}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Action<UIClipboardEventArgs> value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Func{UIClipboardEventArgs, Task}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Func<UIClipboardEventArgs, Task> value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Action{UIErrorEventArgs}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Action<UIErrorEventArgs> value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Func{UIErrorEventArgs, Task}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Func<UIErrorEventArgs, Task> value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Action{UIFocusEventArgs}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Action<UIFocusEventArgs> value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Func{UIFocusEventArgs, Task}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Func<UIFocusEventArgs, Task> value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Action{UIKeyboardEventArgs}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Action<UIKeyboardEventArgs> value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Func{UIKeyboardEventArgs, Task}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Func<UIKeyboardEventArgs, Task> value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Action{UIMouseEventArgs}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Action<UIMouseEventArgs> value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Func{UIMouseEventArgs, Task}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Func<UIMouseEventArgs, Task> value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Action{UIPointerEventArgs}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Action<UIPointerEventArgs> value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Func{UIPointerEventArgs, Task}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Func<UIPointerEventArgs, Task> value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Action{UIProgressEventArgs}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Action<UIProgressEventArgs> value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Func{UIProgressEventArgs, Task}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Func<UIProgressEventArgs, Task> value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Action{UITouchEventArgs}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Action<UITouchEventArgs> value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Func{UITouchEventArgs, Task}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Func<UITouchEventArgs, Task> value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Action{UIWheelEventArgs}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Action<UIWheelEventArgs> value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Func{UIWheelEventArgs, Task}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Func<UIWheelEventArgs, Task> value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }
    }
}
