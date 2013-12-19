using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Interfaces
{
    public interface IHttpTransportLayerSecurity
    {
        X509Certificate ClientCertificate { get; set; }
        Task LoadAsync();
    }
}