// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.hxx"
#include "pudebug.h"

#define MAX_OBJECT_NAME 256 // chars


LONG g_PuDbgEventsCreated = 0;
LONG g_PuDbgSemaphoresCreated = 0;
LONG g_PuDbgMutexesCreated = 0;



LPSTR
PuDbgpBuildObjectName(
    __in LPSTR ObjectNameBuffer,
    IN   ULONG ObjectNameBufferCch,
    __in LPSTR FileName,
    IN   ULONG LineNumber,
    __in LPSTR MemberName,
    IN   PVOID Address
    )

/*++

Routine Description:

    Internal routine that builds an appropriate object name based on
    the file name, line number, member name, address, and process ID.

Arguments:

    ObjectNameBuffer - Pointer to the target buffer for the name.

    FileName - The filename of the source creating the object. This
        is __FILE__ of the caller.

    LineNumber - The line number within the source. This is __LINE__
        of the caller.

    MemberName - The member/global variable name where the object handle
        is to be stored.

    Address - The address of the containing structure/class or of the
        global itself.

Return Value:

    LPSTR - Pointer to ObjectNameBuffer if successful, NULL otherwise.

    N.B. This routine always returns NULL when running under Win9x.

--*/

{

    PLATFORM_TYPE platformType;
    LPSTR fileNamePart;
    LPSTR result;

    //
    // We have no convenient way to dump objects w/ names from
    // Win9x, so we'll only enable this functionality under NT.
    //

    // platformType = IISGetPlatformType();

    //
    // By default IIS-Duct-tape will only run on NT platforms. So
    // do not worry about getting the platform types yet.
    // 
    platformType = PtNtServer;
    result = NULL;

    if( platformType == PtNtServer ||
        platformType == PtNtWorkstation ) {

        //
        // Find the filename part of the incoming source file name.
        //

        fileNamePart = strrchr( FileName, '\\' );

        if( fileNamePart == NULL ) {
            fileNamePart = strrchr( FileName, '/' );
        }

        if( fileNamePart == NULL ) {
            fileNamePart = strrchr( FileName, ':' );
        }

        if( fileNamePart == NULL ) {
            fileNamePart = FileName;
        } else {
            fileNamePart++;
        }

        //
        // Ensure we don't overwrite our object name buffer.
        //

        if( ( sizeof(":1234567890 :12345678 PID:1234567890") +
              strlen( fileNamePart ) +
              strlen( MemberName ) ) < MAX_OBJECT_NAME ) {

            sprintf_s(
                ObjectNameBuffer,
                ObjectNameBufferCch,
                "%s:%lu %s:%08p PID:%lu",
                fileNamePart,
                LineNumber,
                MemberName,
                Address,
                GetCurrentProcessId()
                );

            result = ObjectNameBuffer;

        }

    }

    return result;

}   // PuDbgpBuildObjectName


HANDLE
PuDbgCreateEvent(
    __in LPSTR FileName,
    IN   ULONG LineNumber,
    __in LPSTR MemberName,
    IN   PVOID Address,
    IN   BOOL ManualReset,
    IN   BOOL InitialState
    )

/*++

Routine Description:

    Creates a new event object.

Arguments:

    FileName - The filename of the source creating the object. This
        is __FILE__ of the caller.

    LineNumber - The line number within the source. This is __LINE__
        of the caller.

    MemberName - The member/global variable name where the object handle
        is to be stored.

    Address - The address of the containing structure/class or of the
        global itself.

    ManualReset - TRUE to create a manual reset event, FALSE to create
        an automatic reset event.

    InitialState - The intitial state of the event object.

Return Value:

    HANDLE - Handle to the object if successful, NULL otherwise.

--*/

{
	UNREFERENCED_PARAMETER( Address );
	UNREFERENCED_PARAMETER( MemberName );
	UNREFERENCED_PARAMETER( LineNumber );
	UNREFERENCED_PARAMETER( FileName );

    LPSTR objName = NULL;
    HANDLE objHandle;
    //CHAR objNameBuffer[MAX_OBJECT_NAME];

/*
    disable passing names to event creation
    Longhorn forces some security checks that
    prevent hostable webcore to work on checked builds
    (at least ASP requests are failing when
     trying to create event)
    
    objName = PuDbgpBuildObjectName(
                  objNameBuffer,
                  FileName,
                  LineNumber,
                  MemberName,
                  Address
                  );
*/
    objHandle = CreateEventA(
                    NULL,                       // lpEventAttributes
                    ManualReset,                // bManualReset
                    InitialState,               // bInitialState
                    objName                     // lpName
                    );

    if( objHandle != NULL ) {
        InterlockedIncrement( &g_PuDbgEventsCreated );
    }

    return objHandle;

}   // PuDbgCreateEvent


HANDLE
PuDbgCreateSemaphore(
    __in LPSTR FileName,
    IN   ULONG LineNumber,
    __in LPSTR MemberName,
    IN   PVOID Address,
    IN   LONG InitialCount,
    IN   LONG MaximumCount
    )

/*++

Routine Description:

    Creates a new semaphore object.

Arguments:

    FileName - The filename of the source creating the object. This
        is __FILE__ of the caller.

    LineNumber - The line number within the source. This is __LINE__
        of the caller.

    MemberName - The member/global variable name where the object handle
        is to be stored.

    Address - The address of the containing structure/class or of the
        global itself.

    InitialCount - The initial count of the semaphore.

    MaximumCount - The maximum count of the semaphore.

Return Value:

    HANDLE - Handle to the object if successful, NULL otherwise.

--*/

{

    LPSTR objName;
    HANDLE objHandle;
    CHAR objNameBuffer[MAX_OBJECT_NAME];

    objName = PuDbgpBuildObjectName(
                  objNameBuffer,
                  sizeof( objNameBuffer) / sizeof( objNameBuffer[0] ),
                  FileName,
                  LineNumber,
                  MemberName,
                  Address
                  );

    objHandle = CreateSemaphoreA(
                    NULL,                       // lpSemaphoreAttributes
                    InitialCount,               // lInitialCount
                    MaximumCount,               // lMaximumCount
                    objName                     // lpName
                    );

    if( objHandle != NULL ) {
        InterlockedIncrement( &g_PuDbgSemaphoresCreated );
    }

    return objHandle;

}   // PuDbgCreateSemaphore


HANDLE
PuDbgCreateMutex(
    __in LPSTR FileName,
    IN   ULONG LineNumber,
    __in LPSTR MemberName,
    IN   PVOID Address,
    IN   BOOL InitialOwner
    )

/*++

Routine Description:

    Creates a new mutex object.

Arguments:

    FileName - The filename of the source creating the object. This
        is __FILE__ of the caller.

    LineNumber - The line number within the source. This is __LINE__
        of the caller.

    MemberName - The member/global variable name where the object handle
        is to be stored.

    Address - The address of the containing structure/class or of the
        global itself.

    InitialOwner - TRUE if the mutex should be created "owned".

Return Value:

    HANDLE - Handle to the object if successful, NULL otherwise.

--*/

{

    LPSTR objName;
    HANDLE objHandle;
    CHAR objNameBuffer[MAX_OBJECT_NAME];

    objName = PuDbgpBuildObjectName(
                  objNameBuffer,
                  sizeof( objNameBuffer) / sizeof( objNameBuffer[0] ),
                  FileName,
                  LineNumber,
                  MemberName,
                  Address
                  );

    objHandle = CreateMutexA(
                    NULL,                       // lpMutexAttributes
                    InitialOwner,               // bInitialOwner,
                    objName                     // lpName
                    );

    if( objHandle != NULL ) {
        InterlockedIncrement( &g_PuDbgMutexesCreated );
    }

    return objHandle;

}   // PuDbgCreateMutex

