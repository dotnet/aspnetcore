// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.hxx"
#include "dbgutil.h"
#include <stdio.h>

DECLARE_DEBUG_PRINT_OBJECT( "test" );

VOID PrintLevel( DWORD level )
{
    DWORD old = DEBUG_FLAGS_VAR;
    DEBUG_FLAGS_VAR = level;

    DBGPRINTF(( DBG_CONTEXT, "Some Data %d\n", 47 ));
    DBGINFO(( DBG_CONTEXT, "Some Info %s\n", "info" ));
    DBGWARN(( DBG_CONTEXT, "Some Info %s\n", "warning" ));
    DBGERROR(( DBG_CONTEXT, "Some Info %s\n", "error" )); 

    DEBUG_FLAGS_VAR = old;
}


#pragma managed

using namespace Microsoft::VisualStudio::TestTools::UnitTesting;

[TestClass]
public ref class DebugUtilitiesTests
{
public:

    [ClassInitialize]
    static void InitializeDebugObjects(TestContext)
    {
        CREATE_DEBUG_PRINT_OBJECT;

        _CrtSetReportMode( _CRT_WARN, _CRTDBG_MODE_FILE ); 
        _CrtSetReportFile( _CRT_WARN, _CRTDBG_FILE_STDERR );
    }

    [TestMethod]
    void TestDbgError()
    {
        PrintLevel( DEBUG_FLAGS_ERROR );
    }

    [TestMethod]
    void TestPrintAny()
    {
        PrintLevel( DEBUG_FLAGS_ANY );
    }

    [TestMethod]
    void TestPrintError()
    {
        DBGERROR_HR( E_FAIL );
        DBGERROR_STATUS( 47 );
    }

    [TestMethod]
    void TestPrintWarn()
    {
        PrintLevel( DEBUG_FLAGS_WARN );
    }

    [TestMethod]
    void TestPrintInfo()
    {
        PrintLevel( DEBUG_FLAGS_INFO );
    }
};