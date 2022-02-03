// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace IdentitySample.Services;

public interface ISmsSender
{
    Task SendSmsAsync(string number, string message);
}
