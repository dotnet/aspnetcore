# JsonSchema.Net.Generation inspection sources

This directory has two roles:

1. Most top-level sources, `flagship.bundle.json`, and
   `FlagshipCorvusProgramImage.g.cs` reproduce the archived released-7.3.11
   investigation and its old `CF865...` graph. They remain in place because the
   inspection benchmark project compiles them.
2. `corvus-image-producer/` is reused by the active same-pass demo to compile the
   current compatibility schema and emit a binding-private program image.

The historical interpretation and results are under
[`archive/released-7.3.11-spike`](../archive/released-7.3.11-spike/README.md).
The active flow and current identities are under
[current generated-artifact proof](../current-proof.md). Do not treat
the top-level historical bundle or image source in this directory as the current
schema authority.
