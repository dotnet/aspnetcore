// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.Routing;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Specifies what HTTP methods an action supports.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class AcceptVerbsAttribute : Attribute, IActionHttpMethodProvider, IRouteTemplateProvider
    {
        private readonly IEnumerable<string> _httpMethods;
        private int? _order;

        /// <summary>
        /// Initializes a new instance of the <see cref="AcceptVerbsAttribute" /> class.
        /// </summary>
        /// <param name="method">The HTTP method the action supports.</param>
        public AcceptVerbsAttribute([NotNull] string method)
            : this(new string[] { method })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcceptVerbsAttribute" /> class.
        /// </summary>
        /// <param name="methods">The HTTP methods the action supports.</param>
        public AcceptVerbsAttribute(params string[] methods)
        {
            // TODO: This assumes that the methods are exactly same as standard Http Methods.
            // The Http Abstractions should take care of these.
            _httpMethods = methods.Select(method => method.ToUpperInvariant());
        }

        /// <summary>
        /// Gets the HTTP methods the action supports.
        /// </summary>
        public IEnumerable<string> HttpMethods
        {
            get
            {
                return _httpMethods;
            }
        }

        /// <summary>
        /// The route template. May be null.
        /// </summary>
        public string Route { get; set; }

        /// <inheritdoc />
        string IRouteTemplateProvider.Template
        {
            get { return Route; }
        }

        /// <summary>
        /// Gets the route order. The order determines the order of route execution. Routes with a lower
        /// order value are tried first. When a route doesn't specify a value, it gets the value of the
        /// <see cref="RouteAttribute.Order"/> or a default value of 0 if the <see cref="RouteAttribute"/>
        /// doesn't define a value on the controller.
        /// </summary>
        public int Order
        {
            get { return _order ?? 0; }
            set { _order = value; }
        }

        /// <inheritdoc />
        int? IRouteTemplateProvider.Order
        {
            get
            {
                return _order;
            }
        }

        /// <inheritdoc />
        public string Name { get; set; }
    }
}