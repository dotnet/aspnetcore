// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace HtmlGenerationWebSite.Models
{
    public class WeirdModel
    {
        public string Field = "Hello, Field World!";

        public static string StaticProperty { get; set; } = "Hello, Static World!";
    }
}