# Pull Request Benchmarks

Pull requests that may impact performance should be benchmarked to ensure no regression is introduced.
The most common way is to build the PR on your development machine and submit the binaries to an existing [scenario](https://github.com/aspnet/Benchmarks/tree/main/scenarios). To simplify this process this repository can do it automatically using GitHub Actions.

## Submitting a PR for benchmarking

A special set of comments are recognized by this repository in order to trigger a benchmark. The comment starts with `/benchmark` and takes the name of the benchmark command as an argument. For instance, `/benchmark kestrel` will build the Kestrel components and submit a `plaintext` scenario benchmark to dedicated machines.

Once the benchmark is executed, the results are displayed in new comment on the original PR.

If the comment contains a wrong command, the list of available commands is listed as a new comment. The currently available commands are:

- `/benchmark kestrel`: Builds Kestrel and runs the Plaintext scenario
- `/benchmark yarp`: Builds Kestrel and runs Yarp on http-http
- `/benchmark mvc`: Builds MVC and runs the Plaintext scenario

## Security

- Submitting a PR benchmark is only permitted to the collaborators of the repository. This prevents malicious code from being executed. For this reason the code that is benchmarked needs the same level of scrutiny as if it were to be merged.

- No generated artifacts are stored or made available. Each benchmark rebuilds a new set of binaries.
