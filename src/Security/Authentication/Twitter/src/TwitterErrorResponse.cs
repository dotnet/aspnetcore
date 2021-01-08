using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authentication.Twitter
{
    public class TwitterErrorResponse
    {
        public List<TwitterError> Errors { get; set; }
    }
}
