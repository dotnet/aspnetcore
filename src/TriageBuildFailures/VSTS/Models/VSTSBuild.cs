// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using TriageBuildFailures.Abstractions;

namespace TriageBuildFailures.VSTS.Models
{
    public class VSTSBuild : ICIBuild
    {
        private Build _build;

        public VSTSBuild(Build build)
        {
            _build = build;
        }

        public Type CIType => typeof(VSTSClient);

        public string Id => _build.Id;

        public string BuildTypeID => _build.Definition.Id;

        public string BuildName => _build.Definition.Name;

        public BuildStatus Status
        {
            get
            {
                switch (_build.Result)
                {
                    case VSTSBuildResult.Canceled:
                        return BuildStatus.UNKNOWN;
                    case VSTSBuildResult.Failed:
                        return BuildStatus.FAILURE;
                    case VSTSBuildResult.Succeeded:
                        return BuildStatus.SUCCESS;
                    case VSTSBuildResult.PartiallySucceeded:
                    default:
                        throw new NotImplementedException($"VSTS had an unknown build result '{_build.Result}'!");
                }
            }
        }

        public string Project => _build.Project.Id;

        public string Branch => _build.SourceBranch.Replace("refs/heads/", string.Empty);

        public DateTimeOffset StartDate => new DateTimeOffset(_build.StartTime.Value);

        public Uri Uri => _build.Uri;

        public Uri WebURL => _build._Links.Web.Href;

        public CIConfigBase GetCIConfig(Config config)
        {
            return config.VSTS;
        }
    }

    public class Build
    {
        public string Id { get; set; }
        public BuildDefinition Definition { get; set; }
        public DateTime? StartTime { get; set; }
        public Uri Uri { get; set; }
        public string SourceBranch { get; set; }
        public VSTSProject Project { get; set; }
        public VSTSBuildResult Result { get; set; }
        public IDictionary<string, string> TriggerInfo { get; set; }

        public Links _Links { get; set; }
    }

    public class Links
    {
        public Link Self { get; set; }
        public Link Web { get; set; }
        public Link Badge { get; set; }
    }

    public class Link
    {
        public Uri Href { get; set; }
    }

    public class BuildDefinition
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Path { get; set; }
    }
}
