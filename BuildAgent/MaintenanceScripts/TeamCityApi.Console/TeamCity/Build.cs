// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Xml.Serialization;

namespace TeamCityApi
{
    [XmlRoot("builds")]
    public class Builds
    {
        [XmlElement("build")]
        public List<Build> BuildList;
    }


    public class Build
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
