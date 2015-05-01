// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ModelBinding;

namespace ModelBindingWebSite
{
    public class BindingBehaviorModel
    {
        [BindingBehavior(BindingBehavior.Never)]
        public string BehaviourNeverProperty { get; set; }

        [BindingBehavior(BindingBehavior.Optional)]
        public string BehaviourOptionalProperty { get; set; }

        [BindingBehavior(BindingBehavior.Required)]
        public string BehaviourRequiredProperty { get; set; }

        [BindRequired]
        public string BindRequiredProperty { get; set; }

        [BindNever]
        public string BindNeverProperty { get; set; }
    }
}
