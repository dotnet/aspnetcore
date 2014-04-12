using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ClientModelValidationContext
    {
        public ClientModelValidationContext([NotNull] ModelMetadata metadata)
        {
            ModelMetadata = metadata;
        }

        public ModelMetadata ModelMetadata { get; private set; }
    }
}
