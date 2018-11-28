// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace HtmlGenerationWebSite.Models
{
    public class AClass
    {
        public DayOfWeek DayOfWeek { get; set; }
        [DisplayFormat(DataFormatString = "Month: {0}")]
        public Month Month { get; set; }
    }
}
