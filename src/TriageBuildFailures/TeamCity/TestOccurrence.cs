// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Xml;
using System.Xml.Serialization;

namespace TriageBuildFailures.TeamCity
{
    public class TestOccurrence
    {
        [XmlAttribute("id")]
        public string _Id { get; set; }

        public int TestId {
            get {
                return int.Parse(_Id?.Split(',')[0].Split(':')[1]);
            }
        }

        public int BuildId {
            get {
                var buildDescriptor = _Id?.Split(',')[1];
                return int.Parse(buildDescriptor.Split(':')[2].TrimEnd(')'));
            }
        }

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
}
