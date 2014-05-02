// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
