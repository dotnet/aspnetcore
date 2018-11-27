// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace System.Web.Http
{
    /// <summary>
    /// An action result that returns an empty <see cref="StatusCodes.Status409Conflict"/> response.
    /// </summary>
    public class ConflictResult : StatusCodeResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictResult"/> class.
        /// </summary>
        public ConflictResult()
            : base(StatusCodes.Status409Conflict)
        {
        }
    }
}