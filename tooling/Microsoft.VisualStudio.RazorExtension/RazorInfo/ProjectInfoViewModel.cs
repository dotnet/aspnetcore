// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE
using System.Collections.ObjectModel;
using System.Windows;

namespace Microsoft.VisualStudio.RazorExtension.RazorInfo
{
    public class ProjectInfoViewModel : NotifyPropertyChanged
    {
        private ObservableCollection<DirectiveDescriptorViewModel> _directives;
        private ObservableCollection<DocumentSnapshotViewModel> _documents;
        private ObservableCollection<TagHelperViewModel> _tagHelpers;
        private bool _tagHelpersLoading;

        public ObservableCollection<DirectiveDescriptorViewModel> Directives
        {
            get { return _directives; }
            set
            {
                _directives = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<DocumentSnapshotViewModel> Documents
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

        public bool TagHelpersLoading
        {
            get { return _tagHelpersLoading; }
            set
            {
                _tagHelpersLoading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TagHelperProgressVisibility));
            }
        }

        public Visibility TagHelperProgressVisibility => TagHelpersLoading ? Visibility.Visible : Visibility.Hidden;

    }
}
#endif
