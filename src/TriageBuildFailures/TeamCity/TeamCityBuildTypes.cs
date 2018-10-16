// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace TriageBuildFailures.TeamCity
{
    [XmlRoot("buildTypes")]
    public class TeamCityBuildTypes
    {
        [XmlElement("buildType")]
        public List<BuildType> BuildTypeList { get; set; }
    }

    public class BuildType
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }
    }
}
