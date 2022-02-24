// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace BasicTestApp.ServerReliability;

public class JSInterop
{
    [JSInvokable]
    public static DotNetObjectReference<ImportantInformation> CreateImportant()
    {
        return DotNetObjectReference.Create(new ImportantInformation());
    }

    [JSInvokable]
    public static string ReceiveTrivial(DotNetObjectReference<TrivialInformation> information)
    {
        return information.Value.Message;
    }
}

public class ImportantInformation
{
    public string Message { get; set; } = "Important";

    [JSInvokable]
    public string Reverse()
    {
        var messageChars = Message.ToCharArray();
        Array.Reverse(messageChars);
        return new string(messageChars);
    }
}

public class TrivialInformation
{
    public string Message { get; set; } = "Trivial";
}
