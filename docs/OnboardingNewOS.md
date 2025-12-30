# Onboarding New Operating System Versions

Guidance for onboarding new OS versions in ASP.NET, including how to add build & test coverage.

For broader context on our OS philosophy in .NET, see [Onboarding Guide for New Operating System Versions](https://github.com/dotnet/runtime/blob/main/docs/project/os-onboarding.md) in dotnet/runtime.

To see the list of our latest docker tags, see [this file](https://github.com/dotnet/versions/blob/main/build-info/docker/image-info.dotnet-dotnet-buildtools-prereqs-docker-main.json).

For the full set of docker tags, see [this file](https://mcr.microsoft.com/v2/dotnet-buildtools/prereqs/tags/list).

For info on modifying/adding new docker images, see [this doc](https://github.com/dotnet/dotnet-buildtools-prereqs-docker?tab=readme-ov-file#how-to-modify-or-create-a-new-image).

## Building on a new Operating System

On Mac and Linux, dotnet/aspnetcore does not build any native code - therefore we cross-compile all of our Linux bits on one docker image in CI. If you need to update the image that we use, simply follow the pattern from [this PR](https://github.com/dotnet/aspnetcore/pull/60260) to update the image we use in [ci.yml](https://github.com/dotnet/aspnetcore/blob/main/.azure/pipelines/ci.yml) and [ci-public.yml](https://github.com/dotnet/aspnetcore/blob/main/.azure/pipelines/ci-public.yml). Make sure to use one of the docker tags from the link above.

## Testing on a new Operating System

We run our tests in Helix on a variety of Operating Systems - the set of queues that we run tests on is listed in [Helix.Common.props](https://github.com/dotnet/aspnetcore/blob/main/eng/targets/Helix.Common.props). 

In order to update one of the queues we use, follow the example in [this PR](https://github.com/dotnet/aspnetcore/pull/54609) - if you change any property names, be sure to find and replace all instances of that property in the repo.

In order to add a new queue to the Helix matrix, add the new queue to the top of [Helix.Common.props](https://github.com/dotnet/aspnetcore/blob/8ee12ef7a2c179f3d7c7da5ab33d76d652042d0b/eng/targets/Helix.Common.props#L3-L9), and add it to the [list of queues we use in the Helix Matrix](https://github.com/dotnet/aspnetcore/blob/8ee12ef7a2c179f3d7c7da5ab33d76d652042d0b/eng/targets/Helix.Common.props#L39-L60). Be sure to queue a build of the [aspnetcore-helix-matrix pipeline](https://dev.azure.com/dnceng-public/public/_build?definitionId=85) against your branch for validation.