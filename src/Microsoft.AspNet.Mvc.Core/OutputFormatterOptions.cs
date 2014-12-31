using System;
using System.Collections.Generic;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc.Core
{
    public class OutputFormatterOptions
    {
        private Dictionary<string, MediaTypeHeaderValue> map = new Dictionary<string, MediaTypeHeaderValue>();

        public void AddFormatMapping(string format, MediaTypeHeaderValue contentType)
        {
            if (!string.IsNullOrEmpty(format) && contentType != null)
            {
                if(format.StartsWith("."))
                {
                    format = format.TrimStart('.');
                }
                                
                map[format.ToLower()] = contentType;
            }
        }

        public MediaTypeHeaderValue GetContentTypeForFormat(string format)
        {
            if (!string.IsNullOrEmpty(format))
            {
                if (format.StartsWith("."))
                {
                    format = format.TrimStart('.');
                }

                if (map.ContainsKey(format.ToLower()))
                {
                    return map[format.ToLower()];
                }
                else
                {
                    return null;
                }
            }

            return null;
        }
    }
}