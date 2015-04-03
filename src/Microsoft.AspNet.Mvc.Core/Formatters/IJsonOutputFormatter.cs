// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// An output formatter that specializes in writing JSON content.
    /// </summary>
    /// <remarks>
    /// The <see cref="JsonResult"/> class filter the collection of
    /// <see cref="MvcOptions.OutputFormatters"/> and use only those which implement
    /// <see cref="IJsonOutputFormatter"/>.
    ///
    /// To create a custom formatter that can be used by <see cref="JsonResult"/>, derive from
    /// <see cref="JsonOutputFormatter"/> or implement <see cref="IJsonOutputFormatter"/>.
    /// </remarks>
    public interface IJsonOutputFormatter : IOutputFormatter
    {
    }
}