using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class JsonInputFormatter : IInputFormatter
    {
        public Task<bool> ReadAsync(InputFormatterContext bindingContext)
        {
            return Task.FromResult(false);
        }
    }
}
