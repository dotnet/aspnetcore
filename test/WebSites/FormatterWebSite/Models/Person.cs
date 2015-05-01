// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.Serialization;

namespace FormatterWebSite
{
    [DataContract]
    public class Person
    {
        public Person(string name)
        {
            Name = name;
        }

        [DataMember]
        public string Name { get; set; }
    }
}