// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using Microsoft.AspNet.Mvc;

namespace System.Web.Http
{
    /// <summary>
    /// An action result that returns an empty <see cref="HttpStatusCode.OK"/> response.
    /// </summary>
    public class OkResult : HttpStatusCodeResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OkResult"/> class.
        /// </summary>
        public OkResult()
            : base((int)HttpStatusCode.OK)
        {
        }
    }
}