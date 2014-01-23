using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.FeatureModel
{
    public interface IInterfaceDictionary : IDictionary<Type, object>, IDisposable
    {
        int Revision { get; }
    }
}
