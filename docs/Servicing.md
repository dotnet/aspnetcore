# Servicing Process

We maintain several on-going releases at once and produce patches for them. An essential part of our support commitment to users is that we build high-quality patches that avoid breaking applications. This means we have to be extremely cautious with taking changes in patch releases. This document describes the "bar" (criteria for accepting servicing fixes) and the process for managing these changes.

See the [.NET Core release lifecycle](https://dotnet.microsoft.com/platform/support/policy/dotnet-core#lifecycle) for more details on the currently-supported .NET releases.

The status of current servicing fixes can be found on the [Servicing Status](https://github.com/orgs/dotnet/projects/448) GitHub project.

## Servicing Bar

The servicing bar is defined as any fixes the .NET "Shiproom" (see below) approves. We use certain criteria to evaluate fixes (described below) but still reserve the right to accept/reject bugs despite this criteria in certain circumstances.

A fix is generally suitable for accepting in a servicing release if **all** of the below are true:

* It impacts a "significant" number of users. There's no formal definition here, but generally means multiple users have reported the issue, or the team is confident that a large number of users would be affected.
* It has no suitable workaround. Since any change comes with risk, having users apply a workaround is generally preferable to shipping an update that may cause more issues.
* It does not change public API (removing/adding/changing APIs). Applications should be binary-compatible with **all** patches for a given major.minor version, so API changes cannot be made in patches.
* Any behavioral changes are fixing unexpected exceptions/failures/crashes/etc. or are behind opt-in configuration. In rare cases, where the value is high, we will take changes that are not opt-in, but will provide opt-out configuration to disable them and restore previous behavior.

In addition, the following factors make a particular servicing fix *more likely* to be accepted:

* It fixes a regression introduced in a previous release
* It is necessary to meet key "tenants" (Security, Compliance, Geopolitical issues, etc.)
* It is required to support new OS distributions
* If the issue is reported through [Microsoft Product Support](https://dotnet.microsoft.com/platform/support).

Finally, infrastructure and test-only fixes are generally acceptable since they do not impact the customer usage of the product. However, these should generally be focused on fixes that improve the *reliability* of building/testing the product.

### Long-Term Support Releases

In general, Long-Term Support releases are very risk-averse. Users choose these releases over the "Current" releases because their applications can't take the risks involved in frequent updates. We want users to feel very confident installing patches.

As a result, in general, requests for servicing fixes in Long-Term Support releases should come through [Microsoft Support](https://dotnet.microsoft.com/platform/support).

## Submitting a fix to Shiproom

**External Contributors**: In general, this will be done by a team member. Reach out to the team members reviewing your change to ask for help with this process.

To request Shiproom approval for a fix, open a **Pull Request** to the target `release/` branch (for example `release/3.1` for 3.1.x). Prior to submitting to shiproom, ensure all of the following:

* The PR is "ready-to-merge" (Has at least one review approval, passing builds, is not a draft)
* The PR description contains the following template: [https://aka.ms/aspnet/servicing/template](https://aka.ms/aspnet/servicing/template)

Once the above conditions are met, apply the `servicing-consider` label.

## Shiproom

The .NET Shiproom meets regularly (approximately twice a week) and reviews PRs labelled `servicing-consider`. The Shiproom attendees include stakeholders from across the stack (runtime, libraries, app models, sdk, etc.). Any PR with this label will be considered. Having a fully-complete template is important to ensuring the PR can be properly reviewed. Generally, someone familiar with the PR should be present at the meeting, but having the template filled out helps ensure that if that person is unavailable, the bug is well-represented.

After reviewing a PR, Shiproom will take one of the following actions:

* Apply the `servicing-approved` label and place it in the appropriate milestone based on the target patch release. The change is approved and can be merged when branches are open for the target patch.
* Apply the `servicing-more-info` label (or just leave `servicing-consider`) and request additional information or better representation at a subsequent meeting for approval.
* Apply the `servicing-rejected` label. The change has been declined and should not be merged. It can be resubmitted if there is new information to consider.

## Merging

Only a repository admin can merge changes to `release/*` branches. Once branches open for a particular patch release, the admins will go through and merge PRs labelled `servicing-approved` and targeting that patch release. Sometimes we are tracking multiple patch releases at once (rare, but it happens) so it's possible that only some approved PRs will be merged at the same time.
