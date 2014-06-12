using System.IO;
using System.Threading.Tasks;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.HttpFeature
{
    [AssemblyNeutral]
    public interface IHttpOpaqueUpgradeFeature
    {
        bool IsUpgradableRequest { get; }
        Task<Stream> UpgradeAsync();
    }
}