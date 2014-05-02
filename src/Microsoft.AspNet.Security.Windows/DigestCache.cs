// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

// -----------------------------------------------------------------------
// <copyright file="DigestCache.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Threading;

namespace Microsoft.AspNet.Security.Windows
{
    // Saves generated digest challenges so that they are still valid when the authenticated request arrives.
    internal class DigestCache : IDisposable
    {
        private const int DigestLifetimeSeconds = 300;
        private const int MaximumDigests = 1024;  // Must be a power of two.
        private const int MinimumDigestLifetimeSeconds = 10;

        private DigestContext[] _savedDigests;
        private ArrayList _extraSavedDigests;
        private ArrayList _extraSavedDigestsBaking;
        private int _extraSavedDigestsTimestamp;
        private int _newestContext;
        private int _oldestContext;

        internal DigestCache()
        {
        }

        internal void SaveDigestContext(NTAuthentication digestContext)
        {
            if (_savedDigests == null)
            {
                Interlocked.CompareExchange<DigestContext[]>(ref _savedDigests, new DigestContext[MaximumDigests], null);
            }

            // We want to actually close the contexts outside the lock.
            NTAuthentication oldContext = null;
            ArrayList digestsToClose = null;
            lock (_savedDigests)
            {
                int now = ((now = Environment.TickCount) == 0 ? 1 : now);

                _newestContext = (_newestContext + 1) & (MaximumDigests - 1);

                int oldTimestamp = _savedDigests[_newestContext].timestamp;
                oldContext = _savedDigests[_newestContext].context;
                _savedDigests[_newestContext].timestamp = now;
                _savedDigests[_newestContext].context = digestContext;

                // May need to move this up.
                if (_oldestContext == _newestContext)
                {
                    _oldestContext = (_newestContext + 1) & (MaximumDigests - 1);
                }

                // Delete additional contexts older than five minutes.
                while (unchecked(now - _savedDigests[_oldestContext].timestamp) >= DigestLifetimeSeconds && _savedDigests[_oldestContext].context != null)
                {
                    if (digestsToClose == null)
                    {
                        digestsToClose = new ArrayList();
                    }
                    digestsToClose.Add(_savedDigests[_oldestContext].context);
                    _savedDigests[_oldestContext].context = null;
                    _oldestContext = (_oldestContext + 1) & (MaximumDigests - 1);
                }

                // If the old context is younger than 10 seconds, put it in the backup pile.
                if (oldContext != null && unchecked(now - oldTimestamp) <= MinimumDigestLifetimeSeconds * 1000)
                {
                    // Use a two-tier ArrayList system to guarantee each entry lives at least 10 seconds.
                    if (_extraSavedDigests == null ||
                        unchecked(now - _extraSavedDigestsTimestamp) > MinimumDigestLifetimeSeconds * 1000)
                    {
                        digestsToClose = _extraSavedDigestsBaking;
                        _extraSavedDigestsBaking = _extraSavedDigests;
                        _extraSavedDigestsTimestamp = now;
                        _extraSavedDigests = new ArrayList();
                    }
                    _extraSavedDigests.Add(oldContext);
                    oldContext = null;
                }
            }

            if (oldContext != null)
            {
                oldContext.CloseContext();
            }
            if (digestsToClose != null)
            {
                for (int i = 0; i < digestsToClose.Count; i++)
                {
                    ((NTAuthentication)digestsToClose[i]).CloseContext();
                }
            }
        }

        private void ClearDigestCache()
        {
            if (_savedDigests == null)
            {
                return;
            }

            ArrayList[] toClose = new ArrayList[3];
            lock (_savedDigests)
            {
                toClose[0] = _extraSavedDigestsBaking;
                _extraSavedDigestsBaking = null;
                toClose[1] = _extraSavedDigests;
                _extraSavedDigests = null;

                _newestContext = 0;
                _oldestContext = 0;

                toClose[2] = new ArrayList();
                for (int i = 0; i < MaximumDigests; i++)
                {
                    if (_savedDigests[i].context != null)
                    {
                        toClose[2].Add(_savedDigests[i].context);
                        _savedDigests[i].context = null;
                    }
                    _savedDigests[i].timestamp = 0;
                }
            }

            for (int j = 0; j < toClose.Length; j++)
            {
                if (toClose[j] != null)
                {
                    for (int k = 0; k < toClose[j].Count; k++)
                    {
                        ((NTAuthentication)toClose[j][k]).CloseContext();
                    }
                }
            }
        }

        public void Dispose()
        {
            ClearDigestCache();
        }

        private struct DigestContext
        {
            internal NTAuthentication context;
            internal int timestamp;
        }
    }
}
