
using System;
using System.IO;
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

        private JsonSerializerSettings CreateSerializerSettings()
        {
            return new JsonSerializerSettings()
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,

                // Do not change this setting
                // Setting this to None prevents Json.NET from loading malicious, unsafe, or security-sensitive types.
                TypeNameHandling = TypeNameHandling.None
            };
        }

        private JsonSerializer CreateJsonSerializer()
        {
            var jsonSerializer = JsonSerializer.Create(SerializerSettings);
            return jsonSerializer;
        }

        private JsonWriter CreateJsonWriter([NotNull] TextWriter writer)
        {
            var jsonWriter = new JsonTextWriter(writer);
            if (Indent)
            {
                jsonWriter.Formatting = Formatting.Indented;
            }

            return jsonWriter;
        }

        public void Execute([NotNull] ViewComponentContext context)
        {
            using (var jsonWriter = CreateJsonWriter(context.Writer))
            {
                jsonWriter.CloseOutput = false;

                var jsonSerializer = CreateJsonSerializer();
                jsonSerializer.Serialize(jsonWriter, _value);

                jsonWriter.Flush();
            }
        }

        public async Task ExecuteAsync([NotNull] ViewComponentContext context)
        {
            Execute(context);
        }
    }
}
