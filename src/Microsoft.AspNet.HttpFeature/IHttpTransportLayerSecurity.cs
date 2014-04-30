using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.HttpFeature
{
    [AssemblyNeutral]
    public interface IHttpTransportLayerSecurity
    {
        X509Certificate ClientCertificate { get; set; }
        Task LoadAsync();
    }
}
