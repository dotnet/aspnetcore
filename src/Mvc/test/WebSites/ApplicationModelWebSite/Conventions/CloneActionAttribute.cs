// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ApplicationModelWebSite;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class CloneActionAttribute : Attribute
{
    public CloneActionAttribute(string newActionName)
    {
        ActionName = newActionName;
    }

    public string ActionName { get; private set; }
}
