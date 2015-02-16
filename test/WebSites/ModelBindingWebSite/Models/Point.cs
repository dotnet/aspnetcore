// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace ModelBindingWebSite
{
    public struct Point
    {
        [Required]
        public int X { get; set; }

        [Required]
        public int Y { get; set; }
    }
}