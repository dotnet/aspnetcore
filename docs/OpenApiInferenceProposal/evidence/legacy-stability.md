# Legacy stability evidence boundary

Branch HEAD:

```text
7e7b647ef269dbdbf118c6052c2657f98b36626e
```

True merge base with `main`:

```text
89ab93803f3fcbb928f8ed1523945f89284f7e79
```

A byte-for-byte public-app comparison against the merge base is not available.
The evidence app intentionally exercises branch-added public APIs and scenarios
that do not compile at the merge base, including schema generation modes,
version-targeted document generation, scalar format policy, and positional
tuple registration. Rewriting the app to remove those calls would no longer be
the same endpoint/configuration program and would not establish the requested
claim.

The narrower, reviewable evidence is:

- `documents/legacy-oas30.json`, `documents/legacy-oas31.json`, and
  `documents/legacy-oas32.json` are exact current-HEAD public-provider output.
- `validate.ps1` case-sensitively checks representative current Legacy scalar
  annotations (`decimal`/`double` and base64/`byte`) and parses every complete
  Legacy document through `OpenApiDocument.Parse`.
- The repository regression tests explicitly preserve Legacy behavior for
  polymorphism, component-name collisions, extension data, union transformer
  traversal, scalar contracts, collection contracts, tuple schemas, and schema
  transformer traversal. Representative tests are:
  - `SchemaGenerationMode_Legacy_PreservesPolymorphicAnyOf`
  - `SchemaGenerationMode_Legacy_PreservesCollidingComponentBehavior`
  - `SchemaGenerationMode_Legacy_ExtensionDataOutputIsUnchanged`
  - `SchemaGenerationMode_Legacy_UnionTransformerTraversalIsUnchanged`
  - the Legacy rows in `OpenApiSchemaService.ScalarContracts.cs`,
    `OpenApiSchemaService.CollectionContracts.cs`, and
    `OpenApiSchemaService.TupleSchemas.cs` for OpenAPI 3.0, 3.1, and 3.2
- `GeneratedTupleConverters_RequireExplicitOptIn` executes actual generated RDG
  code and verifies that merely referencing OpenAPI leaves default tuple JSON
  unchanged. Adjacent runtime tests cover explicit dynamic/closed opt-in,
  frozen options, and user-converter precedence.
- The final exact repository validation completed with `1350 passed`,
  `5 skipped`, and `0 failed` managed OpenAPI tests. The exact
  `source activate.sh && ./src/OpenApi/build.sh -test` and
  `./src/Http/build.sh -test` validations both completed with zero warnings and
  zero errors.

These checks establish representative current-branch Legacy compatibility and
the tuple opt-in invariant. They do not claim a complete byte-level equivalence
proof against the merge base.
