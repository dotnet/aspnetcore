// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace ModelBindingWebSite.Models
{
   public class Document
    {
        [FromNonExistantBinder]
        public string Version { get; set; }

        [FromNonExistantBinder]
        public Document SubDocument { get; set; }
    }
}