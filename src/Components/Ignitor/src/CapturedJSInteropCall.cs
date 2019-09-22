// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Ignitor
{
    public class CapturedJSInteropCall
    {
        public CapturedJSInteropCall(int asyncHandle, string identifier, string argsJson)
        {
            AsyncHandle = asyncHandle;
            Identifier = identifier;
            ArgsJson = argsJson;
        }

        public int AsyncHandle { get; }
        public string Identifier { get; }
        public string ArgsJson { get; }
    }
}
