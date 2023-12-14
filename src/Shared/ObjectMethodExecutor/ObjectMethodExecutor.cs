// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Extensions.Internal;

[RequiresUnreferencedCode("ObjectMethodExecutor performs reflection on arbitrary types.")]
[RequiresDynamicCode("ObjectMethodExecutor performs reflection on arbitrary types.")]
internal sealed class ObjectMethodExecutor
{
    private readonly object?[]? _parameterDefaultValues;
    private readonly MethodExecutorAsync? _executorAsync;
    private readonly MethodExecutor? _executor;

    private static readonly ConstructorInfo _objectMethodExecutorAwaitableConstructor =
        typeof(ObjectMethodExecutorAwaitable).GetConstructor(new[] {
            typeof(object),                 // customAwaitable
            typeof(Func<object, object>),   // getAwaiterMethod
            typeof(Func<object, bool>),     // isCompletedMethod
            typeof(Func<object, object>),   // getResultMethod
            typeof(Action<object, Action>), // onCompletedMethod
            typeof(Action<object, Action>)  // unsafeOnCompletedMethod
    })!;

    private ObjectMethodExecutor(MethodInfo methodInfo, TypeInfo targetTypeInfo, object?[]? parameterDefaultValues)
    {
        ArgumentNullException.ThrowIfNull(methodInfo);

        MethodInfo = methodInfo;
        MethodParameters = methodInfo.GetParameters();
        TargetTypeInfo = targetTypeInfo;
        MethodReturnType = methodInfo.ReturnType;

        var isAwaitable = CoercedAwaitableInfo.IsTypeAwaitable(MethodReturnType, out var coercedAwaitableInfo);

        IsMethodAsync = isAwaitable;
        AsyncResultType = isAwaitable ? coercedAwaitableInfo.AwaitableInfo.ResultType : null;

        // Upstream code may prefer to use the sync-executor even for async methods, because if it knows
        // that the result is a specific Task<T> where T is known, then it can directly cast to that type
        // and await it without the extra heap allocations involved in the _executorAsync code path.
        _executor = GetExecutor(methodInfo, targetTypeInfo);

        if (IsMethodAsync)
        {
            _executorAsync = GetExecutorAsync(methodInfo, targetTypeInfo, coercedAwaitableInfo);
        }

        _parameterDefaultValues = parameterDefaultValues;
    }

    private delegate ObjectMethodExecutorAwaitable MethodExecutorAsync(object target, object?[]? parameters);

    private delegate object? MethodExecutor(object target, object?[]? parameters);

    private delegate void VoidMethodExecutor(object target, object?[]? parameters);

    public MethodInfo MethodInfo { get; }

    public ParameterInfo[] MethodParameters { get; }

    public TypeInfo TargetTypeInfo { get; }

    public Type? AsyncResultType { get; }

    // This field is made internal set because it is set in unit tests.
    public Type MethodReturnType { get; internal set; }

    public bool IsMethodAsync { get; }

    public static ObjectMethodExecutor Create(MethodInfo methodInfo, TypeInfo targetTypeInfo)
    {
        return new ObjectMethodExecutor(methodInfo, targetTypeInfo, null);
    }

    public static ObjectMethodExecutor Create(MethodInfo methodInfo, TypeInfo targetTypeInfo, object?[] parameterDefaultValues)
    {
        ArgumentNullException.ThrowIfNull(parameterDefaultValues);

        return new ObjectMethodExecutor(methodInfo, targetTypeInfo, parameterDefaultValues);
    }

    /// <summary>
    /// Executes the configured method on <paramref name="target"/>. This can be used whether or not
    /// the configured method is asynchronous.
    /// </summary>
    /// <remarks>
    /// Even if the target method is asynchronous, it's desirable to invoke it using Execute rather than
    /// ExecuteAsync if you know at compile time what the return type is, because then you can directly
    /// "await" that value (via a cast), and then the generated code will be able to reference the
    /// resulting awaitable as a value-typed variable. If you use ExecuteAsync instead, the generated
    /// code will have to treat the resulting awaitable as a boxed object, because it doesn't know at
    /// compile time what type it would be.
    /// </remarks>
    /// <param name="target">The object whose method is to be executed.</param>
    /// <param name="parameters">Parameters to pass to the method.</param>
    /// <returns>The method return value.</returns>
    public object? Execute(object target, object?[]? parameters)
    {
        Debug.Assert(_executor != null, "Sync execution is not supported.");
        return _executor(target, parameters);
    }

    /// <summary>
    /// Executes the configured method on <paramref name="target"/>. This can only be used if the configured
    /// method is asynchronous.
    /// </summary>
    /// <remarks>
    /// If you don't know at compile time the type of the method's returned awaitable, you can use ExecuteAsync,
    /// which supplies an awaitable-of-object. This always works, but can incur several extra heap allocations
    /// as compared with using Execute and then using "await" on the result value typecasted to the known
    /// awaitable type. The possible extra heap allocations are for:
    ///
    /// 1. The custom awaitable (though usually there's a heap allocation for this anyway, since normally
    ///    it's a reference type, and you normally create a new instance per call).
    /// 2. The custom awaiter (whether or not it's a value type, since if it's not, you need a new instance
    ///    of it, and if it is, it will have to be boxed so the calling code can reference it as an object).
    /// 3. The async result value, if it's a value type (it has to be boxed as an object, since the calling
    ///    code doesn't know what type it's going to be).
    /// </remarks>
    /// <param name="target">The object whose method is to be executed.</param>
    /// <param name="parameters">Parameters to pass to the method.</param>
    /// <returns>An object that you can "await" to get the method return value.</returns>
    public ObjectMethodExecutorAwaitable ExecuteAsync(object target, object?[]? parameters)
    {
        Debug.Assert(_executorAsync != null, "Async execution is not supported.");
        return _executorAsync(target, parameters);
    }

    public object? GetDefaultValueForParameter(int index)
    {
        if (_parameterDefaultValues == null)
        {
            throw new InvalidOperationException($"Cannot call {nameof(GetDefaultValueForParameter)}, because no parameter default values were supplied.");
        }

        if (index < 0 || index > MethodParameters.Length - 1)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _parameterDefaultValues[index];
    }

    private static MethodExecutor GetExecutor(MethodInfo methodInfo, TypeInfo targetTypeInfo)
    {
        // Parameters to executor
        var targetParameter = Expression.Parameter(typeof(object), "target");
        var parametersParameter = Expression.Parameter(typeof(object?[]), "parameters");

        // Build parameter list
        var paramInfos = methodInfo.GetParameters();
        var parameters = new List<Expression>(paramInfos.Length);
        for (int i = 0; i < paramInfos.Length; i++)
        {
            var paramInfo = paramInfos[i];
            var valueObj = Expression.ArrayIndex(parametersParameter, Expression.Constant(i));
            var valueCast = Expression.Convert(valueObj, paramInfo.ParameterType);

            // valueCast is "(Ti) parameters[i]"
            parameters.Add(valueCast);
        }

        // Call method
        var instanceCast = Expression.Convert(targetParameter, targetTypeInfo.AsType());
        var methodCall = Expression.Call(instanceCast, methodInfo, parameters);

        // methodCall is "((Ttarget) target) method((T0) parameters[0], (T1) parameters[1], ...)"
        // Create function
        if (methodCall.Type == typeof(void))
        {
            var lambda = Expression.Lambda<VoidMethodExecutor>(methodCall, targetParameter, parametersParameter);
            var voidExecutor = lambda.Compile();
            return WrapVoidMethod(voidExecutor);
        }
        else
        {
            // must coerce methodCall to match ActionExecutor signature
            var castMethodCall = Expression.Convert(methodCall, typeof(object));
            var lambda = Expression.Lambda<MethodExecutor>(castMethodCall, targetParameter, parametersParameter);
            return lambda.Compile();
        }
    }

    private static MethodExecutor WrapVoidMethod(VoidMethodExecutor executor)
    {
        return delegate (object target, object?[]? parameters)
        {
            executor(target, parameters);
            return null;
        };
    }

    private static MethodExecutorAsync GetExecutorAsync(
        MethodInfo methodInfo,
        TypeInfo targetTypeInfo,
        CoercedAwaitableInfo coercedAwaitableInfo)
    {
        // Parameters to executor
        var targetParameter = Expression.Parameter(typeof(object), "target");
        var parametersParameter = Expression.Parameter(typeof(object[]), "parameters");

        // Build parameter list
        var paramInfos = methodInfo.GetParameters();
        var parameters = new List<Expression>(paramInfos.Length);
        for (int i = 0; i < paramInfos.Length; i++)
        {
            var paramInfo = paramInfos[i];
            var valueObj = Expression.ArrayIndex(parametersParameter, Expression.Constant(i));
            var valueCast = Expression.Convert(valueObj, paramInfo.ParameterType);

            // valueCast is "(Ti) parameters[i]"
            parameters.Add(valueCast);
        }

        // Call method
        var instanceCast = Expression.Convert(targetParameter, targetTypeInfo.AsType());
        var methodCall = Expression.Call(instanceCast, methodInfo, parameters);

        // Using the method return value, construct an ObjectMethodExecutorAwaitable based on
        // the info we have about its implementation of the awaitable pattern. Note that all
        // the funcs/actions we construct here are precompiled, so that only one instance of
        // each is preserved throughout the lifetime of the ObjectMethodExecutor.

        // var getAwaiterFunc = (object awaitable) =>
        //     (object)((CustomAwaitableType)awaitable).GetAwaiter();
        var customAwaitableParam = Expression.Parameter(typeof(object), "awaitable");
        var awaitableInfo = coercedAwaitableInfo.AwaitableInfo;
        var postCoercionMethodReturnType = coercedAwaitableInfo.CoercerResultType ?? methodInfo.ReturnType;
        var getAwaiterFunc = Expression.Lambda<Func<object, object>>(
            Expression.Convert(
                Expression.Call(
                    Expression.Convert(customAwaitableParam, postCoercionMethodReturnType),
                    awaitableInfo.GetAwaiterMethod),
                typeof(object)),
            customAwaitableParam).Compile();

        // var isCompletedFunc = (object awaiter) =>
        //     ((CustomAwaiterType)awaiter).IsCompleted;
        var isCompletedParam = Expression.Parameter(typeof(object), "awaiter");
        var isCompletedFunc = Expression.Lambda<Func<object, bool>>(
            Expression.MakeMemberAccess(
                Expression.Convert(isCompletedParam, awaitableInfo.AwaiterType),
                awaitableInfo.AwaiterIsCompletedProperty),
            isCompletedParam).Compile();

        var getResultParam = Expression.Parameter(typeof(object), "awaiter");
        Func<object, object> getResultFunc;
        if (awaitableInfo.ResultType == typeof(void))
        {
            // var getResultFunc = (object awaiter) =>
            // {
            //     ((CustomAwaiterType)awaiter).GetResult(); // We need to invoke this to surface any exceptions
            //     return (object)null;
            // };
            getResultFunc = Expression.Lambda<Func<object, object>>(
                Expression.Block(
                    Expression.Call(
                        Expression.Convert(getResultParam, awaitableInfo.AwaiterType),
                        awaitableInfo.AwaiterGetResultMethod),
                    Expression.Constant(null)
                ),
                getResultParam).Compile();
        }
        else
        {
            // var getResultFunc = (object awaiter) =>
            //     (object)((CustomAwaiterType)awaiter).GetResult();
            getResultFunc = Expression.Lambda<Func<object, object>>(
                Expression.Convert(
                    Expression.Call(
                        Expression.Convert(getResultParam, awaitableInfo.AwaiterType),
                        awaitableInfo.AwaiterGetResultMethod),
                    typeof(object)),
                getResultParam).Compile();
        }

        // var onCompletedFunc = (object awaiter, Action continuation) => {
        //     ((CustomAwaiterType)awaiter).OnCompleted(continuation);
        // };
        var onCompletedParam1 = Expression.Parameter(typeof(object), "awaiter");
        var onCompletedParam2 = Expression.Parameter(typeof(Action), "continuation");
        var onCompletedFunc = Expression.Lambda<Action<object, Action>>(
            Expression.Call(
                Expression.Convert(onCompletedParam1, awaitableInfo.AwaiterType),
                awaitableInfo.AwaiterOnCompletedMethod,
                onCompletedParam2),
            onCompletedParam1,
            onCompletedParam2).Compile();

        Action<object, Action>? unsafeOnCompletedFunc = null;
        if (awaitableInfo.AwaiterUnsafeOnCompletedMethod != null)
        {
            // var unsafeOnCompletedFunc = (object awaiter, Action continuation) => {
            //     ((CustomAwaiterType)awaiter).UnsafeOnCompleted(continuation);
            // };
            var unsafeOnCompletedParam1 = Expression.Parameter(typeof(object), "awaiter");
            var unsafeOnCompletedParam2 = Expression.Parameter(typeof(Action), "continuation");
            unsafeOnCompletedFunc = Expression.Lambda<Action<object, Action>>(
                Expression.Call(
                    Expression.Convert(unsafeOnCompletedParam1, awaitableInfo.AwaiterType),
                    awaitableInfo.AwaiterUnsafeOnCompletedMethod,
                    unsafeOnCompletedParam2),
                unsafeOnCompletedParam1,
                unsafeOnCompletedParam2).Compile();
        }

        // If we need to pass the method call result through a coercer function to get an
        // awaitable, then do so.
        var coercedMethodCall = coercedAwaitableInfo.RequiresCoercion
            ? Expression.Invoke(coercedAwaitableInfo.CoercerExpression, methodCall)
            : (Expression)methodCall;

        // return new ObjectMethodExecutorAwaitable(
        //     (object)coercedMethodCall,
        //     getAwaiterFunc,
        //     isCompletedFunc,
        //     getResultFunc,
        //     onCompletedFunc,
        //     unsafeOnCompletedFunc);
        var returnValueExpression = Expression.New(
            _objectMethodExecutorAwaitableConstructor,
            Expression.Convert(coercedMethodCall, typeof(object)),
            Expression.Constant(getAwaiterFunc),
            Expression.Constant(isCompletedFunc),
            Expression.Constant(getResultFunc),
            Expression.Constant(onCompletedFunc),
            Expression.Constant(unsafeOnCompletedFunc, typeof(Action<object, Action>)));

        var lambda = Expression.Lambda<MethodExecutorAsync>(returnValueExpression, targetParameter, parametersParameter);
        return lambda.Compile();
    }
}
