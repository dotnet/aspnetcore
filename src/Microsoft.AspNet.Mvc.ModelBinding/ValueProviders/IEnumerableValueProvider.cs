using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public interface IEnumerableValueProvider : IValueProvider
    {
        IDictionary<string, string> GetKeysFromPrefix(string prefix);
    }
}
