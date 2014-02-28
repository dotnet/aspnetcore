using System;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public sealed class ModelValidatedEventArgs : EventArgs
    {
        public ModelValidatedEventArgs([NotNull] ModelValidationContext validationContext, 
                                       [NotNull] ModelValidationNode parentNode)
        {
            ValidationContext = validationContext;
            ParentNode = parentNode;
        }

        public ModelValidationContext ValidationContext { get; private set; }

        public ModelValidationNode ParentNode { get; private set; }
    }
}
