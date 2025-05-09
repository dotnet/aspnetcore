using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Optional interface for models that wish to receive uploaded files from multipart/form-data requests.
/// </summary>
public interface IFormFileReceiver
{
    void SetFiles(IFormFileCollection files);
}
