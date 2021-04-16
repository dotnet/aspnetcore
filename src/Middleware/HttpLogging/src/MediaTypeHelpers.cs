using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.HttpLogging
{
    internal static class MediaTypeHelpers
    {
        private static List<Encoding> SupportedEncodings = new List<Encoding>()
        {
            Encoding.UTF8,
            Encoding.Unicode,
            Encoding.ASCII,
            Encoding.Latin1 // TODO allowed by default? Make this configurable?
        };

        public static bool TryGetEncodingForMediaType(string contentType, List<KeyValuePair<MediaTypeHeaderValue, Encoding>> mediaTypeList, out Encoding? encoding)
        {
            encoding = null;
            if (mediaTypeList == null || mediaTypeList.Count == 0 || string.IsNullOrEmpty(contentType))
            {
                return false;
            }

            var mediaType = new MediaTypeHeaderValue(contentType);

            if (mediaType.Charset.HasValue)
            {
                // Create encoding based on charset
                var requestEncoding = mediaType.Encoding;

                if (requestEncoding != null)
                {
                    for (var i = 0; i < SupportedEncodings.Count; i++)
                    {
                        if (string.Equals(requestEncoding.WebName,
                            SupportedEncodings[i].WebName,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            encoding = SupportedEncodings[i];
                            return true;
                        }
                    }
                }
            }
            else
            {
                foreach (var kvp in mediaTypeList)
                {
                    var type = kvp.Key;
                    if (mediaType.IsSubsetOf(type))
                    {
                        encoding = kvp.Value;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
