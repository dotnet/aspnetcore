using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// Feature to access the <see cref="Activity"/> associated with a request
    /// </summary>
    public interface IActivityFeature
    {
        /// <summary>
        /// Returns the <see cref="Activity"/>  associated with the current request if available 
        /// </summary>
        Activity? Activity { get; }
    }
}
