// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;

namespace Microsoft.Extensions.Internal
{
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

        public static bool IsTypeAwaitable(Type type, out CoercedAwaitableInfo info)
        {
            if (AwaitableInfo.IsTypeAwaitable(type, out var directlyAwaitableInfo))
            {
                info = new CoercedAwaitableInfo(directlyAwaitableInfo);
                return true;
            }

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
