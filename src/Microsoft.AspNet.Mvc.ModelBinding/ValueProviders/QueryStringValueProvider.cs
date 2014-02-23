using System.Globalization;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class QueryStringValueProvider : NameValuePairsValueProvider
    {
        public QueryStringValueProvider(HttpContext context, CultureInfo culture)
            : base(context.Request.Query, culture)
        {
        }
    }
}
