using System.Globalization;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;

namespace BasicWebSite
{
    public class MonitorController : Controller
    {
        private readonly ActionDescriptorCreationCounter _counterService;

        public MonitorController(INestedProvider<ActionDescriptorProviderContext> counterService)
        {
            _counterService = (ActionDescriptorCreationCounter)counterService;
        }

        public IActionResult CountActionDescriptorInvocations()
        {
            return Content(_counterService.CallCount.ToString(CultureInfo.InvariantCulture));
        }
    }
}