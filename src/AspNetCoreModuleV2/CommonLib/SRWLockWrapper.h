#pragma once
class SRWLockWrapper
{
public:
	SRWLockWrapper(const SRWLOCK& lock);
	~SRWLockWrapper();
private:
    const SRWLOCK& m_lock;
};
