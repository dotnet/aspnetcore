// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Xml;
using System.Xml.Serialization;
using TriageBuildFailures.Abstractions;

namespace TriageBuildFailures.TeamCity
{
    public class TeamCityTestOccurrence : ICITestOccurrence
    {
        [XmlAttribute("id")]
        public string _Id { get; set; }

        public string TestId
        {
            get
            {
                return _Id?.Split(',')[0].Split(':')[1];
            }
        }

        public string BuildId
        {
            get
            {
                var buildDescriptor = _Id?.Split(',')[1];
                return buildDescriptor.Split(':')[2].TrimEnd(')');
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
        public TeamCityTest Test { get; set; }
    }
}
