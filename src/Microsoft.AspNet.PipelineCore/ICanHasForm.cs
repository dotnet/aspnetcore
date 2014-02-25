using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.PipelineCore
{
    public interface ICanHasForm
    {
        Task<IReadableStringCollection> GetFormAsync();
    }
}
