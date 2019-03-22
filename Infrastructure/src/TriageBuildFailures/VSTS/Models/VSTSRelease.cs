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

    public class ThinRelease
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ReleaseStatus Status { get; set; }
        public ReleaseDefinition ReleaseDefinition { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
        public Links _Links { get; set; }
        public VSTSProject ProjectReference { get; set; }
    }

    public class Release : ThinRelease
    {
        public IEnumerable<ReleaseEnvironment> Environments { get; set; }
    }

    public class ReleaseDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class ReleaseEnvironment
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public EnvironmentStatus Status { get; set; }
        public IEnumerable<DeployStep> DeploySteps { get; set; }
    }

    public class DeployStep
    {
        public string Id { get; set; }
        public string DeploymentId { get; set; }
        public int Attempt { get; set; }
        public DeployStatus Status { get; set; }
        public IEnumerable<DeployPhase> ReleaseDeployPhases { get; set; }
    }

    public class DeployPhase
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Rank { get; set; }
        public PhaseType PhaseType { get; set; }
        public DeployPhaseStatus Status { get; set; }
        public string RunPlanId { get; set; }
        public IEnumerable<DeploymentJobItem> DeploymentJobs { get; set; }
        public DateTime StartedOn { get; set; }
    }

    public class DeploymentJobItem
    {
        public DeploymentJob Job { get; set; }
        public IEnumerable<DeploymentTask> Tasks { get; set; }
    }

    public class DeploymentTask
    {
        public string Id { get; set; }
        public string TimelineRecordId { get; set; }
        public string Name { get; set; }
        public DateTime DateStarted { get; set; }
        public DeployTaskStatus Status { get; set; }
        public int? Rank { get; set; }
        public IEnumerable<VSTSIssue> Issues { get; set; }
        public string AgentName { get; set; }
        public string LogUrl { get; set; }
        public Uri LogUri => !string.IsNullOrEmpty(LogUrl) ? new Uri(LogUrl) : null;
    }

    public class DeploymentJob
    {
        public string Id { get; set; }
        public string TimelineRecordId { get; set; }
        public string Name { get; set; }
        public DateTime DateStarted { get; set; }
        public DateTime DateEnded { get; set; }
        public BuildStatus TaskStatus { get; set; }
        public int Rank { get; set; }
        public IEnumerable<VSTSIssue> Issues { get; set; }
        public string AgentName { get; set; }
        public string LogUrl { get; set; }
        public Uri LogUri => new Uri(LogUrl);
    }

    public class VSTSIssue
    {
        public string IssueType { get; set; }
        public string Message { get; set; }
    }

    public enum DeployTaskStatus
    {
        Canceled,
        Failed,
        Failure,
        InProgress,
        PartiallySucceeded,
        Pending,
        Skipped,
        Succeeded,
        Success,
        Unknown
    }

    public enum DeployPhaseStatus
    {
        Canceled,
        Cancelling,
        Failed,
        InProgress,
        NotStarted,
        PartiallySucceeded,
        Skipped,
        Succeeded,
        Undefined
    }

    public enum DeployStatus
    {
        All,
        Failed,
        InProgress,
        NotDeployed,
        PartiallySucceeded,
        Succeeded,
        Undefined
    }

    public enum PhaseType
    {
        AgentBasedDeployment,
        DeploymentGates,
        MachineGroupBasedDeployment,
        RunOnServer,
        Undefined
    }

    public enum EnvironmentStatus
    {
        Canceled,
        InProgress,
        NotStarted,
        PartiallySucceeded,
        Queued,
        Rejected,
        Scheduled,
        Succeeded,
        Undefined
    }

    public enum ReleaseStatus
    {
        Abandoned,
        Active,
        Draft,
        Undefined
    }
}
