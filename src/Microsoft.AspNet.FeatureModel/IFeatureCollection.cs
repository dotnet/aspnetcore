using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.FeatureModel
{
    public interface IFeatureCollection : IDictionary<Type, object>, IDisposable
    {
        int Revision { get; }
    }
}
