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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Mvc
{
    public class JsonResult : ActionResult
    {
        private readonly object _returnValue;

        private JsonSerializerSettings _jsonSerializerSettings;
        private Encoding _encoding = Encoding.UTF8;

        public JsonResult(object returnValue)
        {
            if (returnValue == null)
            {
                throw new ArgumentNullException("returnValue");
            }

            _returnValue = returnValue;
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

        public Encoding Encoding
        {
            get { return _encoding; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _encoding = value;
            }
        }

        public override void ExecuteResult([NotNull] ActionContext context)
        {
            HttpResponse response = context.HttpContext.Response;

            Stream writeStream = response.Body;

            if (response.ContentType == null)
            {
                response.ContentType = "application/json";
            }

            using (var writer = new StreamWriter(writeStream, Encoding, 1024, leaveOpen: true))
            {
                var formatter = new JsonOutputFormatter(SerializerSettings, Indent);
                formatter.WriteObject(writer, _returnValue);
            }
        }
    }
}
