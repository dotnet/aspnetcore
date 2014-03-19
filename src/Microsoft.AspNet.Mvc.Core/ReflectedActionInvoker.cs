using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
{
    public class ReflectedActionInvoker : IActionInvoker
    {
        private readonly ActionContext _actionContext;
        private readonly ReflectedActionDescriptor _descriptor;
        private readonly IActionResultFactory _actionResultFactory;
        private readonly IControllerFactory _controllerFactory;
        private readonly IActionBindingContextProvider _bindingProvider;
        private readonly INestedProviderManager<FilterProviderContext> _filterProvider;

        private readonly List<IAuthorizationFilter> _authorizationFilters = new List<IAuthorizationFilter>();
        private readonly List<IActionFilter> _actionFilters = new List<IActionFilter>();
        private readonly List<IActionResultFilter> _actionResultFilters = new List<IActionResultFilter>();

        public ReflectedActionInvoker(ActionContext actionContext,
                                      ReflectedActionDescriptor descriptor,
                                      IActionResultFactory actionResultFactory,
                                      IControllerFactory controllerFactory,
                                      IActionBindingContextProvider bindingContextProvider,
                                      INestedProviderManager<FilterProviderContext> filterProvider)
        {
            _actionContext = actionContext;
            _descriptor = descriptor;
            _actionResultFactory = actionResultFactory;
            _controllerFactory = controllerFactory;
            _bindingProvider = bindingContextProvider;
            _filterProvider = filterProvider;
        }

        public async Task InvokeActionAsync()
        {
            IActionResult actionResult;
            var filterProviderContext =
                new FilterProviderContext(_descriptor,
                                          _descriptor.
                                          FilterDescriptors.
                                          Select(fd => new FilterItem(fd)).ToList());

            _filterProvider.Invoke(filterProviderContext);
            var filterMetaItems = filterProviderContext.Result.ToArray();

            PreArrangeFiltersInPipeline(filterProviderContext);

            var controller = _controllerFactory.CreateController(_actionContext);

            if (controller == null)
            {
                actionResult = new HttpStatusCodeResult(404);
            }
            else
            {
                var method = _descriptor.MethodInfo;

                if (method == null)
                {
                    actionResult = new HttpStatusCodeResult(404);
                }
                else
                {
                    if (_authorizationFilters.Count > 0)
                    {
                        var authZEndPoint = new AuthorizationFilterEndPoint();
                        _authorizationFilters.Add(authZEndPoint);

                        var authZContext = new AuthorizationFilterContext(_actionContext, filterMetaItems);
                        var authZPipeline = new FilterPipelineBuilder<AuthorizationFilterContext>(_authorizationFilters, authZContext);

                        await authZPipeline.InvokeAsync();

                        if (authZContext.ActionResult == null &&
                            !authZContext.HasFailed &&
                            authZEndPoint.EndPointCalled)
                        {
                            actionResult = null;
                        }
                        else
                        {
                            // User cleaned out the result but we failed or short circuited the end point.
                            actionResult = authZContext.ActionResult ?? new HttpStatusCodeResult(401);
                        }
                    }
                    else
                    {
                        actionResult = null;
                    }

                    if (actionResult == null)
                    {
                        var parameterValues = await GetParameterValues(_actionContext.ModelState);

                        var actionFilterContext = new ActionFilterContext(_actionContext,
                                                                          filterMetaItems,
                                                                          parameterValues);

                        var actionEndPoint = new ReflectedActionFilterEndPoint(_actionResultFactory, controller);

                        _actionFilters.Add(actionEndPoint);
                        var actionFilterPipeline = new FilterPipelineBuilder<ActionFilterContext>(_actionFilters,
                            actionFilterContext);

                        await actionFilterPipeline.InvokeAsync();

                        actionResult = actionFilterContext.ActionResult;
                    }
                }
            }

            var actionResultFilterContext = new ActionResultFilterContext(_actionContext, filterMetaItems, actionResult);
            var actionResultFilterEndPoint = new ActionResultFilterEndPoint();
            _actionResultFilters.Add(actionResultFilterEndPoint);

            var actionResultPipeline = new FilterPipelineBuilder<ActionResultFilterContext>(_actionResultFilters, actionResultFilterContext);

            await actionResultPipeline.InvokeAsync();
        }

        private async Task<IDictionary<string, object>> GetParameterValues(ModelStateDictionary modelState)
        {
            var actionBindingContext = await _bindingProvider.GetActionBindingContextAsync(_actionContext);
            var parameters = _descriptor.Parameters;
            var metadataProvider = actionBindingContext.MetadataProvider;
            var parameterValues = new Dictionary<string, object>(parameters.Count, StringComparer.Ordinal);

            for (var i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                if (parameter.BodyParameterInfo != null)
                {
                    var parameterType = parameter.BodyParameterInfo.ParameterType;
                    var modelMetadata = metadataProvider.GetMetadataForType(modelAccessor: null, modelType: parameterType);
                    var providerContext = new InputFormatterProviderContext(actionBindingContext.ActionContext.HttpContext,
                                                                            modelMetadata,
                                                                            modelState);

                    var inputFormatter = actionBindingContext.InputFormatterProvider.GetInputFormatter(providerContext);


                    var formatterContext = new InputFormatterContext(actionBindingContext.ActionContext.HttpContext,
                                                                     modelMetadata,
                                                                     modelState);
                    await inputFormatter.ReadAsync(formatterContext);
                    parameterValues[parameter.Name] = formatterContext.Model;
                }
                else
                {
                    var parameterType = parameter.ParameterBindingInfo.ParameterType;
                    var modelMetadata = metadataProvider.GetMetadataForType(modelAccessor: null, modelType: parameterType);

                    var modelBindingContext = new ModelBindingContext
                    {
                        ModelName = parameter.Name,
                        ModelState = modelState,
                        ModelMetadata = modelMetadata,
                        ModelBinder = actionBindingContext.ModelBinder,
                        ValueProvider = actionBindingContext.ValueProvider,
                        ValidatorProviders = actionBindingContext.ValidatorProviders,
                        MetadataProvider = metadataProvider,
                        HttpContext = actionBindingContext.ActionContext.HttpContext,
                        FallbackToEmptyPrefix = true
                    };
                    actionBindingContext.ModelBinder.BindModel(modelBindingContext);
                    parameterValues[parameter.Name] = modelBindingContext.Model;
                }
            }

            return parameterValues;
        }

        private void PreArrangeFiltersInPipeline(FilterProviderContext context)
        {
            if (context.Result == null || context.Result.Count == 0)
            {
                return;
            }

            foreach (var filter in context.Result)
            {
                PlaceFilter(filter.Filter);
            }
        }

        private void PlaceFilter(object filter)
        {
            var authFilter = filter as IAuthorizationFilter;
            var actionFilter = filter as IActionFilter;
            var actionResultFilter = filter as IActionResultFilter;

            if (authFilter != null)
            {
                _authorizationFilters.Add(authFilter);
            }

            if (actionFilter != null)
            {
                _actionFilters.Add(actionFilter);
            }

            if (actionResultFilter != null)
            {
                _actionResultFilters.Add(actionResultFilter);
            }
        }
    }
}
