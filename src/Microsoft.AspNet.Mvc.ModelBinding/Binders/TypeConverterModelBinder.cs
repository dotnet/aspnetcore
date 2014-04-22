using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public sealed class TypeConverterModelBinder : IModelBinder
    {
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The exception is recorded to be acted upon later.")]
        [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.Web.Http.ValueProviders.ValueProviderResult.ConvertTo(System.Type)", Justification = "The ValueProviderResult already has the necessary context to perform a culture-aware conversion.")]
        public bool BindModel(ModelBindingContext bindingContext)
        {
            ModelBindingHelper.ValidateBindingContext(bindingContext);

            if (!ValueProviderResult.CanConvertFromString(bindingContext.ModelType))
            {
                // this type cannot be converted
                return false;
            }

            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == null)
            {
                return false; // no entry
            }

            object newModel;
            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);
            try
            {
                newModel = valueProviderResult.ConvertTo(bindingContext.ModelType);
                ModelBindingHelper.ReplaceEmptyStringWithNull(bindingContext.ModelMetadata, ref newModel);
                bindingContext.Model = newModel;
            }
            catch (Exception ex)
            {
                if (IsFormatException(ex))
                {
                    // there was a type conversion failure
                    bindingContext.ModelState.AddModelError(bindingContext.ModelName, ex.Message);
                }
                else
                {
                    bindingContext.ModelState.AddModelError(bindingContext.ModelName, ex);
                }
            }

            return true;
        }

        private static bool IsFormatException(Exception ex)
        {
            for (; ex != null; ex = ex.InnerException)
            {
                if (ex is FormatException)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
