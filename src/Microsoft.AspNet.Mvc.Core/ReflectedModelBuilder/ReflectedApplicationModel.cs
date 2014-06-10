// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ReflectedModelBuilder
{
    public class ReflectedApplicationModel
    {
        public ReflectedApplicationModel()
        {
            Controllers = new List<ReflectedControllerModel>();
            Filters = new List<IFilter>();
        }

        public List<ReflectedControllerModel> Controllers { get; private set; }

        public List<IFilter> Filters { get; private set; }
    }
}