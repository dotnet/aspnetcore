// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Mvc
{
    public class JsonResult : ActionResult
    {
        private const int BufferSize = 1024;

        private JsonSerializerSettings _jsonSerializerSettings;
        private Encoding _encoding = Encodings.UTF8EncodingWithoutBOM;

        public JsonResult([NotNull] object data)
        {
            Data = data;
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

        public object Data { get; private set; }

        public override void ExecuteResult([NotNull] ActionContext context)
        {
            var response = context.HttpContext.Response;
            var writeStream = response.Body;

            if (response.ContentType == null)
            {
                response.ContentType = "application/json";
            }

            using (var writer = new StreamWriter(writeStream, Encoding, BufferSize, leaveOpen: true))
            {
                var formatter = new JsonOutputFormatter(SerializerSettings, Indent);
                formatter.WriteObject(writer, Data);
            }
        }
    }
}
