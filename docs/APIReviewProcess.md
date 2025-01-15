# Getting started

To get the process of adding/modifying API going, file a new issue using [the API Proposal issue template](https://github.com/dotnet/aspnetcore/issues/new?assignees=&labels=api-suggestion&template=30_api_proposal.md&title=). Below is more information about the process.

## Process

The goal of the API Review process is to ensure that the new APIs are following common patterns and best practices.
Also, it's aimed to help and guide engineers towards better API design decisions. People should feel empowered to submit their APIs for review as besides all the benefits it's also a learning and knowledge sharing experience.

The process is visualized in the below diagram:
![A sequence diagram illustrating the same process described below.](https://user-images.githubusercontent.com/34246760/66542496-95052c80-eae7-11e9-9c7c-549b82a8d492.png)

1. The API review process kicks in after the owner of the issue identifies that the work required for the issue will need an API change or addition. In such cases, the issue owner will handle (either himself/herself, or with the community member who has expressed interest in handling the work) driving a design proposal. When working with a community member, the issue owner is responsible for guiding them to an acceptable design.
1. If the proposed design adds new APIs, mark those issues with the `api-suggestion` label
1. When the issue owner thinks the proposal is in good shape, he/she marks the issue with `api-ready-for-review` label. Also, the @asp-net-api-reviews team should be notified of the issue.
1. The `asp-net-api-reviews` team will host a weekly API review meeting and will review your proposed API change during the next meeting. If you have an API scheduled for review, you must have a representative in the meeting.
1. Some API reviews can happen through a shorter process. For these situations, simply ping the API review crew for a quicker review, so that it can happen as a conversation.
1. When an API change/suggestion gets approved, the `api-approved` label should be added to the issue.
1. The owner of the issue is now free to work on the implementation of the proposed API.
1. In case during implementation changes to the original proposal are required, the review should become obsolete and the process should start from the beginning.

## Learnings and growth

Over time the team will build up certain conventions and principles, that will be useful to establish for future API reviews too. To help with this, the [API Review Principles document](/docs/APIReviewPrinciples.md) document will be used to store such principles and conventions, making them persistent over time. Eventually, it will grow into a good knowledge base that will also help newcomers to learn and be better prepared with designing their APIs.

## What Makes an issue/PR "ready-for-review"?

Before marking an issue as `api-ready-for-review`, make sure that the issue has the following:

* A short description that will help reviewers not familiar with this area.
* The API changes in ref-assembly format. It's fine to link this to the generated ref-assembly-code in the PR. If the changes are to an area that does produce ref-assemblies, please write out what it would look like in ref-assembly format for us to review.

```text
Good: This is the API for the widget factory, users use it in startup code to
configure how their widgets work. We have an overload that accepts URI, but
not one that accepts string, so we're adding it for convenience.

Bad: Adding a string overload for Widget.ConfigureFactory.
```

Note: Ideally all of the following would be in the top comment on an issue, but that's not always possible when the issue was opened by a user. As a rule, we don't edit or replace user comments except for formatting, or if they break the rules. In this case it's fine to post a new comment on the issue, OR to edit the top post and insert a link. If you edit an external contributor's post to add a link make sure you explain why it was done!

In general, larger changes should have more explanation and context provided, and small changes need less explanation. A really large change or feature-area design should probably come with a lot of explanation: [example](https://github.com/dotnet/aspnetcore/issues/17160)

### Why do we do this?

Putting this information in an issue with all of the context makes it possible for discussion to take place before the api-review meeting. Writing things down and posting them online enables remote work as well as our community to give feedback on designs as well. We want to provide enough context for people *working outside that feature area* to understand what the change is about and give meaningful feedback. If you're ready to present a change in the meeting, then you should definitely be ready to explain why it matters.

We use the ref-assembly format because it's more readable and useful for the kinds of things that come up in api-review discussions. Using a more compact format (without docs and implementations) makes it easier to notice patterns. In the rare case that you have to manually transcribe this format, think of this as you spending a little time to save a lot of others time in the meeting.

## If you are the "champion" for a community-submitted change

If you are assigned a community-submitted change to *champion* in our API-review, then just put on your pretend pajamas and pretend that it was your change to begin with. Come to the meeting ready to explain why this addition is needed, and why it's the best approach.

## API Review Meeting

The API Review meeting is open to all members of the ASP.NET Core team. The meeting invite as well as communication about API reviews is shared on the internal `ASP.NET Core API Reviews` distribution list. Every API review meeting should include the area owners of the API change proposals as mandatory attendees. To list of all pending API review proposals can be found at: <https://aka.ms/aspnet/apireviews>
