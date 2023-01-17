// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.SignalR.Internal;

internal sealed class HubMethodDescriptor
{
    private static readonly MethodInfo MakeCancelableAsyncEnumeratorMethod = typeof(AsyncEnumerableAdapters)
        .GetRuntimeMethods()
        .Single(m => m.Name.Equals(nameof(AsyncEnumerableAdapters.MakeCancelableAsyncEnumerator)) && m.IsGenericMethod);

    private static readonly MethodInfo MakeAsyncEnumeratorFromChannelMethod = typeof(AsyncEnumerableAdapters)
        .GetRuntimeMethods()
        .Single(m => m.Name.Equals(nameof(AsyncEnumerableAdapters.MakeAsyncEnumeratorFromChannel)) && m.IsGenericMethod);

    private readonly MethodInfo? _makeCancelableEnumeratorMethodInfo;
    private Func<object, CancellationToken, IAsyncEnumerator<object>>? _makeCancelableEnumerator;
    // bitset to store which parameters come from DI up to 64 arguments
    private ulong _isServiceArgument;

    public HubMethodDescriptor(ObjectMethodExecutor methodExecutor, IServiceProviderIsService? serviceProviderIsService, IEnumerable<IAuthorizeData> policies)
    {
        MethodExecutor = methodExecutor;

        NonAsyncReturnType = (MethodExecutor.IsMethodAsync)
            ? MethodExecutor.AsyncResultType!
            : MethodExecutor.MethodReturnType;

        foreach (var returnType in NonAsyncReturnType.GetInterfaces().Concat(NonAsyncReturnType.AllBaseTypes()))
        {
            if (!returnType.IsGenericType)
            {
                continue;
            }

            var openReturnType = returnType.GetGenericTypeDefinition();

            if (openReturnType == typeof(IAsyncEnumerable<>))
            {
                StreamReturnType = returnType.GetGenericArguments()[0];
                _makeCancelableEnumeratorMethodInfo = MakeCancelableAsyncEnumeratorMethod;
                break;
            }

            if (openReturnType == typeof(ChannelReader<>))
            {
                StreamReturnType = returnType.GetGenericArguments()[0];
                _makeCancelableEnumeratorMethodInfo = MakeAsyncEnumeratorFromChannelMethod;
                break;
            }
        }

        // Take out synthetic arguments that will be provided by the server, this list will be given to the protocol parsers
        ParameterTypes = methodExecutor.MethodParameters.Where((p, index) =>
        {
            // Only streams can take CancellationTokens currently
            if (IsStreamResponse && p.ParameterType == typeof(CancellationToken))
            {
                HasSyntheticArguments = true;
                return false;
            }
            else if (ReflectionHelper.IsStreamingType(p.ParameterType, mustBeDirectType: true))
            {
                if (StreamingParameters == null)
                {
                    StreamingParameters = new List<Type>();
                }

                StreamingParameters.Add(p.ParameterType.GetGenericArguments()[0]);
                HasSyntheticArguments = true;
                return false;
            }
            else if (p.CustomAttributes.Any(a => typeof(IFromServiceMetadata).IsAssignableFrom(a.AttributeType)) ||
                serviceProviderIsService?.IsService(GetServiceType(p.ParameterType)) == true)
            {
                if (index >= 64)
                {
                    throw new InvalidOperationException(
                        "Hub methods can't use services from DI in the parameters after the 64th parameter.");
                }
                _isServiceArgument |= (1UL << index);
                HasSyntheticArguments = true;
                return false;
            }
            return true;
        }).Select(p => p.ParameterType).ToArray();

        if (HasSyntheticArguments)
        {
            OriginalParameterTypes = methodExecutor.MethodParameters.Select(p => p.ParameterType).ToArray();
        }

        Policies = policies.ToArray();
    }

    public List<Type>? StreamingParameters { get; private set; }

    public ObjectMethodExecutor MethodExecutor { get; }

    public IReadOnlyList<Type> ParameterTypes { get; }

    public IReadOnlyList<Type>? OriginalParameterTypes { get; }

    public Type NonAsyncReturnType { get; }

    public bool IsStreamResponse => StreamReturnType != null;

    public Type? StreamReturnType { get; }

    public IList<IAuthorizeData> Policies { get; }

    public bool HasSyntheticArguments { get; private set; }

    public bool IsServiceArgument(int argumentIndex)
    {
        return (_isServiceArgument & (1UL << argumentIndex)) != 0;
    }

    public IAsyncEnumerator<object> FromReturnedStream(object stream, CancellationToken cancellationToken)
    {
        // there is the potential for compile to be called times but this has no harmful effect other than perf
        if (_makeCancelableEnumerator == null)
        {
            _makeCancelableEnumerator = CompileConvertToEnumerator(_makeCancelableEnumeratorMethodInfo!, StreamReturnType!);
        }

        return _makeCancelableEnumerator.Invoke(stream, cancellationToken);
    }

    private static Func<object, CancellationToken, IAsyncEnumerator<object>> CompileConvertToEnumerator(MethodInfo adapterMethodInfo, Type streamReturnType)
    {
        // This will call one of two adapter methods to wrap the passed in streamable value into an IAsyncEnumerable<object>:
        // - AsyncEnumerableAdapters.MakeCancelableAsyncEnumerator<T>(asyncEnumerable, cancellationToken);
        // - AsyncEnumerableAdapters.MakeCancelableAsyncEnumeratorFromChannel<T>(channelReader, cancellationToken);

        var parameters = new[]
        {
                Expression.Parameter(typeof(object)),
                Expression.Parameter(typeof(CancellationToken)),
            };

        var genericMethodInfo = adapterMethodInfo.MakeGenericMethod(streamReturnType);
        var methodParameters = genericMethodInfo.GetParameters();
        var methodArguments = new Expression[]
        {
                Expression.Convert(parameters[0], methodParameters[0].ParameterType),
                parameters[1],
        };

        var methodCall = Expression.Call(null, genericMethodInfo, methodArguments);
        var lambda = Expression.Lambda<Func<object, CancellationToken, IAsyncEnumerator<object>>>(methodCall, parameters);
        return lambda.Compile();
    }

    private static Type GetServiceType(Type type)
    {
        // IServiceProviderIsService will special case IEnumerable<> and always return true
        // so, in this case checking the element type instead
        if (type.IsConstructedGenericType &&
            type.GetGenericTypeDefinition() is Type genericDefinition &&
            genericDefinition == typeof(IEnumerable<>))
        {
            return type.GenericTypeArguments[0];
        }

        return type;
    }
}
