using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public class TrimModelBinder : IModelBinder
    {
        private readonly ILogger _logger;

        public TrimModelBinder(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<TrimModelBinder>();
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                _logger.FoundNoValueInRequest(bindingContext);

                // no entry
                _logger.DoneAttemptingToBindModel(bindingContext);
                return Task.CompletedTask;
            }

            _logger.AttemptingToBindModel(bindingContext);

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

            var value = valueProviderResult.FirstValue;

            object model = null;
            //checking this condition for unit testing 
            if (bindingContext.ModelMetadata.CanTrim)
            {
                if (bindingContext.ModelMetadata.ConvertEmptyStringToNull && string.IsNullOrWhiteSpace(value))
                {
                    model = null;
                }
                else
                {
                    model = TrimModel(bindingContext, value);
                }
            }
            else if (string.IsNullOrWhiteSpace(value))
            {
                model = null;
            }
            else
            {
                model = value;
            }

            if (model == null)
            {
                bindingContext.Result = ModelBindingResult.Failed();
            }
            else
            {
                bindingContext.Result = ModelBindingResult.Success(model);
            }

            _logger.DoneAttemptingToBindModel(bindingContext);
            return Task.CompletedTask;
        }

        private string TrimModel(ModelBindingContext bindingContext, string value)
        {
            switch (bindingContext.ModelMetadata.TrimType)
            {
                case TrimType.Trim:
                    return value.Trim();
                case TrimType.TrimEnd:
                    return value.TrimEnd();
                case TrimType.TrimStart:
                    return value.TrimStart();
                default:
                    return value;
            }
        }
    }
}
