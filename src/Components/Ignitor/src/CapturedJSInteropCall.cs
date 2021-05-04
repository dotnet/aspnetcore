// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Ignitor
{
    public class CapturedJSInteropCall
    {
        public CapturedJSInteropCall(int asyncHandle, string identifier, string argsJson, int resultType, long targetInstanceId)
        {
            AsyncHandle = asyncHandle;
            Identifier = identifier;
            ArgsJson = argsJson;
            ResultType = resultType;
            TargetInstanceId = targetInstanceId;
        }

        public int AsyncHandle { get; }
        public string Identifier { get; }
        public string ArgsJson { get; }
        public int ResultType { get; }
        public long TargetInstanceId { get; }
    }
}
