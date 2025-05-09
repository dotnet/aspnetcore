using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// A generic ModelBinder that supports both 'application/json' and 'multipart/form-data' content types.
/// For multipart requests, it expects a 'payload' field containing a JSON object.
/// Optionally, the target model can implement <see cref="IFormFileReceiver"/> to receive uploaded files from the form.
/// </summary>
public class FromFormOrJsonModelBinder<T> : IModelBinder where T : class
{
    private static readonly JsonSerializerOptions _defaultJsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var request = bindingContext.HttpContext.Request;
        var modelName = bindingContext.ModelName;

        if (string.IsNullOrEmpty(request.ContentType))
        {
            bindingContext.ModelState.AddModelError(modelName, "Missing Content-Type header.");
            bindingContext.Result = ModelBindingResult.Failed();
            return;
        }

        try
        {
            T? model = null;

            if (request.ContentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                var form = await request.ReadFormAsync(bindingContext.HttpContext.RequestAborted);

                if (form.TryGetValue("payload", out var payloadJson) && !StringValues.IsNullOrEmpty(payloadJson))
                {
                    model = JsonSerializer.Deserialize<T>(payloadJson.ToString(), _defaultJsonOptions);
                }

                if (model is IFormFileReceiver receiver)
                {
                    receiver.SetFiles(form.Files);
                }
            }
            else if (request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
            {
                model = await JsonSerializer.DeserializeAsync<T>(
                    request.Body,
                    _defaultJsonOptions,
                    bindingContext.HttpContext.RequestAborted
                );
            }
            else
            {
                bindingContext.ModelState.AddModelError(modelName, $"Unsupported Content-Type '{request.ContentType}'.");
                bindingContext.Result = ModelBindingResult.Failed();
                return;
            }

            if (model is null)
            {
                bindingContext.ModelState.AddModelError(modelName, "Could not deserialize the request body.");
                bindingContext.Result = ModelBindingResult.Failed();
                return;
            }

            bindingContext.Result = ModelBindingResult.Success(model);
        }
        catch (JsonException)
        {
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Invalid JSON payload.");
            bindingContext.Result = ModelBindingResult.Failed();
        }
        catch (Exception)
        {
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Unexpected error during model binding.");
            bindingContext.Result = ModelBindingResult.Failed();
        }
    }
}
