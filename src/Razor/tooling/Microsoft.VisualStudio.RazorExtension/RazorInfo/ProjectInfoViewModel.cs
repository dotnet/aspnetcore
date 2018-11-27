// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE
using System.Collections.ObjectModel;

namespace Microsoft.VisualStudio.RazorExtension.RazorInfo
{
    public class ProjectInfoViewModel : NotifyPropertyChanged
    {
        private ObservableCollection<DirectiveViewModel> _directives;
        private ObservableCollection<DocumentViewModel> _documents;
        private ObservableCollection<TagHelperViewModel> _tagHelpers;

        public ObservableCollection<DirectiveViewModel> Directives
        {
            get { return _directives; }
            set
            {
                _directives = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<DocumentViewModel> Documents
        {
            get { return _documents; }
            set
            {
                _documents = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<TagHelperViewModel> TagHelpers
        {
            get { return _tagHelpers; }
            set
            {
                _tagHelpers = value;
                OnPropertyChanged();
            }
        }
    }
}
#endif
