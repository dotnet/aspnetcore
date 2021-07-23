// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
