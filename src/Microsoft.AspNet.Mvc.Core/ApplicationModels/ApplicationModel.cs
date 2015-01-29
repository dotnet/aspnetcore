// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
    public class ApplicationModel
    {
        public ApplicationModel()
        {
            Controllers = new List<ControllerModel>();
            Filters = new List<IFilter>();
        }

        public IList<ControllerModel> Controllers { get; private set; }

        public IList<IFilter> Filters { get; private set; }
    }
}