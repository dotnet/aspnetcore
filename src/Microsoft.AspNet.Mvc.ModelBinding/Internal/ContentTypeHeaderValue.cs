
namespace Microsoft.AspNet.Mvc.ModelBinding
{
    internal class ContentTypeHeaderValue
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
