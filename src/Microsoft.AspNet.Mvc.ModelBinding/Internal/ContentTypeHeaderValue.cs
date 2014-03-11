using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding.Internal
{
    public class ContentTypeHeaderValue
    {
        public ContentTypeHeaderValue([NotNull] string contentType,
                                      string charSet)
        {
            ContentType = contentType;
            CharSet = charSet;
        }

        public string ContentType { get; private set; }

        public string CharSet { get; set; }

    }
}
