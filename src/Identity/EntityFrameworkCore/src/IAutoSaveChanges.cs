using System;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore
{
    public interface IAutoSaveChanges
    {
        bool AutoSaveChanges { get; set; }
    }
}
