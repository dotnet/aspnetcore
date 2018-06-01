// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

class SRWLockWrapper
{
public:
	SRWLockWrapper(const SRWLOCK& lock);
	~SRWLockWrapper();
private:
    const SRWLOCK& m_lock;
};
