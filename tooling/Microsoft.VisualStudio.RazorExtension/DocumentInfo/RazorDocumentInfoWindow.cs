// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE

using System;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.RazorExtension.DocumentInfo
{
    [Guid("d8d83218-309c-4c8f-9c9f-38a6fead8dca")]
    internal class RazorDocumentInfoWindow : ToolWindowPane
    {
        private IVsEditorAdaptersFactoryService _adapterFactory;
        private VisualStudioDocumentTrackerFactory _documentTrackerService;
        private IVsTextManager _textManager;
        private IVsRunningDocumentTable _rdt;
        
        private uint _cookie;
        private ITextView _textView;
        private VisualStudioDocumentTracker _documentTracker;

        public RazorDocumentInfoWindow() 
            : base(null)
        {
            Caption = "Razor Document Info";

            Content = new RazorDocumentInfoWindowControl();
        }

        protected override void Initialize()
        {
            base.Initialize();

            var component = (IComponentModel)GetService(typeof(SComponentModel));
            _adapterFactory = component.GetService<IVsEditorAdaptersFactoryService>();
            _documentTrackerService = component.GetService<VisualStudioDocumentTrackerFactory>();
            
            _textManager = (IVsTextManager)GetService(typeof(SVsTextManager));
            _rdt = (IVsRunningDocumentTable)GetService(typeof(SVsRunningDocumentTable));
            
            var hr = _rdt.AdviseRunningDocTableEvents(new RdtEvents(this), out uint _cookie);
            ErrorHandler.ThrowOnFailure(hr);
        }

        protected override void OnClose()
        {
            _rdt.UnadviseRunningDocTableEvents(_cookie);
            _cookie = 0u;

            base.OnClose();
        }

        private void OnBeforeDocumentWindowShow(IVsWindowFrame frame)
        {
            var vsTextView = VsShellUtilities.GetTextView(frame);
            if (vsTextView == null)
            {
                return;
            }

            var textView = _adapterFactory.GetWpfTextView(vsTextView);
            if (textView != null && textView != _textView)
            {
                _textView = textView;

                if (_documentTracker != null)
                {
                    _documentTracker.ContextChanged -= DocumentTracker_ContextChanged;
                }

                _documentTracker = _documentTrackerService.GetTracker(textView);
                _documentTracker.ContextChanged += DocumentTracker_ContextChanged;

                ((FrameworkElement)Content).DataContext = new RazorDocumentInfoViewModel(_documentTracker);
            }
        }

        private void OnAfterDocumentWindowHide(IVsWindowFrame frame)
        {
            var vsTextView = VsShellUtilities.GetTextView(frame);

            var textView = _adapterFactory.GetWpfTextView(vsTextView);
            if (textView == _textView)
            {
                ((FrameworkElement)Content).DataContext = null;
                _documentTracker.ContextChanged -= DocumentTracker_ContextChanged;

                _textView = null;
                _documentTracker = null;
            }
        }

        private void DocumentTracker_ContextChanged(object sender, EventArgs e)
        {
            ((FrameworkElement)Content).DataContext = new RazorDocumentInfoViewModel(_documentTracker);
        }

        private class RdtEvents : IVsRunningDocTableEvents
        {
            private readonly RazorDocumentInfoWindow _window;

            public RdtEvents(RazorDocumentInfoWindow window)
            {
                _window = window;
            }

            public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;

            public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;

            public int OnAfterSave(uint docCookie) => VSConstants.S_OK;

            public int OnAfterAttributeChange(uint docCookie, uint grfAttribs) => VSConstants.S_OK;

            public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
            {
                _window.OnBeforeDocumentWindowShow(pFrame);
                return VSConstants.S_OK;
            }

            public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
            {
                _window.OnAfterDocumentWindowHide(pFrame);
                return VSConstants.S_OK;
            }
        }
    }
}
#endif
