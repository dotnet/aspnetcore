---
name: Analyzer proposal
about: Propose a new analyzer/codefixer or an update to an existing one
title: ''
labels: api-suggestion, analyzer
assignees: ''
---

## Background and Motivation

<!--
We welcome new analyzers and codefixers in the ASP.NET repo!

We use the same process to review both new analyzer/codefixer submissions and API proposals. There is an overview of our process [here](https://github.com/dotnet/aspnetcore/blob/main/docs/APIReviewProcess.md). This template will help us gather the information we need to start the review process.

Under this heading, describe the problem that your analyzer is trying to solve. Examples of great motivating scenarios include helping users avoid
performance issues, potentially insecure code, or recommending better APIs for a scenario.
-->

## Proposed Analyzer

### Analyzer Behavior and Message

<!--
Provide a description of when the analyzer will trigger and the associated analyzer message.
-->

<!--
Analyzer categories are derived from the categories documented in https://learn.microsoft.com/dotnet/fundamentals/code-analysis/categories
To select a category, review each category's description and select the best category based on the functionality of your analyzer.

Analyzer severity levels are documented in https://learn.microsoft.com/visualstudio/code-quality/use-roslyn-analyzers#configure-severity-levels
Review the description to observe how the level set on the analyzer will affect build-time and editor behavior and select the best
level for the task.
-->

### Category

- [ ] Design
- [ ] Documentation
- [ ] Globalization
- [ ] Interoperability
- [ ] Maintainability
- [ ] Naming
- [ ] Performance
- [ ] Reliability
- [ ] Security
- [ ] Style
- [ ] Usage

### Severity Level

- [ ] Error
- [ ] Warning
- [ ] Info
- [ ] Hidden

## Usage Scenarios

<!--
Provide code examples that would trigger your analyzer to warn. Identify the spans of code that the analyzer
will be triggered on. When applicable, describe the result of the code fix associated with the change.
-->

## Risks

<!--
Please mention any risks that to your knowledge the API proposal might entail, such as breaking changes, performance regressions, etc.
-->
