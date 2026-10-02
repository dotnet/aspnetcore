// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"

#include "sttimer.h"
#include "forwarderconnection.h"
#include "serverprocess.h"
#include "processmanager.h"

FORWARDER_CONNECTION::FORWARDER_CONNECTION()
    : m_cRefs(1),
      m_hConnection(nullptr)
{
}

HRESULT
FORWARDER_CONNECTION::Initialize(
    DWORD
)
{
    return E_NOTIMPL;
}

struct ProcessManagerTestAccess
{
    static void AddReadyProcess(PROCESS_MANAGER& manager, SERVER_PROCESS& process)
    {
        manager.m_dwProcessesPerApplication = 1;
        manager.m_ppServerProcessList = new SERVER_PROCESS*[1] { &process };
        manager.m_fServerProcessListReady = TRUE;

        process.m_fReady = TRUE;
        process.m_dwPort = 5000;
        process.m_pProcessManager = &manager;
        manager.ReferenceProcessManager();
    }

    static void DeleteProcessList(PROCESS_MANAGER& manager)
    {
        delete[] manager.m_ppServerProcessList;
        manager.m_ppServerProcessList = nullptr;
    }
};

namespace Tests
{
class TestServerProcess : public SERVER_PROCESS
{
public:
    explicit TestServerProcess(bool& destroyed)
        : m_destroyed(destroyed)
    {
    }

    ~TestServerProcess() override
    {
        m_destroyed = true;
    }

private:
    bool& m_destroyed;
};

TEST(ProcessManagerTests, GetProcessReferenceSurvivesRemovalFromManager)
{
    PROCESS_MANAGER manager;
    bool destroyed = false;
    auto* process = new TestServerProcess(destroyed);
    ProcessManagerTestAccess::AddReadyProcess(manager, *process);

    SERVER_PROCESS* processRaw = nullptr;
    ASSERT_EQ(S_OK, manager.GetProcess(nullptr, FALSE, &processRaw));
    ASSERT_EQ(process, processRaw);
    SERVER_PROCESS_PTR processReference(processRaw);

    manager.ShutdownProcess(process);

    EXPECT_FALSE(destroyed);
    if (destroyed)
    {
        processReference.release();
    }
    else
    {
        processReference.reset();
        EXPECT_TRUE(destroyed);
    }

    ProcessManagerTestAccess::DeleteProcessList(manager);
}
}
