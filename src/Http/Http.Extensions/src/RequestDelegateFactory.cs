// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Creates <see cref="RequestDelegate"/> implementations from <see cref="Delegate"/> request handlers.
    /// </summary>
    public static class RequestDelegateFactory
    {
        private static readonly MethodInfo ChangeTypeMethodInfo = GetMethodInfo<Func<object, Type, object>>((value, type) => Convert.ChangeType(value, type, CultureInfo.InvariantCulture));
        private static readonly MethodInfo ExecuteTaskOfTMethodInfo = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteTask), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo ExecuteTaskOfStringMethodInfo = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteTaskOfString), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo ExecuteValueTaskOfTMethodInfo = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteValueTaskOfT), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo ExecuteValueTaskMethodInfo = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteValueTask), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo ExecuteValueTaskOfStringMethodInfo = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteValueTaskOfString), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo ExecuteTaskResultOfTMethodInfo = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteTaskResult), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo ExecuteValueResultTaskOfTMethodInfo = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteValueTaskResult), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo GetRequiredServiceMethodInfo = typeof(ServiceProviderServiceExtensions).GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService), BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(IServiceProvider) })!;
        private static readonly MethodInfo ResultWriteResponseAsync = typeof(IResult).GetMethod(nameof(IResult.ExecuteAsync), BindingFlags.Public | BindingFlags.Instance)!;
        private static readonly MethodInfo StringResultWriteResponseAsync = GetMethodInfo<Func<HttpResponse, string, Task>>((response, text) => HttpResponseWritingExtensions.WriteAsync(response, text, default));
        private static readonly MethodInfo JsonResultWriteResponseAsync = GetMethodInfo<Func<HttpResponse, object, Task>>((response, value) => HttpResponseJsonExtensions.WriteAsJsonAsync(response, value, default));

        private static readonly PropertyInfo CompletedTaskPropertyInfo = (PropertyInfo)GetMemberInfo<Func<Task>>(() => Task.CompletedTask);

        private static readonly MethodInfo LogParameterBindingFailure = GetMethodInfo<Action<HttpContext, string, string, string>>((httpContext, parameterType, parameterName, sourceValue) =>
            Log.ParameterBindingFailed(httpContext, parameterType, parameterName, sourceValue));

        private static readonly ParameterExpression TargetArg = Expression.Parameter(typeof(object), "target");
        private static readonly ParameterExpression HttpContextParameter = Expression.Parameter(typeof(HttpContext), "httpContext");

        private static readonly ParameterExpression DeserializedBodyParameter = Expression.Parameter(typeof(object), "bodyValue");
        private static readonly ParameterExpression WasTryParseFailureVariable = Expression.Variable(typeof(bool), "wasTryParseFailure");

        private static readonly MemberExpression RequestServicesExpr = Expression.Property(HttpContextParameter, nameof(HttpContext.RequestServices));
        private static readonly MemberExpression HttpRequestExpr = Expression.Property(HttpContextParameter, nameof(HttpContext.Request));
        private static readonly MemberExpression HttpResponseExpr = Expression.Property(HttpContextParameter, nameof(HttpContext.Response));
        private static readonly MemberExpression RequestAbortedExpr = Expression.Property(HttpContextParameter, nameof(HttpContext.RequestAborted));
        private static readonly MemberExpression RouteValuesProperty = Expression.Property(HttpRequestExpr, nameof(HttpRequest.RouteValues));
        private static readonly MemberExpression QueryProperty = Expression.Property(HttpRequestExpr, nameof(HttpRequest.Query));
        private static readonly MemberExpression HeadersProperty = Expression.Property(HttpRequestExpr, nameof(HttpRequest.Headers));
        private static readonly MemberExpression FormProperty = Expression.Property(HttpRequestExpr, nameof(HttpRequest.Form));
        private static readonly MemberExpression StatusCodeProperty = Expression.Property(HttpResponseExpr, nameof(HttpResponse.StatusCode));

        /// <summary>
        /// Creates a <see cref="RequestDelegate"/> implementation for <paramref name="action"/>.
        /// </summary>
        /// <param name="action">A request handler with any number of custom parameters that often produces a response with its return value.</param>
        /// <returns>The <see cref="RequestDelegate"/>.</returns>
        public static RequestDelegate Create(Delegate action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var targetExpression = action.Target switch
            {
                object => Expression.Convert(TargetArg, action.Target.GetType()),
                null => null,
            };

            var targetableRequestDelegate = CreateTargetableRequestDelegate(action.Method, targetExpression);

            return httpContext =>
            {
                return targetableRequestDelegate(action.Target, httpContext);
            };
        }

        /// <summary>
        /// Creates a <see cref="RequestDelegate"/> implementation for <paramref name="methodInfo"/>.
        /// </summary>Microsoft.AspNetCore.Routing.MapAction"
        /// <param name="methodInfo">A static request handler with any number of custom parameters that often produces a response with its return value.</param>
        /// <returns>The <see cref="RequestDelegate"/>.</returns>
        public static RequestDelegate Create(MethodInfo methodInfo)
        {
            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            var targetableRequestDelegate = CreateTargetableRequestDelegate(methodInfo, targetExpression: null);

            return httpContext =>
            {
                return targetableRequestDelegate(null, httpContext);
            };
        }

        /// <summary>
        /// Creates a <see cref="RequestDelegate"/> implementation for <paramref name="methodInfo"/>.
        /// </summary>
        /// <param name="methodInfo">A request handler with any number of custom parameters that often produces a response with its return value.</param>
        /// <param name="targetFactory">Creates the <see langword="this"/> for the non-static method.</param>
        /// <returns>The <see cref="RequestDelegate"/>.</returns>
        public static RequestDelegate Create(MethodInfo methodInfo, Func<HttpContext, object> targetFactory)
        {
            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            if (targetFactory is null)
            {
                throw new ArgumentNullException(nameof(targetFactory));
            }

            if (methodInfo.DeclaringType is null)
            {
                throw new ArgumentException($"A {nameof(targetFactory)} was provided, but {nameof(methodInfo)} does not have a Declaring type.");
            }

            var targetExpression = Expression.Convert(TargetArg, methodInfo.DeclaringType);
            var targetableRequestDelegate = CreateTargetableRequestDelegate(methodInfo, targetExpression);

            return httpContext =>
            {
                return targetableRequestDelegate(targetFactory(httpContext), httpContext);
            };
        }

        private static Func<object?, HttpContext, Task> CreateTargetableRequestDelegate(MethodInfo methodInfo, Expression? targetExpression)
        {
            // Non void return type

            // Task Invoke(HttpContext httpContext)
            // {
            //     // Action parameters are bound from the request, services, etc... based on attribute and type information.
            //     return ExecuteTask(action(...), httpContext);
            // }

            // void return type

            // Task Invoke(HttpContext httpContext)
            // {
            //     action(...);
            //     return default;
            // }

            var factoryContext = new FactoryContext();

            var arguments = CreateArguments(methodInfo.GetParameters(), factoryContext);

            var responseWritingMethodCall = factoryContext.CheckForTryParseFailure ?
                CreateTryParseCheckingResponseWritingMethodCall(methodInfo, targetExpression, arguments) :
                CreateResponseWritingMethodCall(methodInfo, targetExpression, arguments);

            return HandleRequestBodyAndCompileRequestDelegate(responseWritingMethodCall, factoryContext);
        }

        private static Expression[] CreateArguments(ParameterInfo[]? parameters, FactoryContext factoryContext)
        {
            if (parameters is null || parameters.Length == 0)
            {
                return Array.Empty<Expression>();
            }

            var args = new Expression[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                args[i] = CreateArgument(parameters[i], factoryContext);
            }

            return args;
        }

        private static Expression CreateArgument(ParameterInfo parameter, FactoryContext factoryContext)
        {
            if (parameter.Name is null)
            {
                // TODO: Add test!
                throw new InvalidOperationException($"Parameter {parameter} does not have a name! All parameters must be named.");
            }

            var parameterCustomAttributes = parameter.GetCustomAttributes();

            if (parameterCustomAttributes.OfType<IFromRouteMetadata>().FirstOrDefault() is { } routeAttribute)
            {
                return BindParameterFromProperty(parameter, RouteValuesProperty, routeAttribute.Name ?? parameter.Name, factoryContext);
            }
            else if (parameterCustomAttributes.OfType<IFromQueryMetadata>().FirstOrDefault() is { } queryAttribute)
            {
                return BindParameterFromProperty(parameter, QueryProperty, queryAttribute.Name ?? parameter.Name, factoryContext);
            }
            else if (parameterCustomAttributes.OfType<IFromHeaderMetadata>().FirstOrDefault() is { } headerAttribute)
            {
                return BindParameterFromProperty(parameter, HeadersProperty, headerAttribute.Name ?? parameter.Name, factoryContext);
            }
            else if (parameterCustomAttributes.OfType<IFromBodyMetadata>().FirstOrDefault() is { } bodyAttribute)
            {
                if (factoryContext.RequestBodyMode is RequestBodyMode.AsJson)
                {
                    throw new InvalidOperationException("Action cannot have more than one FromBody attribute.");
                }

                if (factoryContext.RequestBodyMode is RequestBodyMode.AsForm)
                {
                    ThrowCannotReadBodyDirectlyAndAsForm();
                }

                factoryContext.RequestBodyMode = RequestBodyMode.AsJson;
                factoryContext.JsonRequestBodyType = parameter.ParameterType;
                factoryContext.AllowEmptyRequestBody = bodyAttribute.AllowEmpty;

                return Expression.Convert(DeserializedBodyParameter, parameter.ParameterType);
            }
            else if (parameterCustomAttributes.OfType<IFromFormMetadata>().FirstOrDefault() is { } formAttribute)
            {
                if (factoryContext.RequestBodyMode is RequestBodyMode.AsJson)
                {
                    ThrowCannotReadBodyDirectlyAndAsForm();
                }

                factoryContext.RequestBodyMode = RequestBodyMode.AsForm;

                return BindParameterFromProperty(parameter, FormProperty, formAttribute.Name ?? parameter.Name, factoryContext);
            }
            else if (parameter.CustomAttributes.Any(a => typeof(IFromServiceMetadata).IsAssignableFrom(a.AttributeType)))
            {
                return Expression.Call(GetRequiredServiceMethodInfo.MakeGenericMethod(parameter.ParameterType), RequestServicesExpr);
            }
            else if (parameter.ParameterType == typeof(IFormCollection))
            {
                if (factoryContext.RequestBodyMode is RequestBodyMode.AsJson)
                {
                    ThrowCannotReadBodyDirectlyAndAsForm();
                }

                factoryContext.RequestBodyMode = RequestBodyMode.AsForm;

                return Expression.Property(HttpRequestExpr, nameof(HttpRequest.Form));
            }
            else if (parameter.ParameterType == typeof(HttpContext))
            {
                return HttpContextParameter;
            }
            else if (parameter.ParameterType == typeof(CancellationToken))
            {
                return RequestAbortedExpr;
            }
            else if (parameter.ParameterType == typeof(string))
            {
                return BindParameterFromRouteValueOrQueryString(parameter, parameter.Name, factoryContext);
            }
            else if (HasTryParseMethod(parameter))
            {
                return BindParameterFromRouteValueOrQueryString(parameter, parameter.Name, factoryContext);
            }
            else
            {
                return Expression.Call(GetRequiredServiceMethodInfo.MakeGenericMethod(parameter.ParameterType), RequestServicesExpr);
            }
        }

        private static Expression CreateMethodCall(MethodInfo methodInfo, Expression? target, Expression[] arguments) =>
            target is null ?
                Expression.Call(methodInfo, arguments) :
                Expression.Call(target, methodInfo, arguments);

        private static Expression CreateResponseWritingMethodCall(MethodInfo methodInfo, Expression? target, Expression[] arguments)
        {
            var callMethod = CreateMethodCall(methodInfo, target, arguments);
            return AddResponseWritingToMethodCall(callMethod, methodInfo.ReturnType);
        }

        // If we're calling TryParse and the WasTryParseFailureVariable indicates it failed, set a 400 StatusCode instead of calling the method.
        private static Expression CreateTryParseCheckingResponseWritingMethodCall(MethodInfo methodInfo, Expression? target, Expression[] arguments)
        {
            // {
            //     bool wasTryParseFailure = false;
            //
            //     // Assume "int id" is the first parameter.
            //     int param1 =
            //     {
            //          var sourceValue = httpContext.Request.RouteValue["id"];
            //          int parsedValue = default;
            //
            //          if (!int.TryParse(sourceValue, out parsedValue))
            //          {
            //              Log.ParameterBindingFailed(httpContext, "int", "id", sourceValue)
            //              wasTryParseFailure = true;
            //          }
            //
            //          return parsedValue;
            //     };
            //     // ...
            //
            //     return wasTryParseFailure ?
            //         {
            //              httpContext.Response.StatusCode = 400;
            //              return Task.CompletedTask;
            //         } :
            //         {
            //             // Logic generated by AddResponseWritingToMethodCall() that calls action(param1, ...)
            //         };
            // }

            var parameters = methodInfo.GetParameters();
            var storedArguments = new ParameterExpression[parameters.Length];
            var localVariables = new ParameterExpression[parameters.Length + 1];

            for (var i = 0; i < parameters.Length; i++)
            {
                storedArguments[i] = localVariables[i] = Expression.Parameter(parameters[i].ParameterType);
            }

            localVariables[parameters.Length] = WasTryParseFailureVariable;

            var assignAndCall = new Expression[parameters.Length + 1];
            for (var i = 0; i < parameters.Length; i++)
            {
                assignAndCall[i] = Expression.Assign(localVariables[i], arguments[i]);
            }

            var set400StatusAndReturnCompletedTask = Expression.Block(
                    Expression.Assign(StatusCodeProperty, Expression.Constant(400)),
                    Expression.Property(null, CompletedTaskPropertyInfo));

            var methodCall = CreateMethodCall(methodInfo, target, storedArguments);

            var checkWasTryParseFailure = Expression.Condition(WasTryParseFailureVariable,
                set400StatusAndReturnCompletedTask,
                AddResponseWritingToMethodCall(methodCall, methodInfo.ReturnType));

            assignAndCall[parameters.Length] = checkWasTryParseFailure;

            return Expression.Block(localVariables, assignAndCall);
        }

        private static Expression AddResponseWritingToMethodCall(Expression methodCall, Type returnType)
        {
            // Exact request delegate match
            if (returnType == typeof(void))
            {
                return Expression.Block(
                    methodCall,
                    Expression.Property(null, CompletedTaskPropertyInfo));
            }
            else if (AwaitableInfo.IsTypeAwaitable(returnType, out _))
            {
                if (returnType == typeof(Task))
                {
                    return methodCall;
                }
                else if (returnType == typeof(ValueTask))
                {
                    return Expression.Call(
                        ExecuteValueTaskMethodInfo,
                        methodCall);
                }
                else if (returnType.IsGenericType &&
                         returnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var typeArg = returnType.GetGenericArguments()[0];

                    if (typeof(IResult).IsAssignableFrom(typeArg))
                    {
                        return Expression.Call(
                            ExecuteTaskResultOfTMethodInfo.MakeGenericMethod(typeArg),
                            methodCall,
                            HttpContextParameter);
                    }
                    // ExecuteTask<T>(action(..), httpContext);
                    else if (typeArg == typeof(string))
                    {
                        return Expression.Call(
                            ExecuteTaskOfStringMethodInfo,
                            methodCall,
                            HttpContextParameter);
                    }
                    else
                    {
                        return Expression.Call(
                            ExecuteTaskOfTMethodInfo.MakeGenericMethod(typeArg),
                            methodCall,
                            HttpContextParameter);
                    }
                }
                else if (returnType.IsGenericType &&
                         returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
                {
                    var typeArg = returnType.GetGenericArguments()[0];

                    if (typeof(IResult).IsAssignableFrom(typeArg))
                    {
                        return Expression.Call(
                            ExecuteValueResultTaskOfTMethodInfo.MakeGenericMethod(typeArg),
                            methodCall,
                            HttpContextParameter);
                    }
                    // ExecuteTask<T>(action(..), httpContext);
                    else if (typeArg == typeof(string))
                    {
                        return Expression.Call(
                            ExecuteValueTaskOfStringMethodInfo,
                            methodCall,
                            HttpContextParameter);
                    }
                    else
                    {
                        return Expression.Call(
                            ExecuteValueTaskOfTMethodInfo.MakeGenericMethod(typeArg),
                            methodCall,
                            HttpContextParameter);
                    }
                }
                else
                {
                    // TODO: Handle custom awaitables
                    throw new NotSupportedException($"Unsupported return type: {returnType}");
                }
            }
            else if (typeof(IResult).IsAssignableFrom(returnType))
            {
                return Expression.Call(methodCall, ResultWriteResponseAsync, HttpContextParameter);
            }
            else if (returnType == typeof(string))
            {
                return Expression.Call(StringResultWriteResponseAsync, HttpResponseExpr, methodCall, Expression.Constant(CancellationToken.None));
            }
            else if (returnType.IsValueType)
            {
                var box = Expression.TypeAs(methodCall, typeof(object));
                return Expression.Call(JsonResultWriteResponseAsync, HttpResponseExpr, box, Expression.Constant(CancellationToken.None));
            }
            else
            {
                return Expression.Call(JsonResultWriteResponseAsync, HttpResponseExpr, methodCall, Expression.Constant(CancellationToken.None));
            }
        }

        private static Func<object?, HttpContext, Task> HandleRequestBodyAndCompileRequestDelegate(Expression responseWritingMethodCall, FactoryContext factoryContext)
        {
            if (factoryContext.RequestBodyMode is RequestBodyMode.AsJson)
            {
                // We need to generate the code for reading from the body before calling into the delegate
                var invoker = Expression.Lambda<Func<object?, HttpContext, object?, Task>>(
                    responseWritingMethodCall, TargetArg, HttpContextParameter, DeserializedBodyParameter).Compile();

                var bodyType = factoryContext.JsonRequestBodyType!;
                object? defaultBodyValue = null;

                if (factoryContext.AllowEmptyRequestBody && bodyType.IsValueType)
                {
                    defaultBodyValue = Activator.CreateInstance(bodyType);
                }

                return async (target, httpContext) =>
                {
                    object? bodyValue;

                    if (factoryContext.AllowEmptyRequestBody && httpContext.Request.ContentLength == 0)
                    {
                        bodyValue = defaultBodyValue;
                    }
                    else
                    {
                        try
                        {
                            bodyValue = await httpContext.Request.ReadFromJsonAsync(bodyType);
                        }
                        catch (IOException ex)
                        {
                            Log.RequestBodyIOException(httpContext, ex);
                            return;
                        }
                        catch (InvalidDataException ex)
                        {
                            Log.RequestBodyInvalidDataException(httpContext, ex);
                            httpContext.Response.StatusCode = 400;
                            return;
                        }
                    }

                    await invoker(target, httpContext, bodyValue);
                };
            }
            else if (factoryContext.RequestBodyMode is RequestBodyMode.AsForm)
            {
                var invoker = Expression.Lambda<Func<object?, HttpContext, Task>>(
                    responseWritingMethodCall, TargetArg, HttpContextParameter).Compile();

                return async (target, httpContext) =>
                {
                    // Generating async code would just be insane so if the method needs the form populate it here
                    // so the within the method it's cached
                    try
                    {
                        await httpContext.Request.ReadFormAsync();
                    }
                    catch (IOException ex)
                    {
                        Log.RequestBodyIOException(httpContext, ex);
                        httpContext.Abort();
                        return;
                    }
                    catch (InvalidDataException ex)
                    {
                        Log.RequestBodyInvalidDataException(httpContext, ex);
                        httpContext.Response.StatusCode = 400;
                        return;
                    }

                    await invoker(target, httpContext);
                };
            }
            else
            {
                return Expression.Lambda<Func<object?, HttpContext, Task>>(
                    responseWritingMethodCall, TargetArg, HttpContextParameter).Compile();
            }
        }

        // Todo: Cache this.
        // Todo: Use CultureInfo.InvariantCulture where possible.
        private static MethodInfo? FindTryParseMethod(Type type)
        {
            var staticMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

            foreach (var method in staticMethods)
            {
                if (method.Name != "TryParse" || method.ReturnType != typeof(bool))
                {
                    continue;
                }

                var tryParseParameters = method.GetParameters();

                if (tryParseParameters.Length == 2 &&
                    tryParseParameters[0].ParameterType == typeof(string) &&
                    tryParseParameters[1].IsOut &&
                    tryParseParameters[1].ParameterType == type.MakeByRefType())
                {
                    return method;
                }
            }

            return null;
        }

        private static bool HasTryParseMethod(ParameterInfo parameter)
        {
            var parameterType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
            return FindTryParseMethod(parameterType) is not null;
        }

        private static Expression GetValueFromProperty(Expression sourceExpression, string key)
        {
            var itemProperty = sourceExpression.Type.GetProperty("Item");
            var indexArguments = new[] { Expression.Constant(key) };
            var indexExpression = Expression.MakeIndex(sourceExpression, itemProperty, indexArguments);
            return Expression.Convert(indexExpression, typeof(string));
        }

        private static Expression BindParameterFromValue(ParameterInfo parameter, Expression valueExpression, FactoryContext factoryContext)
        {
            Expression argumentExpression;

            if (parameter.ParameterType == typeof(string))
            {
                argumentExpression = valueExpression;
            }
            else
            {
                var parameterType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
                var tryParseMethod = FindTryParseMethod(parameterType);

                if (tryParseMethod is null)
                {
                    // TODO: Add test!
                    throw new InvalidOperationException($"No public static {parameter.ParameterType}.TryParse(string, out {parameter.ParameterType}) method found for {parameter}.");
                }

                // bool wasTryParseFailureVariable = false;
                //
                // // Assume "int id" is the first parameter.
                // int param1 =
                // {
                //      var sourceValue = httpContext.Request.RouteValue["id"];
                //      int parsedValue = default;
                //
                //      if (!int.TryParse(sourceValue, out parsedValue))
                //      {
                //          Log.ParameterBindingFailed(httpContext, "int", "id", sourceValue)
                //          wasTryParseFailureVariable = true;
                //      }
                //
                //      return parsedValue;
                // };

                factoryContext.CheckForTryParseFailure = true;

                var parsedValue = Expression.Variable(parameter.ParameterType);

                var tryParseCall = Expression.Call(tryParseMethod, valueExpression, parsedValue);

                var parameterTypeConstant = Expression.Constant(parameterType.Name);
                var parameterNameConstant = Expression.Constant(parameter.Name);
                var failBlock = Expression.Block(
                    Expression.Assign(WasTryParseFailureVariable, Expression.Constant(true)),
                    Expression.Call(LogParameterBindingFailure, HttpContextParameter, parameterTypeConstant, parameterNameConstant, valueExpression));

                var ifFailExpression = Expression.IfThen(Expression.Not(tryParseCall), failBlock);

                argumentExpression = Expression.Block(new[] { parsedValue },
                    ifFailExpression,
                    parsedValue);
            }

            if (argumentExpression.Type != parameter.ParameterType)
            {
                argumentExpression = Expression.Convert(argumentExpression, parameter.ParameterType);
            }

            Expression defaultExpression = parameter.HasDefaultValue ?
                Expression.Constant(parameter.DefaultValue) :
                Expression.Default(parameter.ParameterType);

            // property[key] == null ? default : (ParameterType){Type}.Parse(property[key]);
            return Expression.Condition(
                Expression.Equal(valueExpression, Expression.Constant(null)),
                defaultExpression,
                argumentExpression);
        }

        private static Expression BindParameterFromProperty(ParameterInfo parameter, MemberExpression property, string key, FactoryContext factoryContext) =>
            BindParameterFromValue(parameter, GetValueFromProperty(property, key), factoryContext);

        private static Expression BindParameterFromRouteValueOrQueryString(ParameterInfo parameter, string key, FactoryContext factoryContext)
        {
            var routeValue = GetValueFromProperty(RouteValuesProperty, key);
            var queryValue = GetValueFromProperty(QueryProperty, key);
            return BindParameterFromValue(parameter, Expression.Coalesce(routeValue, queryValue), factoryContext);
        }

        private static MethodInfo GetMethodInfo<T>(Expression<T> expr)
        {
            var mc = (MethodCallExpression)expr.Body;
            return mc.Method;
        }

        private static MemberInfo GetMemberInfo<T>(Expression<T> expr)
        {
            var mc = (MemberExpression)expr.Body;
            return mc.Member;
        }

        private static Task ExecuteTask<T>(Task<T> task, HttpContext httpContext)
        {
            static async Task ExecuteAwaited(Task<T> task, HttpContext httpContext)
            {
                await httpContext.Response.WriteAsJsonAsync(await task);
            }

            if (task.IsCompletedSuccessfully)
            {
                return httpContext.Response.WriteAsJsonAsync(task.GetAwaiter().GetResult());
            }

            return ExecuteAwaited(task, httpContext);
        }

        private static Task ExecuteTaskOfString(Task<string> task, HttpContext httpContext)
        {
            static async Task ExecuteAwaited(Task<string> task, HttpContext httpContext)
            {
                await httpContext.Response.WriteAsync(await task);
            }

            if (task.IsCompletedSuccessfully)
            {
                return httpContext.Response.WriteAsync(task.GetAwaiter().GetResult());
            }

            return ExecuteAwaited(task, httpContext);
        }

        private static Task ExecuteValueTask(ValueTask task)
        {
            static async Task ExecuteAwaited(ValueTask task)
            {
                await task;
            }

            if (task.IsCompletedSuccessfully)
            {
                task.GetAwaiter().GetResult();
            }

            return ExecuteAwaited(task);
        }

        private static Task ExecuteValueTaskOfT<T>(ValueTask<T> task, HttpContext httpContext)
        {
            static async Task ExecuteAwaited(ValueTask<T> task, HttpContext httpContext)
            {
                await httpContext.Response.WriteAsJsonAsync(await task);
            }

            if (task.IsCompletedSuccessfully)
            {
                return httpContext.Response.WriteAsJsonAsync(task.GetAwaiter().GetResult());
            }

            return ExecuteAwaited(task, httpContext);
        }

        private static Task ExecuteValueTaskOfString(ValueTask<string> task, HttpContext httpContext)
        {
            static async Task ExecuteAwaited(ValueTask<string> task, HttpContext httpContext)
            {
                await httpContext.Response.WriteAsync(await task);
            }

            if (task.IsCompletedSuccessfully)
            {
                return httpContext.Response.WriteAsync(task.GetAwaiter().GetResult());
            }

            return ExecuteAwaited(task, httpContext);
        }

        private static Task ExecuteValueTaskResult<T>(ValueTask<T> task, HttpContext httpContext) where T : IResult
        {
            static async Task ExecuteAwaited(ValueTask<T> task, HttpContext httpContext)
            {
                await (await task).ExecuteAsync(httpContext);
            }

            if (task.IsCompletedSuccessfully)
            {
                return task.GetAwaiter().GetResult().ExecuteAsync(httpContext);
            }

            return ExecuteAwaited(task, httpContext);
        }

        private static async Task ExecuteTaskResult<T>(Task<T> task, HttpContext httpContext) where T : IResult
        {
            await (await task).ExecuteAsync(httpContext);
        }

        [StackTraceHidden]
        private static void ThrowCannotReadBodyDirectlyAndAsForm()
        {
            throw new InvalidOperationException("Action cannot mix FromBody and FromForm on the same method.");
        }

        private enum RequestBodyMode
        {
            None,
            AsJson,
            AsForm,
        }

        private class FactoryContext
        {
            public RequestBodyMode RequestBodyMode { get; set; }
            public Type? JsonRequestBodyType { get; set; }
            public bool AllowEmptyRequestBody { get; set; }

            public bool CheckForTryParseFailure { get; set; }
        }

        private static class Log
        {
            private static readonly Action<ILogger, Exception> _requestBodyIOException = LoggerMessage.Define(
                LogLevel.Debug,
                new EventId(1, "RequestBodyIOException"),
                "Reading the request body failed with an IOException.");

            private static readonly Action<ILogger, Exception> _requestBodyInvalidDataException = LoggerMessage.Define(
                LogLevel.Debug,
                new EventId(2, "RequestBodyInvalidDataException"),
                "Reading the request body failed with an InvalidDataException.");

            private static readonly Action<ILogger, string, string, string, Exception?> _parameterBindingFailed = LoggerMessage.Define<string, string, string>(
                LogLevel.Debug,
                new EventId(3, "ParamaterBindingFailed"),
                @"Failed to bind parameter ""{ParameterType} {ParameterName}"" from ""{SourceValue}"".");

            public static void RequestBodyIOException(HttpContext httpContext, IOException exception)
            {
                _requestBodyIOException(GetLogger(httpContext), exception);
            }

            public static void RequestBodyInvalidDataException(HttpContext httpContext, InvalidDataException exception)
            {
                _requestBodyInvalidDataException(GetLogger(httpContext), exception);
            }

            public static void ParameterBindingFailed(HttpContext httpContext, string parameterType, string parameterName, string sourceValue)
            {
                _parameterBindingFailed(GetLogger(httpContext), parameterType, parameterName, sourceValue, null);
            }

            private static ILogger GetLogger(HttpContext httpContext)
            {
                var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                return loggerFactory.CreateLogger(typeof(RequestDelegateFactory));
            }
        }
    }
}
