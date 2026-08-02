// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Microsoft.Extensions.Internal;

internal readonly struct CoercedAwaitableInfo
{
    public AwaitableInfo AwaitableInfo { get; }
    public Expression CoercerExpression { get; }
    public Type CoercerResultType { get; }
    public bool RequiresCoercion => CoercerExpression != null;

    public CoercedAwaitableInfo(AwaitableInfo awaitableInfo)
    {
        AwaitableInfo = awaitableInfo;
        CoercerExpression = null;
        CoercerResultType = null;
    }

    public CoercedAwaitableInfo(Expression coercerExpression, Type coercerResultType, AwaitableInfo coercedAwaitableInfo)
    {
        CoercerExpression = coercerExpression;
        CoercerResultType = coercerResultType;
        AwaitableInfo = coercedAwaitableInfo;
    }

    [RequiresUnreferencedCode(AwaitableInfo.RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode("Dynamically generates calls to FSharpAsync.")]
    public static bool IsTypeAwaitable(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type,
        out CoercedAwaitableInfo info)
    {
        if (AwaitableInfo.IsTypeAwaitable(type, out var directlyAwaitableInfo))
        {
            // Convert {Value}Task<unit> to non-generic {Value}Task.
            if (ObjectMethodExecutorFSharpSupport.TryBuildCoercerFromUnitAwaitableToVoidAwaitable(type,
                out var coercerExpression,
                out var nonGenericAwaitableType))
            {
                _ = AwaitableInfo.IsTypeAwaitable(nonGenericAwaitableType, out directlyAwaitableInfo);
                info = new CoercedAwaitableInfo(coercerExpression, nonGenericAwaitableType, directlyAwaitableInfo);
            }
            else
            {
                info = new CoercedAwaitableInfo(directlyAwaitableInfo);
            }

            return true;
        }
        else
        {
            // It's not directly awaitable, but maybe we can coerce it.
            // Currently we support coercing FSharpAsync<T>.
            if (ObjectMethodExecutorFSharpSupport.TryBuildCoercerFromFSharpAsyncToAwaitable(type,
                out var coercerExpression,
                out var coercerResultType))
            {
                if (AwaitableInfo.IsTypeAwaitable(coercerResultType, out var coercedAwaitableInfo))
                {
                    info = new CoercedAwaitableInfo(coercerExpression, coercerResultType, coercedAwaitableInfo);
                    return true;
                }
            }

            info = default(CoercedAwaitableInfo);
            return false;
        }
    }
}
