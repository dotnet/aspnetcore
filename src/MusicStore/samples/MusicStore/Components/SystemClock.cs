using System;

namespace MusicStore.Components
{
    /// <summary>
    /// Provides access to the normal system clock.
    /// </summary>
    public class SystemClock : ISystemClock
    {
        /// <inheritdoc />
        public DateTime UtcNow
        {
            get
            {
                // The clock measures whole seconds only, and truncates the milliseconds,
                // because millisecond resolution is inconsistent among various underlying systems.
                DateTime utcNow = DateTime.UtcNow;
                return utcNow.AddMilliseconds(-utcNow.Millisecond);
            }
        }
    }
}
