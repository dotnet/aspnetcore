using System;
using System.Reflection;

namespace MusicStore.Spa.Infrastructure
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class BuddyTypeAttribute : Attribute
    {
        private readonly Type _metadataBuddyType;
        private readonly Type _validatorBuddyType;

        public BuddyTypeAttribute(Type buddyType)
        {
            _metadataBuddyType = buddyType;
            _validatorBuddyType = buddyType;
        }

        public Type MetadataBuddyType
        {
            get { return _metadataBuddyType; }
        }

        public Type ValidatorBuddyType
        {
            get { return _validatorBuddyType; }
        }

        public static Type GetBuddyType(Type type)
        {
            var attribute = type.GetTypeInfo().GetCustomAttribute<BuddyTypeAttribute>();

            if (attribute != null)
            {
                return attribute.MetadataBuddyType;
            }

            return null;
        }
    }
}