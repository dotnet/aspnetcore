// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace FormatterWebSite;

[DataContract]
public class User
{
    [DataMember(IsRequired = true)]
    [Required, Range(1, 2000)]
    public int Id { get; set; }

    [DataMember(IsRequired = true)]
    [Required, MinLength(5)]
    public string Name { get; set; }

    [DataMember]
    [StringLength(15, MinimumLength = 3)]
    public string Alias { get; set; }

    [DataMember]
    [RegularExpression("[0-9a-zA-Z]*")]
    public string Designation { get; set; }

    [DataMember]
    public string description { get; set; }
}
