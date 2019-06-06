// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR
{
    public interface IInvocationBinder
    {
        Type GetReturnType(string invocationId);
        IReadOnlyList<Type> GetParameterTypes(string methodName);
        Type GetStreamItemType(string streamId);
    }
}
