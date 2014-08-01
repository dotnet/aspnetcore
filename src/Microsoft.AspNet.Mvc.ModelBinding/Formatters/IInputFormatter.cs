// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public interface IInputFormatter
    {
        /// <summary>
        /// Gets the mutable collection of media types supported by this <see cref="JsonInputFormatter"/> instance.
        /// </summary>
        IList<MediaTypeHeaderValue> SupportedMediaTypes { get; }

        /// <summary>
        /// Gets the mutable collection of character encodings supported by this <see cref="JsonInputFormatter"/> 
        /// instance.
        /// </summary>
        IList<Encoding> SupportedEncodings { get; }

        /// <summary>
        /// Called during deserialization to read an object from the request.
        /// </summary>
        Task ReadAsync(InputFormatterContext context);
    }
}
