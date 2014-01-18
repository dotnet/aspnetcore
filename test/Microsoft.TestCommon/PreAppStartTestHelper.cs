// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.ComponentModel;
using System.Reflection;
using Microsoft.TestCommon;

namespace System.Web.WebPages.TestUtils
{
    public static class PreAppStartTestHelper
    {
        public static void TestPreAppStartClass(Type preAppStartType)
        {
            string typeMessage = String.Format("The type '{0}' must be static, public, and named 'PreApplicationStartCode'.", preAppStartType.FullName);
            Assert.True(preAppStartType.IsSealed && preAppStartType.IsAbstract && preAppStartType.IsPublic && preAppStartType.Name == "PreApplicationStartCode", typeMessage);

            string editorBrowsableMessage = String.Format("The only attribute on type '{0}' must be [EditorBrowsable(EditorBrowsableState.Never)].", preAppStartType.FullName);
            object[] attrs = preAppStartType.GetCustomAttributes(typeof(EditorBrowsableAttribute), true);
            Assert.True(attrs.Length == 1 && ((EditorBrowsableAttribute)attrs[0]).State == EditorBrowsableState.Never, editorBrowsableMessage);

            string startMethodMessage = String.Format("The only public member on type '{0}' must be a method called Start().", preAppStartType.FullName);
            MemberInfo[] publicMembers = preAppStartType.GetMembers(BindingFlags.Public | BindingFlags.Static);
            Assert.True(publicMembers.Length == 1, startMethodMessage);
            Assert.True(publicMembers[0].MemberType == MemberTypes.Method, startMethodMessage);
            Assert.True(publicMembers[0].Name == "Start", startMethodMessage);
        }
    }
}
