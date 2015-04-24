using System;

namespace MusicStore.Components
{
    /// <summary>
    /// Abstracts the system clock to facilitate testing.
    /// </summary>
    public interface ISystemClock
    {
        /// <summary>
        /// Gets a DateTime object that is set to the current date and time on this computer,
        /// expressed as the Coordinated Universal Time(UTC)
        /// </summary>
        DateTime UtcNow { get; }
    }
}