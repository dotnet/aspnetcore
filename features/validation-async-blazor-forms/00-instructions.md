# Agent instructions - Async support for Blazor form validation

## Research

We begin to work on adding support for async validation operations and pending validation UI for Blazor forms. You will help us research, design, implement and test the features.

First, do a preliminary research into the area. Read the following sources and build up a knowledge base. The sources will be either:

- URLs to fetch and read (follow any relevant looking references in the content)
- Relative file paths pointing to different parts of the dotnet/aspnetcore repository (checked out locally, root is in ../../ from the work dir)

Sources:

- Existing implementation of Blazor forms (components and services): ./src/Components/Forms
- Original PR adding the feature, read for context (but beware some things have changed): https://github.com/dotnet/aspnetcore/pull/761
- Open issue with basic design ideas for the async form validation features we want to add: https://github.com/dotnet/aspnetcore/issues/7680
- Another closed issue with relevant discussion: https://github.com/dotnet/aspnetcore/issues/40244
- Existing implementation of a new validation package, we are moving Blazor forms to use this package, it is designed for async support: ./src/Validation
- Proposal design doc for adding async support to the DataAnnotations APIs in the BCL: https://github.com/dotnet/designs/pull/363

Topics to do a general web search for (focusing on relevant sources suchs as MSDN, GitHub, StackOverflow, tech blogs):

- Async Blazor form validation
- Async form validation in popular web frameworks, if relevant (e.g., Angular, React, Rails, Django, Spring Boot)
- EditContext.ValidateAsync
- Reporting pending Blazor form validation
- Blazor form validation indicators
- General Blazor async indicators

Use these searches to gather requested features, user pain points, prior art that we could learn from, etc.

Compile your findings into a structured markdown document `01-research.md`.

## Scope

Based on this research, lets create a specification listing goals for the solution. Try to not tie the goals to a specific deisgn (we will create that later) but focus on scenarios and user needs. Include code examples of use. List anything that could make sense - we will review and trim the list later. Write the output into 02-spec-draft.md

## Design


Use either custom AsyncValidationAttribute prototype (before BCL changes) or FluentValidation async validators as benchmark of viability.

## Implementation plan

## Testing plan

## Implementation

## Open questions and alternatives
