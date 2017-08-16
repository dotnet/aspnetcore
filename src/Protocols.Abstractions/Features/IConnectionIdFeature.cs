using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Protocols.Features
{
    public interface IConnectionIdFeature
    {
        string ConnectionId { get; set; }
    }
}
