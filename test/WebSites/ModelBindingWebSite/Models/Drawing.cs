// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ModelBindingWebSite
{
    public class Drawing
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public List<Line> Lines { get; set; }
    }
}