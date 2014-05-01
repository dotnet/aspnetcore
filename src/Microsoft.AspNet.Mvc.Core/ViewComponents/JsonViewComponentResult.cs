// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Mvc
{
    public class JsonViewComponentResult : IViewComponentResult
    {
        private readonly object _value;

        private JsonSerializerSettings _jsonSerializerSettings;

        public JsonViewComponentResult([NotNull] object value)
        {
            _value = value;
            _jsonSerializerSettings = JsonOutputFormatter.CreateDefaultSettings();
        }

        public JsonSerializerSettings SerializerSettings
        {
            get { return _jsonSerializerSettings; }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _jsonSerializerSettings = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to indent elements when writing data.
        /// </summary>
        public bool Indent { get; set; }

        public void Execute([NotNull] ViewComponentContext context)
        {
            var formatter = new JsonOutputFormatter(SerializerSettings, Indent);
            formatter.WriteObject(context.Writer, _value);
        }

        #pragma warning disable 1998
        public async Task ExecuteAsync([NotNull] ViewComponentContext context)
        {
            Execute(context);
        }
        #pragma warning restore 1998
    }
}
