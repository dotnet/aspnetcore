// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace FormatterWebSite;

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
