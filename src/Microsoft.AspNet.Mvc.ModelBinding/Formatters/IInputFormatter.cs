using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public interface IInputFormatter
    {
        /// <summary>
        /// Gets the mutable collection of media types supported by this <see cref="JsonInputFormatter"/> instance.
        /// </summary>
        IList<string> SupportedMediaTypes { get; }

        /// <summary>
        /// Gets the mutable collection of character encodings supported by this <see cref="JsonInputFormatter"/> 
        /// instance.
        /// </summary>
        IList<Encoding> SupportedEncodings { get; }

        /// <summary>
        /// Called during deserialization to read an object from the request.
        /// </summary>
        Task ReadAsync(InputFormatterContext context);
    }
}
