// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace FormatterWebSite;

[KnownType(typeof(DerivedDummyClass))]
[XmlInclude(typeof(DerivedDummyClass))]
public class DummyClass
{
    public int SampleInt { get; set; }
}
