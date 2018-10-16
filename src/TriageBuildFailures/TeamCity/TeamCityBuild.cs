// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using TriageBuildFailures.Abstractions;

namespace TriageBuildFailures.TeamCity
{
    public class TeamCityBuild : ICIBuild
    {
        public static Task<IDictionary<string, string>> BuildNames { get; set; }

        public CIConfigBase GetCIConfig(Config config)
        {
            return config.TeamCity;
        }

        public Type CIType => typeof(TeamCityClientWrapper);
        [XmlAttribute("id")]
        public string Id { get; set; }

        public string BuildName
        {
            get
            {
                return BuildNames.Result[BuildTypeID];
            }
        }

        [XmlAttribute("buildTypeId")]
        public string BuildTypeID { get; set; }

        public BuildType BuildType
        {
            get
            {
                return new BuildType
                {
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
        public string Branch { get; set; }

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
                return $"{BuildTypeID};{Branch}";
            }
        }
    }
}
