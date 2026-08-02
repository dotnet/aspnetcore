Contributor documentation
=========================

The primary audience for documentation in this folder is contributors to ASP.NET Core.
If you are looking for documentation on how to *use* ASP.NET Core, go to <https://docs.asp.net>.

> :bulb: If you're a new contributor looking to set up the repo locally, the [build from source documentation](BuildFromSource.md) is the best place to start.

The table below outlines the different docs in this folder and what they are helpful for.

| Documentation        | What is it about?   | Who is it for?      |
|--------------------------------------------------------------------------|-------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------|
| [API review process](APIReviewProcess.md)      | Outlines the process for reviewing API changes in ASP.NET Core          | Anyone looking to understand the process for making API changes to ASP.NET Core      |
| [Artifacts structure](Artifacts.md)            | Outlines the artifacts produced by the build  | Anyone looking to understand artifacts produced from an Azure DevOps build          |
| [Troubleshooting build errors](BuildErrors.md) | Common errors that occur when building the repo and how to resolve them | Anyone running into an issue with the build        |
| [Building from source](BuildFromSource.md)     | Setup instructions for the ASP.NET Core repo  | First-time contributors          |
| [Working with EventSources and EventCounters](EventSourceAndCounters.md) | Guidance on adding event tracing to a library | Anyone needing to add event tracing for diagnostics purposes      |
| [List of Diagnostics](list-of-diagnostics.md) | List of diagnostic codes used in repo | Anyone needing to add new codes for diagnostics purposes |
| [Tests on Helix](Helix.md)        | An overview of the Helix test environment     | Anyone debugging tests in Helix or looking to understand the output from Helix builds       |
| [Issue management](IssueManagementPolicies.md) | Overview of policies in place to manage issues| Community members and collaborators looking to understand how we handle closed issues, issues that need author feedback, etc |
| [Preparing a patch update](PreparingPatchUpdates.md)        | Documentation on how to setup for a patch release of ASP.NET Core       | Anyone looking to publish servicing updates         |
| [Project properties](ProjectProperties.md)     | Overview of configurable MSBuild properties on the repo    | Anyone looking to modify how a project is packaged   |
| [How references are resolved](ReferenceResolution.md)       | Overview of dependency reference setup in the repo         | Anyone looking to understand how package references are configured in the repo |
| [Servicing changes](Servicing.md) | Documentation on how to submit servicing PRs to previous releases       | Anyone to submit patches or backports to prior releases, contains the "Shiproom Template"  |
| [Shared framework](SharedFramework.md)         | Overview of the ASP.NET Core Shared framework | Anyone looking to understand the policies in place for managing the code of the shared framework         |
| [Submodules](Submodules.md)           | Documentation on working with submodules in Git     |   Anyone working with submodules in the repo     |
| [Triage process](TriageProcess.md)| Overview of the issue triage process used in the repo     | Anyone looking to understand the triage process on the repo  |
| [Updating Major Version & TFM](UpdatingMajorVersionAndTFM.md)| Instructions for updating the repo branding & TFM in preparation for a new major release     | Repo developers who want to know more about our branding & release process  |
| [Assembly trimming guide](Trimming.md)| Guidance on adding trimming support to an ASP.NET Core assembly     | Repo developers who want to help add support for trimming to ASP.NET Core  |
| [Adding new Projects to the Repo](AddingNewProjects.md) | Outlines the process of adding new projects (i.e. `.csproj` files) to the repo | Anyone who finds themselves trying to add a new project and including it in the build.  |
| [Using WebTransport in Kestrel](WebTransport.md) | Outlines how to setup Kestrel to use WebTransport | Anyone looking to support WebTransport |
| [Benchmarking](Benchmarks.md) | Instructions on how to benchmark PRs and local changes | .NET team |
