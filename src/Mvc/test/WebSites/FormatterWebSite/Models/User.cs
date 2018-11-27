// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace FormatterWebSite
{
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
}