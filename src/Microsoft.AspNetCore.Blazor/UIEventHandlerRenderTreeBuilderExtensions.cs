// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Blazor.RenderTree;

namespace Microsoft.AspNetCore.Blazor
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
    public static class UIEventHandlerRenderTreeBuilderExtensions
    {
        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="UIChangeEventArgs"/>-valued attribute.
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
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, UIChangeEventHandler value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="UIKeyboardEventHandler"/>-valued attribute.
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
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, UIKeyboardEventHandler value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="UIMouseEventHandler"/>-valued attribute.
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
        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, UIMouseEventHandler value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAttribute(sequence, name, (MulticastDelegate)value);
        }
    }
}
