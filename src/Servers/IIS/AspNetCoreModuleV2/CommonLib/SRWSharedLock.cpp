// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "SRWSharedLock.h"

SRWSharedLock::SRWSharedLock(const SRWLOCK& lock)
    : m_lock(lock)
{
    AcquireSRWLockShared(const_cast<SRWLOCK*>(&m_lock));
}

SRWSharedLock::~SRWSharedLock()
{
    ReleaseSRWLockShared(const_cast<SRWLOCK*>(&m_lock));
}
