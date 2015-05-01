// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace TagHelpersWebSite.Models
{
    public class WebsiteContext
    {
        public Version Version { get; set; }

        public int CopyrightYear { get; set; }

        public bool Approved { get; set; }

        public int TagsToShow { get; set; }
    }
}