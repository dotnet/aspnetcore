using System.ComponentModel;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public sealed class ModelValidatingEventArgs : CancelEventArgs
    {
        public ModelValidatingEventArgs([NotNull] ModelValidationContext validationContext, 
                                        [NotNull] ModelValidationNode parentNode)
        {
            ValidationContext = validationContext;
            ParentNode = parentNode;
        }

        public ModelValidationContext ValidationContext { get; private set; }

        public ModelValidationNode ParentNode { get; private set; }
    }
}
