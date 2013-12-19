using System;
using Microsoft.AspNet.FeatureModel.Implementation;

namespace Microsoft.AspNet.FeatureModel
{
    public class ObjectFeatureContainer : IFeatureContainer
    {
        private readonly object _instance;

        public ObjectFeatureContainer(object instance)
        {
            _instance = instance;
        }

        public void Dispose()
        {
            var disposable = _instance as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }

        public object GetFeature(Type type)
        {
            if (type.IsInstanceOfType(_instance))
            {
                return _instance;
            }
            foreach (var interfaceType in _instance.GetType().GetInterfaces())
            {
                if (interfaceType.FullName == type.FullName)
                {
                    return Converter.Convert(interfaceType, type, _instance);
                }
            }
            return null;
        }

        public void SetFeature(Type type, object feature)
        {
            throw new NotImplementedException();
        }

        public int Revision
        {
            get { return 0; }
        }
    }
}