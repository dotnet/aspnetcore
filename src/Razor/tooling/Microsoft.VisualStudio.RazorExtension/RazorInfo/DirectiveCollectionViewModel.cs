// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.RazorExtension.RazorInfo
{
    public class DirectiveCollectionViewModel : NotifyPropertyChanged
    {
        private readonly ProjectSnapshot _project;

        internal DirectiveCollectionViewModel(ProjectSnapshot project)
        {
            _project = project;

            Directives = new ObservableCollection<DirectiveItemViewModel>();

            var feature = _project.GetProjectEngine().EngineFeatures.OfType<IRazorDirectiveFeature>().FirstOrDefault();
            foreach (var directive in feature?.Directives ?? Array.Empty<DirectiveDescriptor>())
            {
                Directives.Add(new DirectiveItemViewModel(directive));
            }
        }

        public ObservableCollection<DirectiveItemViewModel> Directives { get; }
    }
}

#endif
