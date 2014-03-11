using System;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Mvc.ModelBinding.Internal
{
    public static class HttpRequestExtensions
    {
        private const string ContentTypeHeader = "Content-Type";
        private const string CharSetToken = "charset=";

        public static ContentTypeHeaderValue GetContentType(this HttpRequest httpRequest)
        {
            var headerValue = httpRequest.Headers[ContentTypeHeader];
            if (!string.IsNullOrEmpty(headerValue))
            {
                var tokens = headerValue.Split(new[] { ';' }, 2);
                string charSet = null;
                if (tokens.Length > 1 && tokens[1].TrimStart().StartsWith(CharSetToken, StringComparison.OrdinalIgnoreCase))
                {
                    charSet = tokens[1].TrimStart().Substring(CharSetToken.Length);
                }
                return new ContentTypeHeaderValue(tokens[0], charSet);
                
            }
            return null;
        }
    }
}
