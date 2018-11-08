// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.AspNetCore.Cryptography
{
    internal static class WeakReferenceHelpers
    {
        public static T GetSharedInstance<T>(ref WeakReference<T> weakReference, Func<T> factory)
            where T : class, IDisposable
        {
            // First, see if the WR already exists and points to a live object.
            WeakReference<T> existingWeakRef = Volatile.Read(ref weakReference);
            T newTarget = null;
            WeakReference<T> newWeakRef = null;

            while (true)
            {
                if (existingWeakRef != null)
                {
                    T existingTarget;
                    if (weakReference.TryGetTarget(out existingTarget))
                    {
                        // If we created a new target on a previous iteration of the loop but we
                        // weren't able to store the target into the desired location, dispose of it now.
                        newTarget?.Dispose();
                        return existingTarget;
                    }
                }

                // If the existing WR didn't point anywhere useful and this is our
                // first iteration through the loop, create the new target and WR now.
                if (newTarget == null)
                {
                    newTarget = factory();
                    Debug.Assert(newTarget != null);
                    newWeakRef = new WeakReference<T>(newTarget);
                }
                Debug.Assert(newWeakRef != null);

                // Try replacing the existing WR with our newly-created one.
                WeakReference<T> currentWeakRef = Interlocked.CompareExchange(ref weakReference, newWeakRef, existingWeakRef);
                if (ReferenceEquals(currentWeakRef, existingWeakRef))
                {
                    // success, 'weakReference' now points to our newly-created WR
                    return newTarget;
                }

                // If we got to this point, somebody beat us to creating a new WR.
                // We'll loop around and check it for validity.
                Debug.Assert(currentWeakRef != null);
                existingWeakRef = currentWeakRef;
            }
        }
    }
}
