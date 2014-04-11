using System;

namespace Microsoft.AspNet.Identity
{
    public class IdentityUserRole : IdentityUserRole<string> { }

    /// <summary>
    ///     EntityType that represents a user belonging to a role
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class IdentityUserRole<TKey> where TKey : IEquatable<TKey>
    {
        // TODO: Remove
        public virtual string Id
        {
            get;
            set;
        }

        /// <summary>
        ///     UserId for the user that is in the role
        /// </summary>
        public virtual TKey UserId { get; set; }

        /// <summary>
        ///     RoleId for the role
        /// </summary>
        public virtual TKey RoleId { get; set; }
    }
}