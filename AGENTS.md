# ASP.NET Core contributor guidance

## Test organization

- Place unit tests under the product's `test/` directory. Name unit-test projects `<ProductAssembly>.Tests`; preserve an area's established `.Test` suffix.
- Keep established test categories separate. In areas with a `.FunctionalTests` project, use it for hosted application or server boundaries. For complete browser or external workflows, preserve the area's established `.E2ETests` or `.E2E.Tests` suffix.
- Treat test project names as build-significant. They control test-project detection and often match exact `InternalsVisibleTo` entries; do not invent alternative names or override test-project detection.
- Put supporting applications and libraries under the area's `testassets/` directory unless the area has an established alternative. Do not give a support project a test-project suffix unless it contains discovered tests.
- Name each test file after its primary test class. For type-focused tests, map `Foo` to `FooTest` or `FooTests`, following nearby convention. Name scenario tests after the behavior exercised, and extend an existing matching test class when one exists.
- Use public test classes and descriptive PascalCase test methods. Follow the containing project's test framework, method-name style, namespace, fixtures, and parallelization configuration.
- Keep helpers used by one test class private or nested. Put reused helpers in the project's established `Helpers`, `Infrastructure`, or `TestObjects` structure.
