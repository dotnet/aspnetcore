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
