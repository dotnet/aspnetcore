using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    internal interface IInputFileJsCallbacks
    {
        Task NotifyChange(FileListEntry[] files);
    }
}
