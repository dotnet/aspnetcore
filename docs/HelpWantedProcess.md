# Help Wanted Process

This document describes the process that the team uses for identifying candidate issues for community members to tackle.

## Process

This process (we'll refer to it as `help-wanted` process going forward) triggers when the engineering team decides that a particular issue may be a good fit for a community member to tackle. Below are the stages that issue will go through, as part of this process:
1. The triage team will **assign the `help candidate` label** to the issue, as well as **assign an engineer to the issue**.
2. The task for the assigned engineer will be to understand the ask based on all the comments in the issue and **summarize the problem in a comment** using the [Help Wanted Template](/docs/HelpWantedIssueSummaryCommentTemplate.md).
3. As part of filling out the template, the engineer will also need to **describe a high-level design for how to approach the problem and how it can be solved**.
4. When all the sections of the template are filled in, the engineer will then need to **apply one of the `Complexity: <value>` labels**, to indicate how easy / hard will it be to tackle the issue.
 This will help community members to find the right type of issue to contribute to.
5. After posting this comment the assigned engineer should:
   - Unassign themself from the issue.
   - **Replace the `help candidate` label with `help wanted` label**, as an indicator, that the issue is ready for the community to pick up.
   - Copy the direct link to the summary comment and add the link to the bottom of the description of the issue to help with discoverability, as there can be too many comments in the issue and the summary comment may be hard to find.
        Here is an example comment to use:

        ```text
        **Summary Comment** : https://github.com/dotnet/aspnetcore/issues/51912#issuecomment-1801246403
        ```

This will conclude the process.
