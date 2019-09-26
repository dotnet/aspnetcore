using Microsoft.AspNetCore.Testing;
using Xunit;

[assembly: Repeat(1)]
[assembly: AssemblyFixture(typeof(TestAssemblyFixture))]
[assembly: TestFramework("Microsoft.AspNetCore.Testing.AspNetTestFramework", "Microsoft.AspNetCore.Testing")]
