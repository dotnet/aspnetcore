namespace Microsoft.AspNet.Mvc.Rendering
{
    public interface ICanHasViewContext
    {
        void Contextualize([NotNull] ViewContext viewContext);
    }
}
