using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNet.FeatureModel.Implementation;

namespace Microsoft.AspNet.FeatureModel
{
    public class FeatureContainer : IFeatureContainer
    {
        private readonly IFeatureContainer _defaultFeatures;
        private readonly IDictionary<Type, object> _featureByFeatureType = new Dictionary<Type, object>();
        private readonly IDictionary<string, Type> _featureTypeByName = new Dictionary<string, Type>();
        private readonly object _containerSync = new Object();
        private int _containerRevision;

        public FeatureContainer()
        {
        }   
        
        public FeatureContainer(IFeatureContainer defaultFeatures)
        {
            _defaultFeatures = defaultFeatures;
        }

        public virtual object GetFeature(Type type)
        {
            object feature;
            if (_featureByFeatureType.TryGetValue(type, out feature))
            {
                return feature;
            }

            Type actualType;
            if (_featureTypeByName.TryGetValue(type.FullName, out actualType))
            {
                if (_featureByFeatureType.TryGetValue(actualType, out feature))
                {
                    return Converter.Convert(type, actualType, feature);
                }
            }

            return _defaultFeatures != null ? _defaultFeatures.GetFeature(type) : null;
        }

        public virtual object GetDefaultFeature(Type type)
        {
            return null;
        }

        public virtual void SetFeature(Type type, object feature)
        {
            lock (_containerSync)
            {
                Type priorFeatureType;
                if (_featureTypeByName.TryGetValue(type.FullName, out priorFeatureType))
                {
                    if (priorFeatureType == type)
                    {
                        _featureByFeatureType[type] = feature;
                    }
                    else
                    {
                        _featureTypeByName[type.FullName] = type;
                        _featureByFeatureType.Remove(priorFeatureType);
                        _featureByFeatureType.Add(type, feature);
                    }
                }
                else
                {
                    _featureTypeByName.Add(type.FullName, type);
                    _featureByFeatureType.Add(type, feature);
                }
                Interlocked.Increment(ref _containerRevision);
            }
        }

        public virtual int Revision
        {
            get { return _containerRevision; }
        }

        public void Dispose()
        {
        }
    }
}