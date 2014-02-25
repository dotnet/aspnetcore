using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
{
    public class ReflectedActionInvoker : IActionInvoker
    {
        private readonly ActionContext _actionContext;
        private readonly ReflectedActionDescriptor _descriptor;
        private readonly IActionResultFactory _actionResultFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly IControllerFactory _controllerFactory;
        private readonly IActionBindingContextProvider _bindingProvider;

        public ReflectedActionInvoker(ActionContext actionContext,
                                      ReflectedActionDescriptor descriptor,
                                      IActionResultFactory actionResultFactory,
                                      IControllerFactory controllerFactory,
                                      IActionBindingContextProvider bindingContextProvider,
                                      IServiceProvider serviceProvider)
        {
            _actionContext = actionContext;
            _descriptor = descriptor;
            _actionResultFactory = actionResultFactory;
            _controllerFactory = controllerFactory;
            _bindingProvider = bindingContextProvider;
            _serviceProvider = serviceProvider;
        }

        public async Task InvokeActionAsync()
        {
            IActionResult actionResult = null;

            object controller = _controllerFactory.CreateController(_actionContext.HttpContext, _descriptor);

            if (controller == null)
            {
                actionResult = new HttpStatusCodeResult(404);
            }
            else
            {
                var modelState = new ModelStateDictionary();
                InitializeController(controller, modelState);

                var method = _descriptor.MethodInfo;

                if (method == null)
                {
                    actionResult = new HttpStatusCodeResult(404);
                }
                else
                {
                    var parameterValues = await GetParameterValues(modelState);

                    object actionReturnValue = method.Invoke(controller, GetArgumentValues(parameterValues));

                    actionResult = _actionResultFactory.CreateActionResult(method.ReturnType, actionReturnValue, _actionContext);
                }
            }

            // TODO: This will probably move out once we got filters
            await actionResult.ExecuteResultAsync(_actionContext);
        }

        private void InitializeController(object controller, ModelStateDictionary modelState)
        {
            var controllerType = controller.GetType();

            foreach (var prop in controllerType.GetRuntimeProperties())
            {
                if (prop.Name == "Context")
                {
                    if (prop.PropertyType == typeof(HttpContext))
                    {
                        prop.SetValue(controller, _actionContext.HttpContext);
                    }
                }
                else if (prop.Name == "ModelState" && prop.PropertyType == typeof(ModelStateDictionary))
                {
                    prop.SetValue(controller, modelState);
                }
            }

            var method = controllerType.GetRuntimeMethods().FirstOrDefault(m => m.Name.Equals("Initialize", StringComparison.OrdinalIgnoreCase));

            if (method == null)
            {
                return;
            }

            var args = method.GetParameters()
                             .Select(p => _serviceProvider.GetService(p.ParameterType)).ToArray();

            method.Invoke(controller, args);
        }

        private async Task<IDictionary<string, object>> GetParameterValues(ModelStateDictionary modelState)
        {
            var actionBindingContext = await _bindingProvider.GetActionBindingContextAsync(_actionContext);
            var parameters = _descriptor.Parameters;

            var parameterValues = new Dictionary<string, object>(parameters.Count, StringComparer.Ordinal);
            for (int i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                if (parameter.BodyParameterInfo != null)
                {
                    var inputFormatterContext = actionBindingContext.CreateInputFormatterContext(
                                                        modelState,
                                                        parameter);
                    await actionBindingContext.InputFormatter.ReadAsync(inputFormatterContext);
                    parameterValues[parameter.Name] = inputFormatterContext.Model;
                }
                else
                {
                    var modelBindingContext = actionBindingContext.CreateModelBindingContext(
                                                        modelState,
                                                        parameter);
                    actionBindingContext.ModelBinder.BindModel(modelBindingContext);
                    parameterValues[parameter.Name] = modelBindingContext.Model;
                }

            }

            return parameterValues;
        }

        private object[] GetArgumentValues(IDictionary<string, object> parameterValues)
        {
            var parameters = _descriptor.MethodInfo.GetParameters();
            var arguments = new object[parameters.Length];

            for (int i = 0; i < arguments.Length; i++)
            {
                object value;
                if (parameterValues.TryGetValue(parameters[i].Name, out value))
                {
                    arguments[i] = value;
                }
                else
                {
                    arguments[i] = parameters[i].DefaultValue;
                }
            }

            return arguments;
        }
    }
}
