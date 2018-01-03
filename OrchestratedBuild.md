Orchestrated Build
==================

The Orchestrated Build is a build triggered by the PipeBuild orchestrator. PipeBuild orchestrates queuing builds and passing variables between builds of dotnet/coreclr, dotnet/corefx, dotnet/roslyn, aspnet/universe, and more.
When building for Orchestrated Build, the versions of depenendencies, runtimes, tools, etc. may be configured to pull from the currently running build chain, and not the versions defined in source code.

## Queueing a new build

PipeBuild runs from VSTS. https://devdiv.visualstudio.com/DevDiv/_build/index?definitionId=7679&_a=completed

It triggers a build on ASP.NET CI (TeamCity) via REST API. Builds that run will be marked as "triggered by dn-bot".

A new build can be queued using the following parameters on VSTS.

> Queueing one with the default params will just  run the full stack.  
>
> If you want to rerun from ASP.NET, you can set:
> - PB_PipelineFile = pipelines.orchestrated-full.json
> - ProductBuildId = \<build id of previous build that has inputs for ASP.NET\>

## Help

Mail: [dncprodcon@microsoft.com](mailto:dncprodcon@microsoft.com)

Slack: #dotnet-prodcon in [microsoft.slack.com](https://microsoft.slack.com/messages/C7Q4W4NHK/)
