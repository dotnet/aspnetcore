using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

[assembly: Repeat(1)]
[assembly: LogLevel(LogLevel.Trace)]
[assembly: AssemblyFixture(typeof(TestAssemblyFixture))]
