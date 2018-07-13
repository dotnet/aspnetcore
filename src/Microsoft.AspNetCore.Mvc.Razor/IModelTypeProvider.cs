using System;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    internal interface IModelTypeProvider
    {
        Type GetModelType();
    }
}
