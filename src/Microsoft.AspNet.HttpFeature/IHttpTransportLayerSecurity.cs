#if NET45
using System.Security.Cryptography.X509Certificates;
#endif
using System.Threading.Tasks;

namespace Microsoft.AspNet.HttpFeature
{
    public interface IHttpTransportLayerSecurity
    {
#if NET45
        X509Certificate ClientCertificate { get; set; }
#endif
        Task LoadAsync();
    }
}