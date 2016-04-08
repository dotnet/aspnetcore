// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace HtmlGenerationWebSite.Models
{
    public class ViewModel
    {
        public int Integer { get; set; } = 23;

        public long? NullableLong { get; set; } = 24L;

        public TemplateModel Template { get; set; } = new SuperTemplateModel();
    }
}
