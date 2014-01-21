// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    // Supports JQuery schema on FormURL. 
    public class JQueryMvcFormUrlEncodedFormatter : FormUrlEncodedMediaTypeFormatter
    {
        public override bool CanReadType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            return true;
        }

        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (readStream == null)
            {
                throw new ArgumentNullException("readStream");
            }

            // For simple types, defer to base class
            if (base.CanReadType(type))
            {
                return base.ReadFromStreamAsync(type, readStream, content, formatterLogger);
            }

            return ReadFromStreamAsyncCore(type, readStream, content, formatterLogger);
        }

        private async Task<object> ReadFromStreamAsyncCore(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            object obj = await base.ReadFromStreamAsync(typeof(FormDataCollection), readStream, content, formatterLogger);
            FormDataCollection fd = (FormDataCollection)obj;

            try
            {
                throw new NotImplementedException();
                //return fd.ReadAs(type, String.Empty, RequiredMemberSelector, formatterLogger);
            }
            catch (Exception e)
            {
                if (formatterLogger == null)
                {
                    throw;
                }
                formatterLogger.LogError(String.Empty, e);
                return GetDefaultValueForType(type);
            }
        }
    }
}
