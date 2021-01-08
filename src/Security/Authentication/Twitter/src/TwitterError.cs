using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authentication.Twitter
{
    public class TwitterError
    {
        public int Code { get; set; }
        public string Message { get; set; }
    }
}
