// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    ///     Represents the HttpResponseBody as a PipeWriter
    /// </summary>
    public interface IResponseBodyPipeFeature
    {
        /// <summary>
        /// A <see cref="PipeWriter"/> representing the response body, if any.
        /// </summary>
        PipeWriter Writer { get; set; }
    }
}
