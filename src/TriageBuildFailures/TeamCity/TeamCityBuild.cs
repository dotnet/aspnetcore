// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace TriageBuildFailures.TeamCity
{
    public class TeamCityBuild
    {
        public static IDictionary<string, string> BuildNames { get; set; }

        [XmlAttribute("id")]
        public int Id { get; set; }

        public string BuildName
        {
            get
            {
                return BuildNames[BuildTypeID];
            }
        }

        [XmlAttribute("buildTypeId")]
        public string BuildTypeID { get; set; }

        public BuildType BuildType {
            get
            {
                return new BuildType {
                    Id = BuildTypeID,
                    Name = BuildName
                };
            }
        }

        public DateTimeOffset StartDate => TeamCityClientWrapper.ParseTCDateTime(StartDateString);

        [XmlElement(ElementName = "startDate")]
        public string StartDateString { get; set; }

        [XmlAttribute("status")]
        public BuildStatus Status { get; set; }

        [XmlAttribute("branchName")]
        public string BranchName { get; set; }

        [XmlAttribute("webUrl")]
        public string UrlString { get; set; }

        public Uri WebURL
        {
            get
            {
                return new Uri(UrlString);
            }
        }

        public string Key
        {
            get
            {
                return $"{BuildTypeID};{BranchName}";
            }
        }
    }
}
