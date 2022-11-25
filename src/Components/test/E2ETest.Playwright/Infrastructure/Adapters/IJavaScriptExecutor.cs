// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OpenQA.Selenium;

public interface IJavaScriptExecutor
{
    object ExecuteScript(string script);
}
