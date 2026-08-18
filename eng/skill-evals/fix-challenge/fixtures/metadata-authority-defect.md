# Multi-stage contract projection fixture

## Accepted contract

A generated request contract must describe what the runtime binder can actually
write. A declared input annotation affects the generated nullability only when
the runtime binder exposes an effective write path. Read nullability remains
independent.

## Pipeline

1. `CreateRuntimeDescriptor` combines declared model annotations with binder
   visibility and produces `CanRead`, `CanWrite`, `ReadNullable`, and
   `WriteNullable`.
2. `CreateInlineContract` correctly uses the runtime descriptor. It emits a
   non-nullable contract when `CanRead=true`, `ReadNullable=false`,
   `CanWrite=false`, and `WriteNullable=true`.
3. `CreateSharedContract` later rebuilds nullability from the declared
   `WriteNullable` annotation without checking `CanWrite`. The shared contract
   becomes nullable even though the runtime binder cannot write the member.
4. The serialized contract document is the consumer-visible output.

## Retained behavioral evidence

- The frozen implementation reaches both contract stages and serializes the
  shared contract.
- For an annotated hidden writer, the identical serialized-document assertion
  expects non-nullable output and fails because the shared contract is nullable.
- Gating declared write nullability on `CanWrite` makes that identical assertion
  pass.
- An annotated public writer remains nullable before and after the candidate.
- An annotated hidden writer explicitly included by the binder remains nullable
  before and after the candidate.
- A non-nullable reader with no write annotation remains non-nullable.
- The focused matrix is 3/4 on frozen code and 4/4 with the candidate. Directly
  impacted unchanged contract tests pass with the candidate.

## Existing review note

A review note proposes replacing the generated member name string with
`nameof(TModel.Member)`. `TModel` is unconstrained, and the generated member
name follows a configurable output naming policy rather than the CLR member
name.
