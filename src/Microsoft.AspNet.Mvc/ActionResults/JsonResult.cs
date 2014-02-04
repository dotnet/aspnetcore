using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Mvc
{
    public class JsonResult : IActionResult
    {
        private readonly object _returnValue;

        private JsonSerializerSettings _jsonSerializerSettings;
        private Encoding _encoding;

        public JsonResult(object returnValue)
        {
            if (returnValue == null)
            {
                throw new ArgumentNullException("returnValue");
            }

            Encoding = Encoding.UTF8;

            _returnValue = returnValue;
            _jsonSerializerSettings = CreateSerializerSettings();
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

        public virtual JsonSerializerSettings CreateSerializerSettings()
        {
            return new JsonSerializerSettings()
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,

                // Do not change this setting
                // Setting this to None prevents Json.NET from loading malicious, unsafe, or security-sensitive types.
                TypeNameHandling = TypeNameHandling.None
            };
        }

        public virtual JsonSerializer CreateJsonSerializer()
        {
            JsonSerializer jsonSerializer = JsonSerializer.Create(SerializerSettings);

            return jsonSerializer;
        }

        public virtual JsonWriter CreateJsonWriter(Stream writeStream, Encoding effectiveEncoding)
        {
            JsonWriter jsonWriter = new JsonTextWriter(new StreamWriter(writeStream, effectiveEncoding));
            if (Indent)
            {
                jsonWriter.Formatting = Formatting.Indented;
            }

            return jsonWriter;
        }

        public async Task ExecuteResultAsync(RequestContext context)
        {
            HttpResponse response = context.HttpContext.Response;

            Stream writeStream = response.Body;

            response.ContentType = "application/json";

            using (JsonWriter jsonWriter = CreateJsonWriter(writeStream, Encoding))
            {
                jsonWriter.CloseOutput = false;

                JsonSerializer jsonSerializer = CreateJsonSerializer();
                jsonSerializer.Serialize(jsonWriter, _returnValue);

                jsonWriter.Flush();
            }
        }
    }
}
