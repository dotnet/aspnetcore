// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace FormatterWebSite.Models;

public class BookModelWithNoValidation
{
    public Guid Id { get; set; }

    public string Title { get; set; }

    [JsonRequired]
    [DataMember(IsRequired = true)]
    public string ISBN { get; set; }
}
