// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;

namespace System.Web.Http
{
    /// <summary>
    /// An action result that returns an empty <see cref="StatusCodes.Status500InternalServerError"/> response.
    /// </summary>
    public class InternalServerErrorResult : HttpStatusCodeResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InternalServerErrorResult"/> class.
        /// </summary>
        public InternalServerErrorResult()
            : base(StatusCodes.Status500InternalServerError)
        {
        }
    }
}