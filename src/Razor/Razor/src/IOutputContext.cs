// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Razor
{
    public interface IOutputContext
    {
        TextWriter Writer { get; set; }
        dynamic ViewBag { get; }
        Task FlushAsync();
    }
}