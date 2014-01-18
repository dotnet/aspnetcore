// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Microsoft.TestCommon;

namespace System.Web.TestUtil
{
    public static class MemberHelper
    {
        private static ConstructorInfo GetConstructorInfo(object instance, Type[] parameterTypes)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }
            ConstructorInfo constructorInfo = instance.GetType().GetConstructor(parameterTypes);
            if (constructorInfo == null)
            {
                throw new ArgumentException(String.Format(
                    "A matching constructor on type '{0}' could not be found.",
                    instance.GetType().FullName));
            }
            return constructorInfo;
        }

        private static EventInfo GetEventInfo(object instance, string eventName)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }
            if (String.IsNullOrEmpty(eventName))
            {
                throw new ArgumentException("An event must be specified.", "eventName");
            }
            EventInfo eventInfo = instance.GetType().GetEvent(eventName);
            if (eventInfo == null)
            {
                throw new ArgumentException(String.Format(
                    "An event named '{0}' on type '{1}' could not be found.",
                    eventName, instance.GetType().FullName));
            }
            return eventInfo;
        }

        private static MethodInfo GetMethodInfo(object instance, string methodName, Type[] types = null, MethodAttributes attrs = MethodAttributes.Public)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }
            if (String.IsNullOrEmpty(methodName))
            {
                throw new ArgumentException("A method must be specified.", "methodName");
            }

            MethodInfo methodInfo;
            if (types != null)
            {
                methodInfo = instance.GetType().GetMethod(methodName,
                                                          BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                                                          null,
                                                          types,
                                                          null);
            }
            else
            {
                methodInfo = instance.GetType().GetMethod(methodName,
                                                          BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }

            if (methodInfo == null)
            {
                throw new ArgumentException(String.Format(
                    "A method named '{0}' on type '{1}' could not be found.",
                    methodName, instance.GetType().FullName));
            }

            if ((methodInfo.Attributes & attrs) != attrs)
            {
                throw new ArgumentException(String.Format(
                    "Method '{0}' on type '{1}' with attributes '{2}' does not match the attributes '{3}'.",
                    methodName, instance.GetType().FullName, methodInfo.Attributes, attrs));
            }

            return methodInfo;
        }

        private static PropertyInfo GetPropertyInfo(object instance, string propertyName)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }
            if (String.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentException("A property must be specified.", "propertyName");
            }
            PropertyInfo propInfo = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (propInfo == null)
            {
                throw new ArgumentException(String.Format(
                    "A property named '{0}' on type '{1}' could not be found.",
                    propertyName, instance.GetType().FullName));
            }
            return propInfo;
        }

        private static void TestAttribute<TAttribute>(MemberInfo memberInfo, TAttribute attributeValue)
            where TAttribute : Attribute
        {
            object[] attrs = memberInfo.GetCustomAttributes(typeof(TAttribute), true);

            if (attributeValue == null)
            {
                Assert.True(attrs.Length == 0, "Member should not have an attribute of type " + typeof(TAttribute));
            }
            else
            {
                Assert.True(attrs != null && attrs.Length > 0,
                            "Member does not have an attribute of type " + typeof(TAttribute));
                Assert.Equal(attributeValue, attrs[0]);
            }
        }

        public static void TestBooleanProperty(object instance, string propertyName, bool initialValue, bool testDefaultValue)
        {
            // Assert initial value
            TestGetPropertyValue(instance, propertyName, initialValue);

            if (testDefaultValue)
            {
                // Assert DefaultValue attribute matches inital value
                TestDefaultValue(instance, propertyName);
            }

            TestPropertyValue(instance, propertyName, true);
            TestPropertyValue(instance, propertyName, false);
        }

        public static void TestDefaultValue(object instance, string propertyName)
        {
            PropertyInfo propInfo = GetPropertyInfo(instance, propertyName);

            object initialValue = propInfo.GetValue(instance, null);
            TestAttribute(propInfo, new DefaultValueAttribute(initialValue));
        }

        public static void TestEvent<TEventArgs>(object instance, string eventName, TEventArgs eventArgs) where TEventArgs : EventArgs
        {
            EventInfo eventInfo = GetEventInfo(instance, eventName);

            // Assert category "Action"
            TestAttribute(eventInfo, new CategoryAttribute("Action"));

            // Call protected method with no event handlers, assert no error
            MethodInfo methodInfo = GetMethodInfo(instance, "On" + eventName, attrs: MethodAttributes.Family | MethodAttributes.Virtual);
            methodInfo.Invoke(instance, new object[] { eventArgs });

            // Attach handler, call method, assert fires once
            List<object> eventHandlerArgs = new List<object>();
            EventHandler<TEventArgs> handler = new EventHandler<TEventArgs>(delegate(object sender, TEventArgs t)
            {
                eventHandlerArgs.Add(sender);
                eventHandlerArgs.Add(t);
            });
            eventInfo.AddEventHandler(instance, handler);
            methodInfo.Invoke(instance, new object[] { eventArgs });
            Assert.Equal(new[] { instance, eventArgs }, eventHandlerArgs.ToArray());

            // Detach handler, call method, assert not fired
            eventHandlerArgs = new List<object>();
            eventInfo.RemoveEventHandler(instance, handler);
            methodInfo.Invoke(instance, new object[] { eventArgs });
            Assert.Empty(eventHandlerArgs);
        }

        public static void TestGetPropertyValue(object instance, string propertyName, object valueToCheck)
        {
            PropertyInfo propInfo = GetPropertyInfo(instance, propertyName);
            object value = propInfo.GetValue(instance, null);
            Assert.Equal(valueToCheck, value);
        }

        public static void TestEnumProperty<TEnumValue>(object instance, string propertyName, TEnumValue initialValue, bool testDefaultValue)
        {
            // Assert initial value
            TestGetPropertyValue(instance, propertyName, initialValue);

            if (testDefaultValue)
            {
                // Assert DefaultValue attribute matches inital value
                TestDefaultValue(instance, propertyName);
            }

            PropertyInfo propInfo = GetPropertyInfo(instance, propertyName);

            // Values are sorted numerically
            TEnumValue[] values = (TEnumValue[])Enum.GetValues(propInfo.PropertyType);

            // Assert get/set works for all valid enum values
            foreach (TEnumValue value in values)
            {
                TestPropertyValue(instance, propertyName, value);
            }

            // Assert ArgumentOutOfRangeException is thrown for value one less than smallest
            // enum value, and one more than largest enum value
            var targetException = Assert.Throws<TargetInvocationException>(() => propInfo.SetValue(instance, Convert.ToInt32(values[0]) - 1, null));
            Assert.IsType<ArgumentOutOfRangeException>(targetException.InnerException);

            targetException = Assert.Throws<TargetInvocationException>(() => propInfo.SetValue(instance, Convert.ToInt32(values[values.Length - 1]) + 1, null));
            Assert.IsType<ArgumentOutOfRangeException>(targetException.InnerException);
        }

        public static void TestInt32Property(object instance, string propertyName, int value1, int value2)
        {
            TestPropertyValue(instance, propertyName, value1);
            TestPropertyValue(instance, propertyName, value2);
        }

        public static void TestPropertyWithDefaultInstance(object instance, string propertyName, object valueToSet)
        {
            PropertyInfo propInfo = GetPropertyInfo(instance, propertyName);

            // Set to explicit property
            propInfo.SetValue(instance, valueToSet, null);
            object value = propInfo.GetValue(instance, null);
            Assert.Equal(valueToSet, value);

            // Set to null
            propInfo.SetValue(instance, null, null);
            value = propInfo.GetValue(instance, null);
            Assert.IsAssignableFrom(propInfo.PropertyType, value);
        }

        public static void TestPropertyWithDefaultInstance(object instance, string propertyName, object valueToSet, object defaultValue)
        {
            PropertyInfo propInfo = GetPropertyInfo(instance, propertyName);

            // Set to explicit property
            propInfo.SetValue(instance, valueToSet, null);
            object value = propInfo.GetValue(instance, null);
            Assert.Same(valueToSet, value);

            // Set to null
            propInfo.SetValue(instance, null, null);
            value = propInfo.GetValue(instance, null);
            Assert.Equal(defaultValue, value);
        }

        public static void TestPropertyValue(object instance, string propertyName, object value)
        {
            TestPropertyValue(instance, propertyName, value, value);
        }

        public static void TestPropertyValue(object instance, string propertyName, object valueToSet, object valueToCheck)
        {
            PropertyInfo propInfo = GetPropertyInfo(instance, propertyName);
            propInfo.SetValue(instance, valueToSet, null);
            object value = propInfo.GetValue(instance, null);
            Assert.Equal(valueToCheck, value);
        }

        public static void TestStringParams(object instance, Type[] constructorParameterTypes, object[] parameters)
        {
            ConstructorInfo ctor = GetConstructorInfo(instance, constructorParameterTypes);
            TestStringParams(ctor, instance, parameters);
        }

        public static void TestStringParams(object instance, string methodName, object[] parameters)
        {
            TestStringParams(instance, methodName, null, parameters);
        }

        public static void TestStringParams(object instance, string methodName, Type[] types, object[] parameters)
        {
            MethodInfo method = GetMethodInfo(instance, methodName, types);
            TestStringParams(method, instance, parameters);
        }

        private static void TestStringParams(MethodBase method, object instance, object[] parameters)
        {
            ParameterInfo[] parameterInfos = method.GetParameters();
            foreach (ParameterInfo parameterInfo in parameterInfos)
            {
                if (parameterInfo.ParameterType == typeof(String))
                {
                    object originalParameter = parameters[parameterInfo.Position];

                    parameters[parameterInfo.Position] = null;
                    Assert.ThrowsArgumentNullOrEmpty(
                        delegate()
                        {
                            try
                            {
                                method.Invoke(instance, parameters);
                            }
                            catch (TargetInvocationException e)
                            {
                                throw e.InnerException;
                            }
                        },
                        parameterInfo.Name);

                    parameters[parameterInfo.Position] = String.Empty;
                    Assert.ThrowsArgumentNullOrEmpty(
                        delegate()
                        {
                            try
                            {
                                method.Invoke(instance, parameters);
                            }
                            catch (TargetInvocationException e)
                            {
                                throw e.InnerException;
                            }
                        },
                        parameterInfo.Name);

                    parameters[parameterInfo.Position] = originalParameter;
                }
            }
        }

        public static void TestStringProperty(object instance, string propertyName, string initialValue,
                                              bool testDefaultValueAttribute = false, bool allowNullAndEmpty = true,
                                              string nullAndEmptyReturnValue = "")
        {
            // Assert initial value
            TestGetPropertyValue(instance, propertyName, initialValue);

            if (testDefaultValueAttribute)
            {
                // Assert DefaultValue attribute matches inital value
                TestDefaultValue(instance, propertyName);
            }

            if (allowNullAndEmpty)
            {
                // Assert get/set works for null
                TestPropertyValue(instance, propertyName, null, nullAndEmptyReturnValue);

                // Assert get/set works for String.Empty
                TestPropertyValue(instance, propertyName, String.Empty, nullAndEmptyReturnValue);
            }
            else
            {
                Assert.ThrowsArgumentNullOrEmpty(
                    delegate()
                    {
                        try
                        {
                            TestPropertyValue(instance, propertyName, null);
                        }
                        catch (TargetInvocationException e)
                        {
                            throw e.InnerException;
                        }
                    },
                    "value");
                Assert.ThrowsArgumentNullOrEmpty(
                    delegate()
                    {
                        try
                        {
                            TestPropertyValue(instance, propertyName, String.Empty);
                        }
                        catch (TargetInvocationException e)
                        {
                            throw e.InnerException;
                        }
                    },
                    "value");
            }

            // Assert get/set works for arbitrary value
            TestPropertyValue(instance, propertyName, "TestValue");
        }
    }
}
