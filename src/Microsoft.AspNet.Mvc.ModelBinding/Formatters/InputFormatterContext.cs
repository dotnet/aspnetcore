using System;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class InputFormatterContext
    {
        public InputFormatterContext(ModelMetadata metadata, ModelStateDictionary modelState)
        {
            Metadata = metadata;
            ModelState = modelState;
        }

        public ModelMetadata Metadata { get; private set; }

        public ModelStateDictionary ModelState { get; private set; }

        public object Model { get; set; }
    }
}
