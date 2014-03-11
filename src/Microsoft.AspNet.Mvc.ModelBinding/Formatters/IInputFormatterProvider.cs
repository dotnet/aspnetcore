using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public interface IInputFormatterProvider
    {
        IInputFormatter GetInputFormatter(InputFormatterProviderContext context);
    }
}
