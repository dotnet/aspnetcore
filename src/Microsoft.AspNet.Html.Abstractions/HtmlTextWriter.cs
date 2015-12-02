// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNet.Html
{
    /// <summary>
    /// A <see cref="TextWriter"/> which supports special processing of <see cref="IHtmlContent"/>.
    /// </summary>
    public abstract class HtmlTextWriter : TextWriter
    {
        /// <summary>
        /// Writes an <see cref="IHtmlContent"/> value.
        /// </summary>
        /// <param name="value">The <see cref="IHtmlContent"/> value.</param>
        public abstract void Write(IHtmlContent value);

        /// <inheritdoc />
        public override void Write(object value)
        {
            var htmlContent = value as IHtmlContent;
            if (htmlContent == null)
            {
                base.Write(value);
            }
            else
            {
                Write(htmlContent);
            }
        }

        /// <inheritdoc />
        public override void WriteLine(object value)
        {
            var htmlContent = value as IHtmlContent;
            if (htmlContent == null)
            {
                base.Write(value);
            }
            else
            {
                Write(htmlContent);
            }

            base.WriteLine();
        }
    }
}
