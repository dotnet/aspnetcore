// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"
#include "SRWLockWrapper.h"

SRWLockWrapper::SRWLockWrapper(const SRWLOCK& lock)
    : m_lock(lock)
{
    AcquireSRWLockExclusive(const_cast<SRWLOCK*>(&m_lock));
}

SRWLockWrapper::~SRWLockWrapper()
{
    ReleaseSRWLockExclusive(const_cast<SRWLOCK*>(&m_lock));
}
