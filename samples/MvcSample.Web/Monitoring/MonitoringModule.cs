#if NET45
using System;
using System.Collections.Concurrent;
using Autofac;
using Autofac.Core;

namespace MvcSample.Web
{
    /// <summary>
    /// Summary description for MonitoringModule
    /// </summary>
    public class MonitoringModule : Module
    {
        private static ConcurrentDictionary<Tuple<Type, IComponentLifetime>, int> _registrations
            = new ConcurrentDictionary<Tuple<Type, IComponentLifetime>, int>();

        private static ConcurrentDictionary<Tuple<Type, IComponentLifetime>, object> _instances
            = new ConcurrentDictionary<Tuple<Type, IComponentLifetime>, object>();

        public static readonly ConcurrentDictionary<Tuple<Type, IComponentLifetime>, int> InstanceCount
            = new ConcurrentDictionary<Tuple<Type, IComponentLifetime>, int>();

        protected override void AttachToComponentRegistration(IComponentRegistry componentRegistry,
                                                              IComponentRegistration registration)
        {
            registration.Activating += Registration_Activating;
            registration.Activated += Registration_Activated;
        }

        public Tuple<Type, IComponentLifetime> GetKey(IComponentRegistration context)
        {
            var activator = context.Activator;
            var lifeTime = context.Lifetime;
            var limitType = context.Activator.LimitType;

            var key = new Tuple<Type, IComponentLifetime>(limitType, lifeTime);

            return key;
        }
        private void Registration_Activated(object sender, ActivatedEventArgs<object> e)
        {
            object instance;
            var key = GetKey(e.Component);
            if (_instances.TryGetValue(key, out instance))
            {
                bool same = (e.Instance == instance);
                InstanceCount.AddOrUpdate(key, 1, (_, count) => same ? 1 : count + 1);
            }
        }

        private void Registration_Activating(object sender, ActivatingEventArgs<object> e)
        {
            var key = GetKey(e.Component);
            _registrations.AddOrUpdate(key, 1, (k, value) => value + 1);
            _instances.GetOrAdd(key, e.Instance);
        }

        private void Registration_Preparing(object sender, PreparingEventArgs e)
        {
            foreach (var param in e.Parameters)
            {
                Console.WriteLine(param.ToString());
            }
        }

        public static void Clear()
        {
            //string count = InstanceCount.Select(kvp => kvp.Value).Aggregate((c, n) => c + n).ToString() + " instances from " + InstanceCount.Count + " types";
            InstanceCount.Clear();
            _instances.Clear();
            _registrations.Clear();

           // return count;
        }
    }
}
#endif