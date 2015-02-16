// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;

namespace ModelBindingWebSite
{
    public struct Line
    {
        [Required]
        public Point Start { get; set; }

        [Required]
        public Point End { get; set; }
    }
}