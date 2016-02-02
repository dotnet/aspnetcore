using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public static class ModelBinderExtensions
    {
        public static async Task<ModelBindingResult> BindModelResultAsync(
            this IModelBinder binder, 
            ModelBindingContext context)
        {
            await binder.BindModelAsync(context);
            return context.Result ?? default(ModelBindingResult);
        }
    }
}
