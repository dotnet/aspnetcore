# MVC contributor guidance

## Test organization

- Keep unit tests under each product project's `test/` directory. MVC generally uses singular `.Test` project names; preserve each project's exact existing name rather than deriving or normalizing it.
- Use `src/Mvc/test/Mvc.FunctionalTests` for hosted HTTP behavior and `src/Mvc/test/Mvc.IntegrationTests` for MVC's established model-binding and validation integration scenarios.
- Reuse the application and fixture that own the functional scenario. Most functional test applications live under `src/Mvc/test/WebSites`; add a new site only when an existing one cannot represent the scenario clearly.
- Put helpers shared across MVC test projects in the existing `src/Mvc/shared` project that matches their responsibility; keep project-specific helpers with their test project.
