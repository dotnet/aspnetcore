# Multi-stage contract projection control fixture

## Accepted contract

A generated request contract must describe what the runtime binder can actually
write. A declared input annotation affects generated nullability only when the
runtime binder exposes an effective write path. Read nullability remains
independent.

## Pipeline

1. `CreateRuntimeDescriptor` combines declared annotations with binder
   visibility and produces `CanRead`, `CanWrite`, `ReadNullable`, and
   `WriteNullable`.
2. `CreateInlineContract` uses the runtime descriptor.
3. `CreateSharedContract` copies the inline contract's effective nullability
   instead of reconstructing it from declared annotations.
4. The serialized contract document is the consumer-visible output.

## Retained behavioral evidence

- For an annotated hidden writer, the frozen implementation reaches both
  contract stages and serializes a non-nullable shared contract.
- The annotated public-writer and explicitly included hidden-writer controls
  serialize nullable contracts.
- A non-nullable reader with no write annotation serializes a non-nullable
  contract.
- All four focused cases and the directly impacted unchanged contract tests pass
  on untouched frozen code.
- A historical mutation that reconstructs nullability from declared
  `WriteNullable` makes only the annotated hidden-writer assertion fail.

## Existing review note

A review note proposes replacing the generated member name string with
`nameof(TModel.Member)`. `TModel` is unconstrained, and the generated member
name follows a configurable output naming policy rather than the CLR member
name.
