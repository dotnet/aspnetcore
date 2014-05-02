// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class MvcForm : IDisposable
    {
        private readonly ViewContext _viewContext;
        private bool _disposed;

        public MvcForm([NotNull] ViewContext viewContext)
        {
            _viewContext = viewContext;

            // Push the new FormContext; GenerateEndForm() does the corresponding pop.
            _viewContext.FormContext = new FormContext();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Renders the closing </form> tag to the response.
        /// </summary>
        public void EndForm()
        {
            Dispose(disposing: true);
        }

        protected virtual void GenerateEndForm()
        {
            _viewContext.Writer.Write("</form>");

            // TODO revive viewContext.OutputClientValidation(), this requires GetJsonValidationMetadata(), GitHub #163
            _viewContext.FormContext = null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                GenerateEndForm();
            }
        }
    }
}
