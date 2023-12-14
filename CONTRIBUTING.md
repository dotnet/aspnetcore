# How to contribute

One of the easiest ways to contribute is to participate in discussions on GitHub issues. You can also contribute by submitting pull requests with code changes.

## General feedback and discussions?

Start a discussion on the [repository issue tracker](https://github.com/dotnet/aspnetcore/issues).

## Bugs and feature requests?

❗ **IMPORTANT: If you want to report a security-related issue, please see the `Reporting security issues and bugs` section below.**

Before reporting a new issue, try to find an existing issue if one already exists. If it already exists, upvote (👍) it. Also, consider adding a comment with your unique scenarios and requirements related to that issue.  Upvotes and clear details on the issue's impact help us prioritize the most important issues to be worked on sooner rather than later. If you can't find one, that's okay, we'd rather get a duplicate report than none.

If you can't find an existing issue, log a new issue in the appropriate GitHub repository. Here are some of the most common repositories:

* [Docs](https://github.com/aspnet/Docs)
* [AspNetCore](https://github.com/dotnet/aspnetcore)
* [Entity Framework Core](https://github.com/dotnet/efcore)
* [Tooling](https://github.com/aspnet/Tooling)
* [Runtime](https://github.com/dotnet/runtime)

Or browse the full list of repositories in the [aspnet](https://github.com/aspnet/) organization.

## Reporting security issues and bugs

Security issues and bugs should be reported privately, via email, to the Microsoft Security Response Center (MSRC)  secure@microsoft.com. You should receive a response within 24 hours. If for some reason you do not, please follow up via email to ensure we received your original message. Further information, including the MSRC PGP key, can be found in the [Security TechCenter](https://technet.microsoft.com/security/ff852094.aspx).

## Other discussions

Our team members also monitor several other discussion forums:

* [ASP.NET Core forum](https://forums.asp.net/1255.aspx/1?ASP+NET+5)
* [Stack Overflow](https://stackoverflow.com/) with the [`asp.net-core`](https://stackoverflow.com/questions/tagged/asp.net-core), [`asp.net-core-mvc`](https://stackoverflow.com/questions/tagged/asp.net-core-mvc), or [`entity-framework-core`](https://stackoverflow.com/questions/tagged/entity-framework-core) tags.

## How to submit a PR

We are always happy to see PRs from community members both for bug fixes as well as new features.
To help you be successful we've put together a few simple rules to follow when you prepare to contribute to our codebase:

### Finding an issue to work on

  Over the years we've seen many PRs targeting areas of the framework, which we didn't plan to expand further at the time.
  In all these cases we had to say `no` to those PRs and close them. That, obviously, is not a great outcome for us. And it's especially bad for the contributor, as they've spent a lot of effort preparing the change.
  To resolve this problem, we've decided to separate a bucket of issues, which would be great candidates for community members to contribute to. We mark these issues with the `help wanted` label. You can find all these issues at [https://aka.ms/aspnet/helpwanted](https://aka.ms/aspnet/helpwanted). When you choose an issue you'd like to work on, you'll see that some include a comment from our team that follows the [Issue Summary Template](/docs/HelpWantedIssueSummaryCommentTemplate.md). That comment is meant to make it much simpler for you to both understand what the problem is as well as provide references and hints about how to approach it.

Within that set, we have additionally marked issues that are good candidates for first-time contributors. Those do not require too much familiarity with the framework and are more novice-friendly. Those are marked with the `good first issue` label. The full list of such issues can be found at [https://aka.ms/aspnet/goodfirstissues](https://aka.ms/aspnet/goodfirstissues).

If you would like to make a contribution to an area not documented here, first open an issue with a description of the change you would like to make and the problem it solves so it can be discussed before a pull request is submitted.

### Before writing code

  We've seen PRs, where customers would solve an issue in a way, which either wouldn't fit into the framework because of how it's designed or it would change the framework in a way, which is not something we'd like to do. To avoid these situations and potentially save you a lot of time, we encourage customers to discuss the preferred design with the team first. To do so, file a new `design proposal` issue, link to the issue you'd like to address, and provide detailed information about how you'd like to solve a specific problem. We triage issues periodically and it will not take long for a team member to engage with you on that proposal.
  When you get an agreement from our team members that the design proposal you have is solid, then go ahead and prepare the PR.
  To file a design proposal, look for the relevant issue in the `New issue` page or simply click [this link](https://github.com/dotnet/aspnetcore/issues/new?assignees=&labels=design-proposal&template=4_design_proposal.md):
  ![image](https://user-images.githubusercontent.com/34246760/107969904-41b9ae80-6f65-11eb-8b84-d15e7d94753b.png)

### Before submitting the pull request

Before submitting a pull request, make sure that it checks the following requirements:

* You find an existing issue with the "help-wanted" label or discuss with the team to agree on adding a new issue with that label
* You post a high-level description of how it will be implemented and receive a positive acknowledgement from the team before getting too committed to the approach or investing too much effort in implementing it.
* You add test coverage following existing patterns within the codebase
* Your code matches the existing syntax conventions within the codebase
* Your PR is small, focused, and avoids making unrelated changes

If your pull request contains any of the below, it's less likely to be merged.

* Changes that break backward compatibility
* Changes that are only wanted by one person/company. Changes need to benefit a large enough proportion of ASP.NET developers.
* Changes that add entirely new feature areas without prior agreement
* Changes that are mostly about refactoring existing code or code style
* Very large PRs that would take hours to review (remember, we're trying to help lots of people at once). For larger work areas, please discuss with us to find ways of breaking it down into smaller, incremental pieces that can go into separate PRs.

### During pull request review

A core contributor will review your pull request and provide feedback. To ensure that there is not a large backlog of inactive PRs, the pull request will be marked as stale after two weeks of no activity. After another four days, it will be closed.

### Resources to help you get started

Here are some resources to help you get started on how to contribute code or new content.

* Look at the [Contributor documentation](/docs/README.md) to get started on building the source code on your own.
* ["Help wanted" issues](https://github.com/dotnet/aspnetcore/labels/help%20wanted) - these issues are up for grabs. Comment on an issue if you want to create a fix.
* ["Good first issue" issues](https://github.com/dotnet/aspnetcore/labels/good%20first%20issue) - we think these are good for newcomers.

### Identifying the scale

If you would like to contribute to one of our repositories, first identify the scale of what you would like to contribute. If it is small (grammar/spelling or a bug fix) feel free to start working on a fix. If you are submitting a feature or substantial code contribution, please discuss it with the team and ensure it follows the product roadmap. You might also read these two blogs posts on contributing code: [Open Source Contribution Etiquette](http://tirania.org/blog/archive/2010/Dec-31.html) by Miguel de Icaza and [Don't "Push" Your Pull Requests](https://www.igvita.com/2011/12/19/dont-push-your-pull-requests/) by Ilya Grigorik. All code submissions will be rigorously reviewed and tested further by the ASP.NET team, and only those that meet an extremely high bar for both quality and design/roadmap appropriateness will be merged into the source.

### Submitting a pull request

You will need to sign a [Contributor License Agreement](https://cla.dotnetfoundation.org/) when submitting your pull request. To complete the Contributor License Agreement (CLA), you will need to follow the instructions provided by the CLA bot when you send the pull request. This needs to only be done once for any .NET Foundation OSS project.

If you don't know what a pull request is read this article: <https://help.github.com/articles/using-pull-requests>. Make sure the repository can build and all tests pass. Familiarize yourself with the project workflow and our coding conventions. The coding, style, and general engineering guidelines are published on the [Engineering guidelines](https://github.com/dotnet/aspnetcore/wiki/Engineering-guidelines) page.

### Tests

* Tests need to be provided for every bug/feature that is completed.
* Tests only need to be present for issues that need to be verified by QA (for example, not tasks)
* If there is a scenario that is far too hard to test there does not need to be a test for it.
  * "Too hard" is determined by the team as a whole.

### Feedback

Your pull request will now go through extensive checks by the subject matter experts on our team. Please be patient; we have hundreds of pull requests across all of our repositories. Update your pull request according to feedback until it is approved by one of the ASP.NET team members. After that, one of our team members may adjust the branch you merge into based on the expected release schedule.

## Merging pull requests

When your pull request has had all feedback addressed, it has been signed off by one or more reviewers with commit access, and all checks are green, we will commit it.

We commit pull requests as a single Squash commit unless there are special circumstances. This creates a simpler history than a Merge or Rebase commit. "Special circumstances" are rare, and typically mean that there are a series of cleanly separated changes that will be too hard to understand if squashed together, or for some reason we want to preserve the ability to bisect them.

## Additional Resources

Here are videos (partially outdated) where members of the ASP.NET Core team provide guidance, advice and samples on how to contribute to this project.
* For ASP.NET Core - https://www.youtube.com/watch?v=hVdwb41FPvU
* For Blazor - https://www.youtube.com/watch?v=gRg0xxK8L6w

## Code of conduct

See [CODE-OF-CONDUCT.md](./CODE-OF-CONDUCT.md)
