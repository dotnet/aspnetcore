using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class InputFormatterProviderContext
    {
        public InputFormatterProviderContext([NotNull] HttpContext httpContext,
                                             [NotNull] ModelMetadata metadata, 
                                             [NotNull] ModelStateDictionary modelState)
        {
            HttpContext = httpContext;
            Metadata = metadata;
            ModelState = modelState;
        }

        public HttpContext HttpContext { get; private set; }

        public ModelMetadata Metadata { get; private set; }

        public ModelStateDictionary ModelState { get; private set; }
    }
}
