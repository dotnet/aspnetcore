using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class CompositeInputFormatter : IInputFormatter
    {
        private IInputFormatter[] _bodyReaders;

        public CompositeInputFormatter(IEnumerable<IInputFormatter> bodyReaders)
        {
            _bodyReaders = bodyReaders.ToArray();
        }

        public async Task<bool> ReadAsync(InputFormatterContext context)
        {
            for(int i = 0; i < _bodyReaders.Length; i++)
            {
                if (await _bodyReaders[i].ReadAsync(context))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
