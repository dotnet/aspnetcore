---
name: 🐞 Razor Tooling Bug report
about: Report an issue about something that is not working in the new Razor tooling
labels: area-razor.tooling, feature-razor.vs
---

<!--

More information on our issue management policies can be found here: https://aka.ms/aspnet/issue-policies

Please keep in mind that the GitHub issue tracker is not intended as a general support forum, but for reporting **non-security** bugs and feature requests.

If you believe you have an issue that affects the SECURITY of the platform, please do NOT create an issue and instead email your issue details to secure@microsoft.com. Your report may be eligible for our [bug bounty](https://www.microsoft.com/en-us/msrc/bounty-dot-net-core) but ONLY if it is reported through email.
For other types of questions, consider using [StackOverflow](https://stackoverflow.com).

-->

<!-- NOTE: This issue template is meant specifically to be used for issues with the new experimental Razor tooling experience provided in Visual Studio's Preview Feature pane -->

### Describe the bug
A clear and concise description of what the bug is.

### To Reproduce
<!--
We ❤ code! Point us to a minimalistic repro project hosted in a GitHub repo.
For a repro project, create a new ASP.NET Core project using the template of your your choice, apply the minimum required code to result in the issue you're observing.

We will close this issue if:
- the repro project you share with us is complex. We can't investigate custom projects, so don't point us to such, please.
- if we will not be able to repro the behavior you're reporting
-->

### Logs & Exceptions

Please collect the data below before reporting your issue to aid us in diagnosing the root cause.

#### Activity log
[Here](https://docs.microsoft.com/en-us/visualstudio/extensibility/how-to-use-the-activity-log?view=vs-2019#to-examine-the-activity-log) are the instructions on how to generate/acquire one.

#### Razor Language Server Client log
<!-- In Visual Studio's `Output` window, the drop-down contains a `Razor Language Server Client` item. Include that below. -->
<details>
<summary>Razor Language Server Client Log Output</summary>

Paste log output here

</details>

#### HTML Language Server Client log
<!-- In Visual Studio's `Output` window, the drop-down contains a `HtmlyLanguageClient` item. Include that below. -->
<details>
<summary>HTML Language Server Client Log Output</summary>

Paste log output here

</details>

### Further technical details
- VS version (Help => About Microsoft Visual Studio, i.e. 16.8.0 Preview 1 30313.27...). If in Codespaces there will be two versions (server and client), please provide both.
- Scenario (Local, LiveShare, Codespaces)

### Pre-requisite checklist
- [ ] Steps to reproduce the issue
- [ ] Visual Studio Activity Log attached.
- [ ] Razor Language Server client logs included.
- [ ] HTML Language Server client logs included
