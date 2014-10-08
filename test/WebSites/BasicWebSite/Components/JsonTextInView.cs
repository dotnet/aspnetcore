using BasicWebSite.Models;
using Microsoft.AspNet.Mvc;

namespace BasicWebSite.Components
{
    [ViewComponent(Name = "JsonTextInView")]
    public class JsonTextInView : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return Json(new Person()
            {
                Id = 10,
                Name = "John"
            });
        }
    }
}