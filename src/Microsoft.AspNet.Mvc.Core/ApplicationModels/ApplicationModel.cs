// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
    public class ApplicationModel
    {
        public ApplicationModel()
        {
            ApiExplorer = new ApiExplorerModel();
            Controllers = new List<ControllerModel>();
            Filters = new List<IFilter>();
        }

        /// <summary>
        /// Gets or sets the <see cref="ApiExplorerModel"/> for the application.
        /// </summary>
        /// <remarks>
        /// <see cref="ApplicationModel.ApiExplorer"/> allows configuration of default settings
        /// for ApiExplorer that apply to all actions unless overridden by 
        /// <see cref="ControllerModel.ApiExplorer"/> or <see cref="ActionModel.ApiExplorer"/>.
        /// 
        /// If using <see cref="ApplicationModel.ApiExplorer"/> to set <see cref="ApiExplorerModel.IsVisible"/> to
        /// <c>true</c>, this setting will only be honored for actions which use attribute routing.
        /// </remarks>
        public ApiExplorerModel ApiExplorer { get; set; }

        public IList<ControllerModel> Controllers { get; private set; }

        public IList<IFilter> Filters { get; private set; }
    }
}