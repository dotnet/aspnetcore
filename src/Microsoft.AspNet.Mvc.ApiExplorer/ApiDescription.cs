// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc.ApiExplorer
{
    /// <summary>
    /// Represents an API exposed by this application.
    /// </summary>
    public class ApiDescription
    {
        /// <summary>
        /// Creates a new instance of <see cref="ApiDescription"/>.
        /// </summary>
        public ApiDescription()
        {
            Properties = new Dictionary<object, object>();
            ParameterDescriptions = new List<ApiParameterDescription>();
            SupportedResponseFormats = new List<ApiResponseFormat>();
        }

        /// <summary>
        /// The <see cref="ActionDescriptor"/> for this api.
        /// </summary>
        public ActionDescriptor ActionDescriptor { get; set; }

        /// <summary>
        /// The group name for this api.
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// The supported HTTP method for this api, or null if all HTTP methods are supported.
        /// </summary>
        public string HttpMethod { get; set; }

        /// <summary>
        /// The list of <see cref="ApiParameterDescription"/> for this api.
        /// </summary>
        public IList<ApiParameterDescription> ParameterDescriptions { get; private set; }

        /// <summary>
        /// Stores arbitrary metadata properties associated with the <see cref="ApiDescription"/>.
        /// </summary>
        public IDictionary<object, object> Properties { get; private set; }

        /// <summary>
        /// The relative url path template (relative to application root) for this api.
        /// </summary>
        public string RelativePath { get; set; }

        /// <summary>
        /// The <see cref="ModelMetadata"/> for the <see cref="ResponseType"/> or null.
        /// </summary>
        /// <remarks>
        /// Will be null if <see cref="ResponseType"/> is null.
        /// </remarks>
        public ModelMetadata ResponseModelMetadata { get; set; }

        /// <summary>
        /// The CLR data type of the response or null.
        /// </summary>
        /// <remarks>
        /// Will be null if the action returns no response, or if the response type is unclear. Use
        /// <see cref="ProducesAttribute"/> on an action method to specify a response type.
        /// </remarks>
        public Type ResponseType { get; set; }

        /// <summary>
        /// A list of possible formats for a response.
        /// </summary>
        /// <remarks>
        /// Will be empty if the action returns no response, or if the response type is unclear. Use
        /// <see cref="ProducesAttribute"/> on an action method to specify a response type.
        /// </remarks>
        public IList<ApiResponseFormat> SupportedResponseFormats { get; private set; }
    }
}