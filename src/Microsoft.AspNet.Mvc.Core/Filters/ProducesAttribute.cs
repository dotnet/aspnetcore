// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Description;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Specifies the allowed content types and the type of the value returned by the action 
    /// which can be used to select a formatter while executing <see cref="ObjectResult"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ProducesAttribute : ResultFilterAttribute, IApiResponseMetadataProvider
    {
        public ProducesAttribute(string contentType, params string[] additionalContentTypes)
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
                SetContentTypes(objectResult.ContentTypes);
            }
        }

        private List<MediaTypeHeaderValue> GetContentTypes(string firstArg, string[] args)
        {
            var contentTypes = new List<MediaTypeHeaderValue>();
            contentTypes.Add(MediaTypeHeaderValue.Parse(firstArg));
            foreach (var item in args)
            {
                var contentType = MediaTypeHeaderValue.Parse(item);
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
