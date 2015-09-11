// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNet.Html.Abstractions;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Abstract class used to buffer content returned by <see cref="ITagHelper"/>s.
    /// </summary>
    public abstract class TagHelperContent : IHtmlContent
    {
        /// <summary>
        /// Gets a value indicating whether the content was modifed.
        /// </summary>
        public abstract bool IsModified { get; }

        /// <summary>
        /// Gets a value indicating whether the content is empty.
        /// </summary>
        public abstract bool IsEmpty { get; }

        /// <summary>
        /// Gets a value indicating whether the content is whitespace.
        /// </summary>
        public abstract bool IsWhiteSpace { get; }

        /// <summary>
        /// Sets the content.
        /// </summary>
        /// <param name="value">The <see cref="string"/> that replaces the content.</param>
        /// <returns>A reference to this instance after the set operation has completed.</returns>
        public TagHelperContent SetContent(string value)
        {
            Clear();
            Append(value);
            return this;
        }

        /// <summary>
        /// Sets the content.
        /// </summary>
        /// <param name="htmlContent">The <see cref="IHtmlContent"/> that replaces the content.</param>
        /// <returns>A reference to this instance after the set operation has completed.</returns>
        public TagHelperContent SetContent(IHtmlContent htmlContent)
        {
            Clear();
            Append(htmlContent);
            return this;
        }

        /// <summary>
        /// Appends <paramref name="value"/> to the existing content.
        /// </summary>
        /// <param name="value">The <see cref="string"/> to be appended.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        public abstract TagHelperContent Append(string value);

        /// <summary>
        /// Appends <paramref name="value"/> to the existing content. <paramref name="value"/> is assumed
        /// to be an HTML encoded <see cref="string"/> and no further encoding will be performed.
        /// </summary>
        /// <param name="value">The <see cref="string"/> to be appended.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        public abstract TagHelperContent AppendEncoded(string value);

        /// <summary>
        /// Appends the specified <paramref name="format"/> to the existing content after
        /// replacing the format item with the <see cref="string"/> representation of the
        /// <paramref name="arg0"/>.
        /// </summary>
        /// <param name="format">
        /// The composite format <see cref="string"/> (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx).
        /// </param>
        /// <param name="arg0">The object to format.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        public abstract TagHelperContent AppendFormat(string format, object arg0);

        /// <summary>
        /// Appends the specified <paramref name="format"/> to the existing content after
        /// replacing each format item with the <see cref="string"/> representation of the
        /// <paramref name="arg0"/> and <paramref name="arg1"/>.
        /// </summary>
        /// <param name="format">
        /// The composite format <see cref="string"/> (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx).
        /// </param>
        /// <param name="arg0">The object to format.</param>
        /// <param name="arg1">The object to format.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        public abstract TagHelperContent AppendFormat(string format, object arg0, object arg1);

        /// <summary>
        /// Appends the specified <paramref name="format"/> to the existing content after
        /// replacing each format item with the <see cref="string"/> representation of the
        /// <paramref name="arg0"/>, <paramref name="arg1"/> and <paramref name="arg2"/>.
        /// </summary>
        /// <param name="format">
        /// The composite format <see cref="string"/> (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx).
        /// </param>
        /// <param name="arg0">The object to format.</param>
        /// <param name="arg1">The object to format.</param>
        /// <param name="arg2">The object to format.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        public abstract TagHelperContent AppendFormat(string format, object arg0, object arg1, object arg2);

        /// <summary>
        /// Appends the specified <paramref name="format"/> to the existing content after
        /// replacing each format item with the <see cref="string"/> representation of the
        /// corresponding item in the <paramref name="args"/> array.
        /// </summary>
        /// <param name="format">
        /// The composite format <see cref="string"/> (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx).
        /// </param>
        /// <param name="args">The object array to format.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        public abstract TagHelperContent AppendFormat(string format, params object[] args);

        /// <summary>
        /// Appends the specified <paramref name="format"/> to the existing content with information from the
        /// <paramref name="provider"/> after replacing the format item with the <see cref="string"/>
        /// representation of the corresponding item in <paramref name="arg0"/>.
        /// </summary>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="format">
        /// The composite format <see cref="string"/> (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx).
        /// </param>
        /// <param name="arg0">The object to format.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        public abstract TagHelperContent AppendFormat(IFormatProvider provider, string format, object arg0);

        /// <summary>
        /// Appends the specified <paramref name="format"/> to the existing content with information from the
        /// <paramref name="provider"/> after replacing each format item with the <see cref="string"/>
        /// representation of the corresponding item in <paramref name="arg0"/> and <paramref name="arg1"/>.
        /// </summary>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="format">
        /// The composite format <see cref="string"/> (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx).
        /// </param>
        /// <param name="arg0">The object to format.</param>
        /// <param name="arg1">The object to format.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        public abstract TagHelperContent AppendFormat(
            IFormatProvider provider,
            string format,
            object arg0,
            object arg1);

        /// <summary>
        /// Appends the specified <paramref name="format"/> to the existing content with information from the
        /// <paramref name="provider"/> after replacing each format item with the <see cref="string"/>
        /// representation of the corresponding item in <paramref name="arg0"/>, <paramref name="arg1"/>
        /// and <paramref name="arg2"/>.
        /// </summary>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="format">
        /// The composite format <see cref="string"/> (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx).
        /// </param>
        /// <param name="arg0">The object to format.</param>
        /// <param name="arg1">The object to format.</param>
        /// <param name="arg2">The object to format.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        public abstract TagHelperContent AppendFormat(
            IFormatProvider provider,
            string format,
            object arg0,
            object arg1,
            object arg2);

        /// <summary>
        /// Appends the specified <paramref name="format"/> to the existing content with information from the
        /// <paramref name="provider"/> after replacing each format item with the <see cref="string"/>
        /// representation of the corresponding item in the <paramref name="args"/> array.
        /// </summary>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="format">
        /// The composite format <see cref="string"/> (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx).
        /// </param>
        /// <param name="args">The object array to format.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        public abstract TagHelperContent AppendFormat(IFormatProvider provider, string format, params object[] args);

        /// <summary>
        /// Appends <paramref name="htmlContent"/> to the existing content.
        /// </summary>
        /// <param name="htmlContent">The <see cref="IHtmlContent"/> to be appended.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        public abstract TagHelperContent Append(IHtmlContent htmlContent);

        /// <summary>
        /// Clears the content.
        /// </summary>
        /// <returns>A reference to this instance after the clear operation has completed.</returns>
        public abstract TagHelperContent Clear();

        /// <summary>
        /// Gets the content.
        /// </summary>
        /// <returns>A <see cref="string"/> containing the content.</returns>
        public abstract string GetContent();

        /// <inheritdoc />
        public abstract void WriteTo(TextWriter writer, IHtmlEncoder encoder);
    }
}