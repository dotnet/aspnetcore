// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Description;
using Microsoft.Framework.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Specifies the allowed content types and the type of the value returned by the action
    /// which can be used to select a formatter while executing <see cref="ObjectResult"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ProducesAttribute : ResultFilterAttribute, IApiResponseMetadataProvider
    {
        /// <summary>
        /// Initializes an instance of <see cref="ProducesAttribute"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of object that is going to be written in the response.</param>
        public ProducesAttribute([NotNull] Type type)
        {
            Type = type;
            ContentTypes = new List<MediaTypeHeaderValue>();
        }

        /// <summary>
        /// Initializes an instance of <see cref="ProducesAttribute"/> with allowed content types.
        /// </summary>
        /// <param name="contentType">The allowed content type for a response.</param>
        /// <param name="additionalContentTypes">Additional allowed content types for a response.</param>
        public ProducesAttribute([NotNull] string contentType, params string[] additionalContentTypes)
        {
            ContentTypes = GetContentTypes(contentType, additionalContentTypes);
        }

        public Type Type { get; set; }

        public IList<MediaTypeHeaderValue> ContentTypes { get; set; }

        public override void OnResultExecuting([NotNull] ResultExecutingContext context)
        {
            base.OnResultExecuting(context);
            var objectResult = context.Result as ObjectResult;

            if (objectResult != null)
            {
                // Check if there are any IFormatFilter in the pipeline, and if any of them is active. If there is one,
                // do not override the content type value.
                if (context.Filters.OfType<IFormatFilter>().All(f => !f.IsActive))
                {
                    SetContentTypes(objectResult.ContentTypes);
                }
            }
        }

        private List<MediaTypeHeaderValue> GetContentTypes(string firstArg, string[] args)
        {
            var completeArgs = new List<string>();
            completeArgs.Add(firstArg);
            completeArgs.AddRange(args);
            var contentTypes = new List<MediaTypeHeaderValue>();
            foreach (var arg in completeArgs)
            {
                var contentType = MediaTypeHeaderValue.Parse(arg);
                if (contentType.MatchesAllSubTypes || contentType.MatchesAllTypes)
                {
                    throw new InvalidOperationException(
                        Resources.FormatMatchAllContentTypeIsNotAllowed(arg));
                }

                contentTypes.Add(contentType);
            }

            return contentTypes;
        }

        public void SetContentTypes(IList<MediaTypeHeaderValue> contentTypes)
        {
            contentTypes.Clear();
            foreach (var contentType in ContentTypes)
            {
                contentTypes.Add(contentType);
            }
        }
    }
}
