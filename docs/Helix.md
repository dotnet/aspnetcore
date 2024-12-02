# Helix testing in ASP.NET Core

Helix is the distributed test platform that we use to run tests.  We build a helix payload that contains the publish directory of every test project that we want to test
send a job with with this payload to a set of queues for the various combinations of OS that we want to test
for example: `Windows.10.Amd64.ClientRS4.VS2017.Open`, `OSX.1100.Amd64.Open`, `Ubuntu.1804.Amd64.Open`. Helix takes care of unzipping, running the job, and reporting results.

For more info about helix see: [SDK](https://github.com/dotnet/arcade/blob/main/src/Microsoft.DotNet.Helix/Sdk/Readme.md), [JobSender](https://github.com/dotnet/arcade/blob/main/src/Microsoft.DotNet.Helix/Sdk/Readme.md)

## Running helix tests locally

To run Helix tests for one particular test project:

``` powershell
.\eng\scripts\RunHelix.ps1 -Project path\mytestproject.csproj
```

This will restore, and then publish all the test projects including some bootstrapping scripts that will install the correct dotnet runtime/sdk before running the test assembly on the helix machine(s), and upload the job to helix.

## Overview of the helix usage in our pipelines

- Required queues: Windows10, OSX, Ubuntu1804
- Full queue matrix: Windows[10, 11], Ubuntu[1804, 2004], Debian11, AlmaLinux8, Arm64 (Win10, Debian11)
- The queues are defined in [Helix.Common.props](https://github.com/dotnet/aspnetcore/blob/main/eng/targets/Helix.Common.props)

[aspnetcore-ci](https://dev.azure.com/dnceng/public/_build?definitionId=278) runs non quarantined tests against the required helix queues as a required PR check and all builds on all branches.

[aspnetcore-helix-matrix](https://dev.azure.com/dnceng/public/_build?definitionId=837) runs non quarantined tests against all queues twice a day only on public main.

[aspnetcore-quarantined-pr](https://dev.azure.com/dnceng/public/_build?definitionId=869) runs only quarantined tests against the required queues on PRs and on main every 4 hours.

[aspnetcore-quarantined-tests](https://dev.azure.com/dnceng/public/_build?definitionId=331) runs only quarantined tests against all queues only on public main once a day at 11 PM.

You can always manually queue pipeline runs by clicking on the link to the pipeline -> Run Pipeline -> select your branch/tag and commit

## Checkin process expectations

- The normal PR process has aspnetcore-ci will ensure that the required queues are green.
- If your changes are likely to have cross platform impact that would affect more than the required queues, you should kick off a manual aspnetcore-helix-matrix pipeline run against your branch before merging your PR. Even though aspnetcore-helix-matrix is not a required checkin gate, if your changes break this pipeline, you must either immediately revert your changes, or quarantine the test, its never ok to leave this pipeline in a broken state.

## How do I look at the results of a helix run on Azure Pipelines?

The easiest way to look at a test failure is via the tests tab in azdo which now should show a summary of the errors and have attachments to the relevant console logs.

You can also drill down into the helix web apis if you take the HelixJobId from the Debug tab of a failing test, and the HelixWorkItemName and go to: `https://helix.dot.net/api/2019-06-17/jobs/<jobId>/workitems/<workitemname>` which will show you more urls you can drill into for more info.

An example of how to get the helix payload to inspect the contents of a test job more completely:

- Start at work item link: https://helix.dot.net/api/jobs/b1c333d0-1681-4140-9a36-ccc70c40a598/workitems?api-version=2019-06-17
- Remove `/workitems` to get jobs link: https://helix.dot.net/api/jobs/b1c333d0-1681-4140-9a36-ccc70c40a598?api-version=2019-06-17
- Click on DetailsUrl value: https://helix.dot.net/api/jobs/b1c333d0-1681-4140-9a36-ccc70c40a598/details?api-version=2019-06-17 (yes, 1 and 2 can be done in one go)
- Click on JobsList: https://helixde8s23ayyeko0k025g8.blob.core.windows.net/helix-job-2e4bec2b-d34f-44a7-976b-aab2c3f237f8e2112b024574d8c8c/job-list-068e8195-20ca-47cb-98de-4be39e9ecc22.json?sv=2019-07-07 (truncated)
- However that JSON opens, click on one of the PayloadUrl values e.g. https://helixde8s23ayyeko0k025g8.blob.core.windows.net/helix-job-2e4bec2b-d34f-44a7-976b-aab2c3f237f8e2112b024574d8c8c/26267e9c-6a79-4fb9-81ef-8c6e1f5a68a6.zip?sv=2019-07-07 (truncated)

There's also a link embedded in the build.cmd log of the Tests: Helix x64 job on Azure Pipelines, near the bottom right that will look something like this:

```text
  Sending Job to Ubuntu.1804.Amd64.Open...
  Sent Helix Job; see work items at https://helix.dot.net/api/jobs/c1b425c8-0fef-4cba-9dee-29344d7a61b8/workitems?api-version=2019-06-17
  Sending Job to Windows.11.Amd64.ClientPre.Open...
  Sent Helix Job; see work items at https://helix.dot.net/api/jobs/1fc117ce-d52a-4ea4-8896-3c289fdf8e17/workitems?api-version=2019-06-17
  Sending Job to OSX.1014.Amd64.Open...
  Sent Helix Job; see work items at https://helix.dot.net/api/jobs/53e2ca23-9efd-4299-8a8f-d9271265aeaa/workitems?api-version=2019-06-17
  Waiting for completion of job 1fc117ce-d52a-4ea4-8896-3c289fdf8e17 on Windows.11.Amd64.ClientPre.Open
  Waiting for completion of job c1b425c8-0fef-4cba-9dee-29344d7a61b8 on Ubuntu.1804.Amd64.Open
  Waiting for completion of job 53e2ca23-9efd-4299-8a8f-d9271265aeaa on OSX.1014.Amd64.Open
  Job 53e2ca23-9efd-4299-8a8f-d9271265aeaa on OSX.1014.Amd64.Open is completed with 139 finished work items.
  Job c1b425c8-0fef-4cba-9dee-29344d7a61b8 on Ubuntu.1804.Amd64.Open is completed with 138 finished work items.
  Job 1fc117ce-d52a-4ea4-8896-3c289fdf8e17 on Windows.11.Amd64.ClientPre.Open is completed with 170 finished work items.
  Stopping Azure Pipelines Test Run Ubuntu.1804.Amd64.Open
  Stopping Azure Pipelines Test Run Windows.11.Amd64.ClientPre.Open
  Stopping Azure Pipelines Test Run OSX.1014.Amd64.Open
D:\a\_work\1\s\.packages\microsoft.dotnet.helix.sdk\7.0.0-beta.21559.3\tools\Microsoft.DotNet.Helix.Sdk.MultiQueue.targets(78,5): error : Work item Microsoft.AspNetCore.Identity.Test--net8.0 in job 53e2ca23-9efd-4299-8a8f-d9271265aeaa has failed. [D:\a\_work\1\s\eng\helix\helix.proj]
D:\a\_work\1\s\.packages\microsoft.dotnet.helix.sdk\7.0.0-beta.21559.3\tools\Microsoft.DotNet.Helix.Sdk.MultiQueue.targets(78,5): error : Failure log: https://helix.dot.net/api/2019-06-17/jobs/53e2ca23-9efd-4299-8a8f-d9271265aeaa/workitems/Microsoft.AspNetCore.Identity.Test--net8.0/console [D:\a\_work\1\s\eng\helix\helix.proj]
##[error].packages\microsoft.dotnet.helix.sdk\7.0.0-beta.21559.3\tools\Microsoft.DotNet.Helix.Sdk.MultiQueue.targets(78,5): error : (NETCORE_ENGINEERING_TELEMETRY=Test) Work item Microsoft.AspNetCore.Identity.Test--net8.0 in job 53e2ca23-9efd-4299-8a8f-d9271265aeaa has failed.
Failure log: https://helix.dot.net/api/2019-06-17/jobs/53e2ca23-9efd-4299-8a8f-d9271265aeaa/workitems/Microsoft.AspNetCore.Identity.Test--net8.0/console
```

The https://helix.dot.net/ home page displays information about the available public queues (nothing about the related BYOC pools and queues or the internal Helix queues)

Some superficial information about both BYOC and Helix agents is available at https://github.com/dotnet/arcade/blob/8ca46105193bd25c95af49bc6cd3604aaefec980/Documentation/AzureDevOps/AzureDevOpsOnboarding.md#agent-queues

More detailed and always up-to-date information about all of the agents is available at https://helix.dot.net/api/2018-03-14/info/queues

## What do I do if a test fails?

You can simulate how most tests run locally:

``` powershell
dotnet publish
cd <the publish directory>
dotnet vstest My.Tests.dll
```

## Differences from running tests locally

Most tests that don't just work on helix automatically are ones that depend on the source code being accessible. The helix payloads only contain whatever is in the publish directories, so anything else that test depends on will need to be included to the payload.

This can be accomplished by using the `HelixContent` property like so.

``` msbuild
<ItemGroup>
  <HelixContent Include="$(RepoRoot)src\KeepMe.js"/>
  <HelixContent Include="$(RepoRoot)src\Project\**"/>
</ItemGroup>
```

By default, these files will be included in the root directory. To include these files in a different directory, you can use either the `Link` or `LinkBase` attributes to set the included path.

``` msbuild
<ItemGroup>
  <HelixContent Include="$(RepoRoot)src\KeepMe.js" Link="$(MSBuildThisFileDirectory)\myassets\KeepMe.js"/>
  <HelixContent Include="$(RepoRoot)src\Project\**" LinkBase="$(MSBuildThisFileDirectory)\myassets"/>
</ItemGroup>
```

## How to skip tests on helix

There are two main ways to opt out of helix

- Skipping the entire test project via `<BuildHelixPayload>false</BuildHelixPayload>` in csproj (the default value for this is IsTestProject).
- Skipping an individual test via `[SkipOnHelix("url to github issue")]`.

Make sure to file an issue for any skipped tests and include that in a comment next to either of these

## Process for updating helix matrix

The goal is to balance cost/flakiness against having some coverage of supported distros:
- At the start of each product version, we pick a set of queues/versions/arches to run based on popularity and perceived risk, and how long is left in the support for that OS version.
- Whenever a new OS is coming online, we ask CTI to do a run on it, and if there is support for it in helix, we submit a PR to update our helix-matrix to include it for it to check for any failures in it, but if there aren’t any, we don’t merge it.
    - If an appropriate queue does not yet exist, we could submit a PR to https://github.com/dotnet/dotnet-buildtools-prereqs-docker to add it. This helps even if we do not plan to keep the dotnet/aspnetcore change around.
- Link to [OS support calendar](https://dev.azure.com/devdiv/DevDiv/_wiki/wikis/DevDiv.wiki/12624/OS-Version-Management-Calendar-2021)
- Link to [current list of queues](../eng/targets/Helix.Common.props)

## Example of adding a new docker image to helix

- Example PR: dotnet/dotnet-buildtools-prereqs-docker#398
- Summary is to update [manifest.json](https://github.com/dotnet/dotnet-buildtools-prereqs-docker/blob/main/manifest.json) with an entry for the new dockerfiles, and then add the docker files as well to dotnet-buildtools-prereqs-docker
- The resulting new docker queue id will be found in: [image-info.dotnet-dotnet-buildtools-prereqs-docker-main.json](https://github.com/dotnet/versions/blob/main/build-info/docker/image-info.dotnet-dotnet-buildtools-prereqs-docker-main.json)

## Investigating helix run time issues

Kusto has all of the helix job data, using a particular job id, with the following query you can get a breakdown of the test projects that take the longest. Ideally to take advantage of the largest fan out, we want smaller test projects since the longest running test project will be the gate for finishing the entire helix test job.

https://dataexplorer.azure.com/clusters/engsrvprod/databases/engineeringdata

```text
WorkItems
| where JobName == "bc108374-750c-4084-853e-bc5b9b0d553e"
| where Name != JobName
| extend RunTime = Finished-Started
| top 20 by RunTime desc
| project FriendlyName, RunTime
```

![image](https://user-images.githubusercontent.com/6537861/144129895-e1e82815-4192-431c-a7b3-dd055b813978.png)
