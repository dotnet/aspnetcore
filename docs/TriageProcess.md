# Triage Process

Managing a popular GitHub repo with a small team is not an easy task. It requires a good balance between creating new features while handling many investigations and bug fixes associated with existing ones.

During the last couple of years the amount of incoming issues has been constantly growing. While this is a sign of a healthy framework and ecosystem surrounding it, it's becoming harder to react to all those issues.
To be able to keep up with ever-evolving expectations, we're introducing a set of rules to help us better handle the incoming issues going forward.

**Note:** Customers that need help with **urgent investigations** should contact [Microsoft Support](https://dotnet.microsoft.com/platform/support).

## Goals

The goals of these rules are listed below in priority order:

- Make it easy to make triage decisions for each issue filed in this repository
- Be able to easily prioritize issues for each milestone
- Set proper expectations with customers regarding how issues are going to be handled

## Triage Process Details

The feature teams should be able to look through every single issue filed against this repository and be able to make a quick call regarding the nature of the issue.
We will first categorize the issues and further handle these depending on the category the issue is in. The subsections below represent these categories and the rules we apply for them during our triage meeting.

### Information Gathering

In this phase we instruct the user on how to collect the appropriate diagnostics and see if they are able to address the issue with that additional information.  When we need user input we will mark the issue with `Needs: Author Feedback` label. Issues in this phase may be closed automatically if we do not receive timely responses, they often do not provide enough information for us to investigate further.
We'll try to respond quickly to such issues (within days). If a user has collected all of the relevant diagnostics and the issue is still not apparent, then we will consider it for further [investigation](#investigations) by the team.

### Feature requests

As soon as we identify an issue that represents an ask for a new feature, we will label the issue with the `enhancement` label.
Most of the time, we will automatically move these issues into the `.NET 8 Planning` milestone for further review during one of our [sprint planning meetings](#milestone-planning).
If we think the feature request is not aligned with our goals, we may choose to close it immediately.
In some situations, however, we may choose to collect more feedback before acting on the issue. In these situations we will move the issue in the `Backlog` so that we can review it during the [release planning](#release-planning).

### Bug reports

If it's immediately clear that the issue is related to a bug in the framework, we will apply the `bug` label to the issue.

At this point, we will try to make a call regarding its impact and severity. If the issue is critical, we may choose to include it in our current milestone for immediate handling or potentially patching.
If the bug is relatively high impact, we will move the issue into the `.NET 8 Planning` milestone to review during our [sprint planning](#milestone-planning) meeting.
If the impact is unclear or the is a very corner case scenario, we may move it to the `Backlog` milestone to further evaluate the impact by reviewing customer upvotes / comments at a later time.

### Investigations

In many situations it's not immediately clear whether a specific issue reported is a bug or not. To be certain, the team will need to spend time to investigate it before making a call regarding the faith of the issue. In these situations we will apply the `investigate` label to the issue.
In many situations, these issues turn out to be a result of some kind of misconfiguration in the user code.
In some rare situations, however, these turn out to be caused by very impactful issues. So we will make a call during the triage whether we need to immediately investigate certain issues or not.
If not, we will move the investigation to the `.NET 8 Planning` to review during one of the upcoming [sprint planning meetings](#milestone-planning).

### Documentation requests

Some issues turn out to indicate user confusion around how to configure different aspects of the framework.
When we determine such issues, we will mark these with the `Docs` label and move them into the `.NET 8 Planning` milestone to handle at a later time. The goal here will be to fill in the gaps or clarify our documentation, so that customers can be successful by using the guidance provided in the documentation.
If we identify a documentation issue which too many customers are having trouble with, we may choose to address that in current milestone.

## Milestone Planning

Our milestones are usually a month long.
Before each milestone we have one or more planning meetings, where we look through all the accumulated issues in the `.NET 8 Planning` milestone and choose the most important and impactful ones to handle during the next milestone. This will be a mixture of feature requests, bug fixes, documentation issues as well as some investigations.

Note, that we will investigate only issues, which have accumulated more than certain number of upvotes and/or comments, which will indicate that there is some wider impact associated with it.
We may not investigate issues which haven't received many votes/comments and choose to close these. The reason is that the impact is very scoped and potentially something is wrong in the user code. Consider asking these questions on StackOverflow.

For some feature requests and bug reports, depending on the user involvement, we may choose to move these to the backlog at this point. What this means, is that they will not be looked at again up until the next major release planning.

## Release Planning

Once we approach the end of the release cycle (such as for .NET 7) we will look through the accumulated issues in the `Backlog` milestone. This is a long process as the amount of issues accumulated in this milestone is quite large.

We will try to prioritize issues with most community requests / upvotes assuming these are aligned with our goals.
Issues, which we will think are candidates for the upcoming release, will be moved to the `.NET 8 Planning` milestone. This process is documented in more detail in the [Release Planning](https://github.com/dotnet/aspnetcore/blob/main/docs/ReleasePlanning.md) document.

## Cleanup
As we go through all the issues in the Backlog milestone as part of our release planning process, we will also cleanup the milestone by closing low priority issues, which have stayed in the backlog for more than 2 releases. While some of these issues may seem reasonable, the fact that those haven't been addressed for such prolonged period of time indicate that they're not as important for the product as they may seem to be.

## Process Visualization

The following diagram summarizes the processes detailed above:
![image](https://user-images.githubusercontent.com/34246760/142244350-bd3484e2-fdee-450d-ac94-e0d192aa962a.png)

## References

We rely on some automation to help us with this process. You can learn more about some of these by reading our [Issue Management Policies](https://github.com/dotnet/aspnetcore/blob/main/docs/IssueManagementPolicies.md) document.
