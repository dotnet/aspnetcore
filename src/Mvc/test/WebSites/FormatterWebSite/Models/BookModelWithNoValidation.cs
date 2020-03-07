// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace FormatterWebSite.Models
{
    public class BookModelWithNoValidation
    {
        public Guid Id { get; set; }

        public string Title { get; set; }

        [JsonRequired]
        [DataMember(IsRequired = true)]
        public string ISBN { get; set; }
    }
}
