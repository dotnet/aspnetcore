// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace TeamCityApi
{
    [XmlRoot("testOccurrences")]
    public class TestOccurrences
    {
        [XmlElement("testOccurrence")]
        public List<TestOccurrence> TestList;
    }

    public class TestOccurrence
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        public string BuildTypeId { get; set; }

        [XmlAttribute("status")]
        public BuildStatus Status { get; set; }

        [XmlAttribute("duration")]
        public int Duration { get; set; }

        [XmlAttribute("ignored")]
        public bool Ignored { get; set; } = false;

        [XmlElement("test")]
        public Test Test { get; set; }
    }

    public class Test
    {
        [XmlAttribute("id")]
        public string ID { get; set; }
    }
}
