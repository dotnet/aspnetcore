// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Identity.OpenIdConnect.WebSite.Identity.Models.ApplicationViewModels
{
    public class RemoveApplicationViewModel
    {
        public RemoveApplicationViewModel(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
