using Microsoft.AspNet.Mvc;

namespace ActivatorWebSite
{
    [ViewComponent(Name = "Test")]
    public class TestComponent : ViewComponent
    {
        public IViewComponentResult Invoke(string content)
        {
            return Content(content + "!");
        }
    }
}