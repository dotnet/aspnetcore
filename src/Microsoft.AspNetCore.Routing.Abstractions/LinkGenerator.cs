// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Defines a contract to generate URLs to endpoints.
    /// </summary>
    public abstract class LinkGenerator
    {
        /// <summary>
        /// Generates a URL with an absolute path from the specified route values.
        /// </summary>
        /// <param name="values">An object that contains route values.</param>
        /// <returns>The generated URL.</returns>
        public string GetLink(object values)
        {
            return GetLink(httpContext: null, routeName: null, values, options: null);
        }

        /// <summary>
        /// Generates a URL with an absolute path from the specified route values and link options.
        /// </summary>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="options">The <see cref="LinkOptions"/>.</param>
        /// <returns>The generated URL.</returns>
        public string GetLink(object values, LinkOptions options)
        {
            return GetLink(httpContext: null, routeName: null, values, options);
        }

        /// <summary>
        /// Generates a URL with an absolute path from the specified route values.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="link">The generated URL.</param>
        /// <returns><c>true</c> if a URL was generated successfully; otherwise, <c>false</c>.</returns>
        public bool TryGetLink(object values, out string link)
        {
            return TryGetLink(httpContext: null, routeName: null, values, options: null, out link);
        }

        /// <summary>
        /// Generates a URL with an absolute path from the specified route values and link options.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="options">The <see cref="LinkOptions"/>.</param>
        /// <param name="link">The generated URL.</param>
        /// <returns><c>true</c> if a URL was generated successfully; otherwise, <c>false</c>.</returns>
        public bool TryGetLink(object values, LinkOptions options, out string link)
        {
            return TryGetLink(httpContext: null, routeName: null, values, options, out link);
        }

        /// <summary>
        /// Generates a URL with an absolute path from the specified route values.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with current request.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <returns>The generated URL.</returns>
        public string GetLink(HttpContext httpContext, object values)
        {
            return GetLink(httpContext, routeName: null, values, options: null);
        }

        /// <summary>
        /// Generates a URL with an absolute path from the specified route values.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with current request.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="link">The generated URL.</param>
        /// <returns><c>true</c> if a URL was generated successfully; otherwise, <c>false</c>.</returns>
        public bool TryGetLink(HttpContext httpContext, object values, out string link)
        {
            return TryGetLink(httpContext, routeName: null, values, options: null, out link);
        }

        /// <summary>
        /// Generates a URL with an absolute path from the specified route values and link options.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with current request.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="options">The <see cref="LinkOptions"/>.</param>
        /// <returns>The generated URL.</returns>
        public string GetLink(HttpContext httpContext, object values, LinkOptions options)
        {
            return GetLink(httpContext, routeName: null, values, options);
        }

        /// <summary>
        /// Generates a URL with an absolute path from the specified route values and link options.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with current request.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="options">The <see cref="LinkOptions"/>.</param>
        /// <param name="link">The generated URL.</param>
        /// <returns><c>true</c> if a URL was generated successfully; otherwise, <c>false</c>.</returns>
        public bool TryGetLink(HttpContext httpContext, object values, LinkOptions options, out string link)
        {
            return TryGetLink(httpContext, routeName: null, values, options, out link);
        }

        /// <summary>
        /// Generates a URL with an absolute path from the specified route name and route values.
        /// </summary>
        /// <param name="routeName">The name of the route to generate the URL to.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <returns>The generated URL.</returns>
        public string GetLink(string routeName, object values)
        {
            return GetLink(httpContext: null, routeName, values, options: null);
        }

        /// <summary>
        /// Generates a URL with an absolute path from the specified route name and route values.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="routeName">The name of the route to generate the URL to.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="link">The generated URL.</param>
        /// <returns><c>true</c> if a URL was generated successfully; otherwise, <c>false</c>.</returns>
        public bool TryGetLink(string routeName, object values, out string link)
        {
            return TryGetLink(httpContext: null, routeName, values, options: null, out link);
        }

        /// <summary>
        /// Generates a URL with an absolute path from the specified route name and route values.
        /// </summary>
        /// <param name="routeName">The name of the route to generate the URL to.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="options">The <see cref="LinkOptions"/>.</param>
        /// <returns>The generated URL.</returns>
        public string GetLink(string routeName, object values, LinkOptions options)
        {
            return GetLink(httpContext: null, routeName, values, options);
        }

        /// <summary>
        /// Generates a URL with an absolute path from the specified route name, route values and link options.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="routeName">The name of the route to generate the URL to.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="options">The <see cref="LinkOptions"/>.</param>
        /// <param name="link">The generated URL.</param>
        /// <returns><c>true</c> if a URL was generated successfully; otherwise, <c>false</c>.</returns>
        public bool TryGetLink(string routeName, object values, LinkOptions options, out string link)
        {
            return TryGetLink(httpContext: null, routeName, values, options, out link);
        }

        /// <summary>
        /// Generates a URL with an absolute path from the specified route name and route values.
        /// </summary>
        /// <param name="routeName">The name of the route to generate the URL to.</param>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with current request.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <returns>The generated URL.</returns>
        public string GetLink(HttpContext httpContext, string routeName, object values)
        {
            return GetLink(httpContext, routeName, values, options: null);
        }

        /// <summary>
        /// Generates a URL with an absolute path from the specified route name and route values.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with current request.</param>
        /// <param name="routeName">The name of the route to generate the URL to.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="link">The generated URL.</param>
        /// <returns><c>true</c> if a URL was generated successfully; otherwise, <c>false</c>.</returns>
        public bool TryGetLink(HttpContext httpContext, string routeName, object values, out string link)
        {
            return TryGetLink(httpContext, routeName, values, options: null, out link);
        }

        /// <summary>
        /// Generates a URL with an absolute path from the specified route name, route values and link options.
        /// </summary>
        /// <param name="routeName">The name of the route to generate the URL to.</param>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with current request.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="options">The <see cref="LinkOptions"/>.</param>
        /// <returns>The generated URL.</returns>
        public string GetLink(HttpContext httpContext, string routeName, object values, LinkOptions options)
        {
            if (TryGetLink(httpContext, routeName, values, options, out var link))
            {
                return link;
            }

            throw new InvalidOperationException("Could not find a matching endpoint to generate a link.");
        }

        /// <summary>
        /// Generates a URL with an absolute path from the specified route name, route values and link options.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with current request.</param>
        /// <param name="routeName">The name of the route to generate the URL to.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="options">The <see cref="LinkOptions"/>.</param>
        /// <param name="link">The generated URL.</param>
        /// <returns><c>true</c> if a URL was generated successfully; otherwise, <c>false</c>.</returns>
        public abstract bool TryGetLink(
            HttpContext httpContext,
            string routeName,
            object values,
            LinkOptions options,
            out string link);

        /// <summary>
        /// Generates a URL with an absolute path from the specified lookup information and route values.
        /// This lookup information is used to find endpoints using a registered 'IEndpointFinder&lt;TAddress&gt;'.
        /// </summary>
        /// <typeparam name="TAddress">The address type to look up endpoints.</typeparam>
        /// <param name="address">The information used to look up endpoints for generating a URL.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <returns>The generated URL.</returns>
        public string GetLinkByAddress<TAddress>(TAddress address, object values)
        {
            return GetLinkByAddress(httpContext: null, address, values, options: null);
        }

        /// <summary>
        /// Generates a URL with an absolute path from the specified lookup information and route values.
        /// This lookup information is used to find endpoints using a registered 'IEndpointFinder&lt;TAddress&gt;'.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <typeparam name="TAddress">The address type to look up endpoints.</typeparam>
        /// <param name="address">The information used to look up endpoints for generating a URL.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="link">The generated URL.</param>
        /// <returns><c>true</c> if a URL was generated successfully; otherwise, <c>false</c>.</returns>
        public bool TryGetLinkByAddress<TAddress>(TAddress address, object values, out string link)
        {
            return TryGetLinkByAddress(address, values, options: null, out link);
        }

        /// <summary>
        /// Generates a URL with an absolute path from the specified lookup information, route values and link options.
        /// This lookup information is used to find endpoints using a registered 'IEndpointFinder&lt;TAddress&gt;'.
        /// </summary>
        /// <typeparam name="TAddress">The address type to look up endpoints.</typeparam>
        /// <param name="address">The information used to look up endpoints for generating a URL.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="options">The <see cref="LinkOptions"/>.</param>
        /// <returns>The generated URL.</returns>
        public string GetLinkByAddress<TAddress>(TAddress address, object values, LinkOptions options)
        {
            return GetLinkByAddress(httpContext: null, address, values, options);
        }

        /// <summary>
        /// Generates a URL with an absolute path from the specified lookup information, route values and link options.
        /// This lookup information is used to find endpoints using a registered 'IEndpointFinder&lt;TAddress&gt;'.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <typeparam name="TAddress">The address type to look up endpoints.</typeparam>
        /// <param name="address">The information used to look up endpoints for generating a URL.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="options">The <see cref="LinkOptions"/>.</param>
        /// <param name="link">The generated URL.</param>
        /// <returns><c>true</c> if a URL was generated successfully; otherwise, <c>false</c>.</returns>
        public bool TryGetLinkByAddress<TAddress>(
            TAddress address,
            object values,
            LinkOptions options,
            out string link)
        {
            return TryGetLinkByAddress(httpContext: null, address, values, options, out link);
        }

        /// <summary>
        /// Generates a URL with an absolute path from the specified lookup information, route values and link options.
        /// This lookup information is used to find endpoints using a registered 'IEndpointFinder&lt;TAddress&gt;'.
        /// </summary>
        /// <typeparam name="TAddress">The address type to look up endpoints.</typeparam>
        /// <param name="address">The information used to look up endpoints for generating a URL.</param>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with current request.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <returns>The generated URL.</returns>
        public string GetLinkByAddress<TAddress>(HttpContext httpContext, TAddress address, object values)
        {
            return GetLinkByAddress(httpContext, address, values, options: null);
        }

        /// <summary>
        /// Generates a URL with an absolute path from the specified lookup information and route values.
        /// This lookup information is used to find endpoints using a registered 'IEndpointFinder&lt;TAddress&gt;'.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <typeparam name="TAddress">The address type to look up endpoints.</typeparam>
        /// <param name="address">The information used to look up endpoints for generating a URL.</param>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with current request.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="link">The generated URL.</param>
        /// <returns><c>true</c> if a URL was generated successfully; otherwise, <c>false</c>.</returns>
        public bool TryGetLinkByAddress<TAddress>(
            HttpContext httpContext,
            TAddress address,
            object values,
            out string link)
        {
            return TryGetLinkByAddress(httpContext, address, values, options: null, out link);
        }

        /// <summary>
        /// Generates a URL with an absolute path from the specified lookup information, route values and link options.
        /// This lookup information is used to find endpoints using a registered 'IEndpointFinder&lt;TAddress&gt;'.
        /// </summary>
        /// <typeparam name="TAddress">The address type to look up endpoints.</typeparam>
        /// <param name="address">The information used to look up endpoints for generating a URL.</param>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with current request.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="options">The <see cref="LinkOptions"/>.</param>
        /// <returns>The generated URL.</returns>
        public string GetLinkByAddress<TAddress>(
            HttpContext httpContext,
            TAddress address,
            object values,
            LinkOptions options)
        {
            if (TryGetLinkByAddress(httpContext, address, values, options, out var link))
            {
                return link;
            }

            throw new InvalidOperationException("Could not find a matching endpoint to generate a link.");
        }

        /// <summary>
        /// Generates a URL with an absolute path from the specified lookup information, route values and link options.
        /// This lookup information is used to find endpoints using a registered 'IEndpointFinder&lt;TAddress&gt;'.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <typeparam name="TAddress">The address type to look up endpoints.</typeparam>
        /// <param name="address">The information used to look up endpoints for generating a URL.</param>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with current request.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="options">The <see cref="LinkOptions"/>.</param>
        /// <param name="link">The generated URL.</param>
        /// <returns><c>true</c> if a URL was generated successfully; otherwise, <c>false</c>.</returns>
        public abstract bool TryGetLinkByAddress<TAddress>(
            HttpContext httpContext,
            TAddress address,
            object values,
            LinkOptions options,
            out string link);

        /// <summary>
        /// Gets a <see cref="LinkGenerationTemplate"/> to generate a URL from the specified route values.
        /// This template object holds information of the endpoint(s) that were found and which can later be used to
        /// generate a URL using the <see cref="LinkGenerationTemplate.MakeUrl(object, LinkOptions)"/> api.
        /// </summary>
        /// <param name="values">
        /// An object that contains route values. These values are used to lookup endpoint(s).
        /// </param>
        /// <returns>
        /// If an endpoint(s) was found successfully, then this returns a template object representing that,
        /// <c>null</c> otherwise.
        /// </returns>
        public LinkGenerationTemplate GetTemplate(object values)
        {
            return GetTemplate(httpContext: null, routeName: null, values);
        }

        /// <summary>
        /// Gets a <see cref="LinkGenerationTemplate"/> to generate a URL from the specified route name and route values.
        /// This template object holds information of the endpoint(s) that were found and which can later be used to
        /// generate a URL using the <see cref="LinkGenerationTemplate.MakeUrl(object, LinkOptions)"/> api.
        /// </summary>
        /// <param name="routeName">The name of the route to generate the URL to.</param>
        /// <param name="values">
        /// An object that contains route values. These values are used to lookup for endpoint(s).
        /// </param>
        /// <returns>
        /// If an endpoint(s) was found successfully, then this returns a template object representing that,
        /// <c>null</c> otherwise.
        /// </returns>
        public LinkGenerationTemplate GetTemplate(string routeName, object values)
        {
            return GetTemplate(httpContext: null, routeName, values);
        }

        /// <summary>
        /// Gets a <see cref="LinkGenerationTemplate"/> to generate a URL from the specified route values.
        /// This template object holds information of the endpoint(s) that were found and which can later be used to
        /// generate a URL using the <see cref="LinkGenerationTemplate.MakeUrl(object, LinkOptions)"/> api.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with current request.</param>
        /// <param name="values">
        /// An object that contains route values. These values are used to lookup for endpoint(s).
        /// </param>
        /// <returns>
        /// If an endpoint(s) was found successfully, then this returns a template object representing that,
        /// <c>null</c> otherwise.
        /// </returns>
        public LinkGenerationTemplate GetTemplate(HttpContext httpContext, object values)
        {
            return GetTemplate(httpContext, routeName: null, values);
        }

        /// <summary>
        /// Gets a <see cref="LinkGenerationTemplate"/> to generate a URL from the specified route name and route values.
        /// This template object holds information of the endpoint(s) that were found and which can later be used to
        /// generate a URL using the <see cref="LinkGenerationTemplate.MakeUrl(object, LinkOptions)"/> api.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with current request.</param>
        /// <param name="routeName">The name of the route to generate the URL to.</param>
        /// <param name="values">
        /// An object that contains route values. These values are used to lookup for endpoint(s).
        /// </param>
        /// <returns>
        /// If an endpoint(s) was found successfully, then this returns a template object representing that,
        /// <c>null</c> otherwise.
        /// </returns>
        public abstract LinkGenerationTemplate GetTemplate(HttpContext httpContext, string routeName, object values);

        /// <summary>
        /// Gets a <see cref="LinkGenerationTemplate"/> to generate a URL from the specified lookup information.
        /// This template object holds information of the endpoint(s) that were found and which can later be used to
        /// generate a URL using the <see cref="LinkGenerationTemplate.MakeUrl(object, LinkOptions)"/> api.
        /// The lookup information is used to find endpoints using a registered 'IEndpointFinder&lt;TAddress&gt;'.
        /// </summary>
        /// <typeparam name="TAddress">The address type to look up endpoints.</typeparam>
        /// <param name="address">The information used to look up endpoints for creating a template.</param>
        /// <returns>
        /// If an endpoint(s) was found successfully, then this returns a template object representing that,
        /// <c>null</c> otherwise.
        /// </returns>
        public LinkGenerationTemplate GetTemplateByAddress<TAddress>(TAddress address)
        {
            return GetTemplateByAddress(httpContext: null, address);
        }

        /// <summary>
        /// Gets a <see cref="LinkGenerationTemplate"/> to generate a URL from the specified lookup information.
        /// This template object holds information of the endpoint(s) that were found and which can later be used to
        /// generate a URL using the <see cref="LinkGenerationTemplate.MakeUrl(object, LinkOptions)"/> api.
        /// The lookup information is used to find endpoints using a registered 'IEndpointFinder&lt;TAddress&gt;'.
        /// </summary>
        /// <typeparam name="TAddress">The address type to look up endpoints.</typeparam>
        /// <param name="address">The information used to look up endpoints for creating a template.</param>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with current request.</param>
        /// <returns>
        /// If an endpoint(s) was found successfully, then this returns a template object representing that,
        /// <c>null</c> otherwise.
        /// </returns>
        public abstract LinkGenerationTemplate GetTemplateByAddress<TAddress>(
            HttpContext httpContext,
            TAddress address);
    }
}
