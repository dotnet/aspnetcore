using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authentication.Twitter
{
    internal class TwitterErrorResponse
    {
        public List<TwitterError> Errors { get; set; }
    }
}
