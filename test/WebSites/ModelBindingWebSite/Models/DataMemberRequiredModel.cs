// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.Serialization;

namespace ModelBindingWebSite
{
    [DataContract]
    public class DataMemberRequiredModel
    {
        [DataMember]
        public string ImplicitlyOptionalProperty { get; set; }

        [DataMember(IsRequired = false)]
        public string ExplicitlyOptionalProperty { get; set; }

        [DataMember(IsRequired = true)]
        public string RequiredProperty { get; set; }
    }
}
