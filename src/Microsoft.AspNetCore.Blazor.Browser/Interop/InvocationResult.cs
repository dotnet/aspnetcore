// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Blazor.Browser.Interop
{
    internal class InvocationResult<TRes>
    {
        // Whether the method call succeeded or threw an exception.
        public bool Succeeded { get; set; }

        // The result of the method call if any.
        public TRes Result { get; set; }

        // The message from the captured exception in case there was an error.
        public string Message { get; set; }

        public static string Success(TRes result) =>
            JsonUtil.Serialize(new InvocationResult<TRes> { Result = result, Succeeded = true });

        public static string Fail(Exception exception) =>
            JsonUtil.Serialize(new InvocationResult<object> { Message = exception.Message, Succeeded = false });
    }
}
