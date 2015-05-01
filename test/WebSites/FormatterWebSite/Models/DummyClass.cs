// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace FormatterWebSite
{
    [KnownType(typeof(DerivedDummyClass))]
    [XmlInclude(typeof(DerivedDummyClass))]
    public class DummyClass
    {
        public int SampleInt { get; set; }
    }
}