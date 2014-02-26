using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class FormValueProviderFactory : IValueProviderFactory
    {
        private const string FormEncodedContentType = "application/x-www-form-urlencoded";

        public async Task<IValueProvider> GetValueProviderAsync(RequestContext requestContext)
        {
            var request = requestContext.HttpContext.Request;
            
            if (IsSupportedContentType(request))
            {
                var queryCollection = await request.GetFormAsync();
                var culture = GetCultureInfo(request);
                return new ReadableStringCollectionValueProvider(queryCollection, culture);
            }

            return null;
        }

        private bool IsSupportedContentType(HttpRequest request)
        {
            var contentType = request.Headers["Content-Type"];
            return !String.IsNullOrEmpty(contentType) && 
                   contentType.Equals(FormEncodedContentType, StringComparison.OrdinalIgnoreCase);
        }

        private static CultureInfo GetCultureInfo(HttpRequest request)
        {
            // TODO: Tracked via https://github.com/aspnet/HttpAbstractions/issues/10. Determine what's the right way to 
            // map Accept-Language to culture.
            return CultureInfo.CurrentCulture;
        }
    }
}
