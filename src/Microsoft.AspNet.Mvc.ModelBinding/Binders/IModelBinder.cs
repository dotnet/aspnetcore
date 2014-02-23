using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Interface for model binding.
    /// </summary>
    public interface IModelBinder
    {
        bool BindModel(ModelBindingContext bindingContext);
    }
}
