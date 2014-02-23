using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class DisplayFormatAttribute : Attribute
    {
        public bool ConvertEmptyStringToNull { get; set; }
    }
}
