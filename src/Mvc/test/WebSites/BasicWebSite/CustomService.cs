// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicWebSite.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite;

public interface ICustomService
{
    string Process();
}

public class OkCustomService : ICustomService
{
    public string Process() => "OK";
    public override string ToString() => Process();
}

public class BadCustomService : ICustomService
{
    public string Process() => "NOT OK";
    public override string ToString() => Process();
}

public class DefaultCustomService : ICustomService
{
    public string Process() => "DEFAULT";
    public override string ToString() => Process();
    public static DefaultCustomService Instance => new DefaultCustomService();
}
