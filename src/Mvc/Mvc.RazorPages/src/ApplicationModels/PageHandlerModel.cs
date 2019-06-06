// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// Represents a handler in a <see cref="PageApplicationModel"/>.
    /// </summary>
    [DebuggerDisplay("PageHandlerModel: Name={" + nameof(PageHandlerModel.Name) + "}")]
    public class PageHandlerModel : ICommonModel
    {
        /// <summary>
        /// Creates a new <see cref="PageHandlerModel"/>.
        /// </summary>
        /// <param name="handlerMethod">The <see cref="System.Reflection.MethodInfo"/> for the handler.</param>
        /// <param name="attributes">Any attributes annotated on the handler method.</param>
        public PageHandlerModel(
            MethodInfo handlerMethod,
            IReadOnlyList<object> attributes)
        {
            MethodInfo = handlerMethod ?? throw new ArgumentNullException(nameof(handlerMethod));
            Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));

            Parameters = new List<PageParameterModel>();
            Properties = new Dictionary<object, object>();
        }

        /// <summary>
        /// Creates a new instance of <see cref="PageHandlerModel"/> from a given <see cref="PageHandlerModel"/>.
        /// </summary>
        /// <param name="other">The <see cref="PageHandlerModel"/> which needs to be copied.</param>
        public PageHandlerModel(PageHandlerModel other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            MethodInfo = other.MethodInfo;
            HandlerName = other.HandlerName;
            HttpMethod = other.HttpMethod;
            Name = other.Name;

            Page = other.Page;

            // These are just metadata, safe to create new collections
            Attributes = new List<object>(other.Attributes);
            Properties = new Dictionary<object, object>(other.Properties);

            // Make a deep copy of other 'model' types.
            Parameters = new List<PageParameterModel>(other.Parameters.Select(p => new PageParameterModel(p) { Handler = this }));
        }

        /// <summary>
        /// Gets the <see cref="System.Reflection.MethodInfo"/> for the handler.
        /// </summary>
        public MethodInfo MethodInfo { get; }

        /// <summary>
        /// Gets or sets the HTTP method supported by this handler.
        /// </summary>
        public string HttpMethod { get; set; }

        /// <summary>
        /// Gets or sets the handler method name.
        /// </summary>
        public string HandlerName { get; set; }

        /// <summary>
        /// Gets or sets a descriptive name for the handler.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the sequence of <see cref="PageParameterModel"/> instances.
        /// </summary>
        public IList<PageParameterModel> Parameters { get; }

        /// <summary>
        /// Gets or sets the <see cref="PageApplicationModel"/>.
        /// </summary>
        public PageApplicationModel Page { get; set; }

        /// <inheritdoc />
        public IReadOnlyList<object> Attributes { get; }

        /// <inheritdoc />
        public IDictionary<object, object> Properties { get; }

        MemberInfo ICommonModel.MemberInfo => MethodInfo;
    }
}
