// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ApplicationModel
{
    public class GlobalModel
    {
        public GlobalModel()
        {
            Controllers = new List<ControllerModel>();
            Filters = new List<IFilter>();
        }

        public List<ControllerModel> Controllers { get; private set; }

        public List<IFilter> Filters { get; private set; }
    }
}