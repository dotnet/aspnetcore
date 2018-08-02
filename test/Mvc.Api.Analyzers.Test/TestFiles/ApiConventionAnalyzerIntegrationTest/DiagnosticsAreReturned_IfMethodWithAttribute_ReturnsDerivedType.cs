using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    [ApiController]
    public class DiagnosticsAreReturned_IfMethodWithAttribute_ReturnsDerivedType : ControllerBase
    {
        [ProducesResponseType(404)]
        public ActionResult<DiagnosticsAreReturned_IfMethodWithAttribute_ReturnsDerivedTypeBaseModel> Method(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            /*MM*/return new DiagnosticsAreReturned_IfMethodWithAttribute_ReturnsDerivedTypeDerived();
        }
    }

    public class DiagnosticsAreReturned_IfMethodWithAttribute_ReturnsDerivedTypeBaseModel { }

    public class DiagnosticsAreReturned_IfMethodWithAttribute_ReturnsDerivedTypeDerived : DiagnosticsAreReturned_IfMethodWithAttribute_ReturnsDerivedTypeBaseModel { }
}
