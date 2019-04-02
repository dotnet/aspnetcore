// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using TriageBuildFailures.Abstractions;

namespace TriageBuildFailures.VSTS.Models
{
    public class VSTSRelease : ICIBuild
    {
        private readonly Release _release;

        public VSTSRelease(Release release)
        {
            _release = release;
        }

        public string Id => _release.Id;

        public Type CIType { get; set; } = typeof(VSTSReleaseClient);

        public string BuildTypeID => _release.ReleaseDefinition.Id;

        public string BuildName => _release.ReleaseDefinition.Name;

        public BuildStatus Status
        {
            get
            {
                if (_release.Environments.All(env => env.Status == EnvironmentStatus.Succeeded))
                {
                    return BuildStatus.SUCCESS;
                }
                else
                {
                    return BuildStatus.FAILURE;
                }
            }
        }

        public IEnumerable<ReleaseEnvironment> Environments => _release.Environments;

        public string Project => _release.ProjectReference.Id;

        public string Branch
        {
            get
            {
                var parts = _release.Name.Split('-');
                if (parts.Length == 3)
                {
                    return parts[1].Trim();
                }
                else
                {
                    return "unknown";
                }
            }
        }

        public DateTimeOffset StartDate => _release.CreatedOn;

        public Uri WebURL => _release._Links.Web.Href;

        public CIConfigBase GetCIConfig(Config config)
        {
            return config.VSTS;
        }
    }
}
