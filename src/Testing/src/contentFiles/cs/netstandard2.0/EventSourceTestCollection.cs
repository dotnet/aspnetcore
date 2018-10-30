namespace Microsoft.AspNetCore.Testing.Tracing
{
    // This file comes from Microsoft.AspNetCore.Testing and has to be defined in the test assembly.
    // It enables EventSourceTestBase's parallel isolation functionality.

    [Xunit.CollectionDefinition(EventSourceTestBase.CollectionName, DisableParallelization = true)]
    public class EventSourceTestCollection
    {
    }
}
