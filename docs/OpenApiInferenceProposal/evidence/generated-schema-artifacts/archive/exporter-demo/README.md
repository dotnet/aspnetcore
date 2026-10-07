# Collectible-exporter phase evidence

This archive preserves the temporary two-stage proof that loaded the generated
assembly through a collectible `AssemblyLoadContext`.

- [`build-transcript.txt`](build-transcript.txt) is the sanitized historical build
  transcript. It invokes `AnnotatedSchemaExporter` and records graph identity
  `0E53596F...`.
- [`results`](results/) contains the corresponding Dry, Short, and default
  BenchmarkDotNet reports and transcripts.

The exporter established the build boundary but executed module/static initializers.
It is no longer part of the active artifact build. The current prototype emits the
canonical resource graph in the annotation generator's own pass; see the
[current generated-artifact proof](../../current-proof.md).
