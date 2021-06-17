// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace BasicTestApp.InteropTest
{
    public class JSDataReferenceInterop
    {
        [JSInvokable]
        public static Task<IJSDataReference> RoundTripJSDataReferenceAsync(IJSDataReference jsDataReference)
        {
            return Task.FromResult(jsDataReference);
        }

        [JSInvokable]
        public static Task<JSDataReferenceWrapper> RoundTripJSDataReferenceWrapperObjectAsync(JSDataReferenceWrapper jsDataReferenceWrapper)
        {
            return Task.FromResult(jsDataReferenceWrapper);
        }

        public class JSDataReferenceWrapper
        {
            public string StrVal { get; set; }
            public IJSDataReference JSDataReferenceVal { get; set; }
            public int IntVal { get; set; }

            public override string ToString()
            {
                return $"StrVal: {StrVal}, IntVal: {IntVal}, JSDataReferenceVal: {string.Join(',', JSDataReferenceVal)}";
            }
        }
    }
}
