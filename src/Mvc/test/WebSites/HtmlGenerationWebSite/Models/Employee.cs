// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace HtmlGenerationWebSite.Models;

public class Employee : Person
{
    public string Address
    {
        get;
        set;
    }

    public string OfficeNumber
    {
        get;
        set;
    }

    public bool Remote
    {
        get;
        set;
    }
}
