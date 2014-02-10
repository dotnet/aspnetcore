using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Hosting.Builder
{
    public interface IBuilderFactory
    {
        IBuilder CreateBuilder();
    }
}