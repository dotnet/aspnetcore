using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.FeatureModel
{
    public interface IFeatureContainer : IDisposable
    {
        object GetFeature(Type type);
        void SetFeature(Type type, object feature);
        //IEnumerable<Type> GetFeatureTypes();
        int Revision { get; }
    }
}
