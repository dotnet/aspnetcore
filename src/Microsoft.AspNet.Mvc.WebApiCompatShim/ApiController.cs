// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Principal;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.WebApiCompatShim;

namespace System.Web.Http
{
    [UseWebApiActionConventions]
    [UseWebApiOverloading]
    public abstract class ApiController : IDisposable
    {
        /// <summary>Gets the action context.</summary>
        /// <remarks>The setter is intended for unit testing purposes only.</remarks>
        [Activate]
        public ActionContext ActionContext { get; set; }

        /// <summary>
        /// Gets the http context.
        /// </summary>
        public HttpContext Context
        {
            get
            {
                return ActionContext?.HttpContext;
            }
        }

        /// <summary>
        /// Gets model state after the model binding process. This ModelState will be empty before model binding happens.
        /// </summary>
        public ModelStateDictionary ModelState
        {
            get
            {
                return ActionContext?.ModelState;
            }
        }

        /// <summary>Gets a factory used to generate URLs to other APIs.</summary>
        /// <remarks>The setter is intended for unit testing purposes only.</remarks>
        [Activate]
        public IUrlHelper Url { get; set; }

        /// <summary>Gets or sets the current principal associated with this request.</summary>
        public IPrincipal User
        {
            get
            {
                return Context?.User;
            }
        }

        [NonAction]
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
