using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Net.Http.Headers;

namespace FormatterWebSite
{
    public class StringInputFormatter : InputFormatter
    {
        public StringInputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/plain"));

            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        public override Task<object> ReadRequestBodyAsync(InputFormatterContext context)
        {
            var request = context.ActionContext.HttpContext.Request;
            MediaTypeHeaderValue requestContentType = null;
            MediaTypeHeaderValue.TryParse(request.ContentType, out requestContentType);
            var effectiveEncoding = SelectCharacterEncoding(requestContentType);

            using (var reader = new StreamReader(request.Body, effectiveEncoding))
            {
                var stringContent = reader.ReadToEnd();
                return Task.FromResult<object>(stringContent);
            }
        }
    }
}