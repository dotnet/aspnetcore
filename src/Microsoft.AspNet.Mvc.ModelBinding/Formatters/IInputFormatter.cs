using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public interface IInputFormatter
    {
        Task<bool> ReadAsync(InputFormatterContext context);
    }
}
