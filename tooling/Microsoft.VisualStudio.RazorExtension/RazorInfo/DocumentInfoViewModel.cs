// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServices.Razor;

namespace Microsoft.VisualStudio.RazorExtension.RazorInfo
{
    public class DocumentInfoViewModel : NotifyPropertyChanged
    {
        private RazorEngineDocument _document;

        internal DocumentInfoViewModel(RazorEngineDocument document)
        {
            _document = document;
        }

        public string Text => _document.Text;
    }
}