// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNet.Html.Abstractions;
using Microsoft.Framework.Internal;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Represents a deferred write operation in a <see cref="RazorPage"/>.
    /// </summary>
    public class HelperResult : IHtmlContent
    {
        private readonly Action<TextWriter> _action;

        /// <summary>
        /// Creates a new instance of <see cref="HelperResult"/>.
        /// </summary>
        /// <param name="action">The delegate to invoke when
        /// <see cref="WriteTo(TextWriter, IHtmlEncoder)"/> is called.</param>
        public HelperResult([NotNull] Action<TextWriter> action)
        {
            _action = action;
        }

        /// <summary>
        /// Gets the delegate to invoke when <see cref="WriteTo(TextWriter, IHtmlEncoder)"/> is called.
        /// </summary>
        public Action<TextWriter> WriteAction
        {
            get { return _action; }
        }

        /// <summary>
        /// Method invoked to produce content from the <see cref="HelperResult"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> instance to write to.</param>
        /// <param name="encoder">The <see cref="IHtmlEncoder"/> to encode the content.</param>
        public virtual void WriteTo([NotNull] TextWriter writer, [NotNull] IHtmlEncoder encoder)
        {
            _action(writer);
        }
    }
}
