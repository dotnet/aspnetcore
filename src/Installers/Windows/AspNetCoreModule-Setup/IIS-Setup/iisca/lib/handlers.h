// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#ifndef _HANDLERS_H_
#define _HANDLERS_H_


#define HANDLER_INDEX_FIRST              0
#define HANDLER_INDEX_LAST               ((ULONG)(-1L))
#define HANDLER_INDEX_BEFORE_STATICFILE  ((ULONG)(-2L))

#define HANDLER_STATICFILE_NAME          L"StaticFile"

HRESULT
RegisterHandler(
    IN          IAppHostAdminManager *  pAdminMgr,
    IN          CONST WCHAR *           szConfigPath,
    IN          ULONG                   Index,
    IN          CONST WCHAR *           szName,
    IN          CONST WCHAR *           szPath,
    IN          CONST WCHAR *           szVerbs,
    IN OPTIONAL CONST WCHAR *           szType,
    IN OPTIONAL CONST WCHAR *           szModules,
    IN OPTIONAL CONST WCHAR *           szScriptProcessor,
    IN OPTIONAL CONST WCHAR *           szResourceType,
    IN OPTIONAL CONST WCHAR *           szRequiredAccess,
    IN OPTIONAL CONST WCHAR *           szPreCondition = NULL
    );

HRESULT
UnRegisterHandler(
    IN          IAppHostAdminManager *  pAdminMgr,
    IN          CONST WCHAR *           szConfigPath,
    IN          CONST WCHAR *           szName
    );

HRESULT
FindHandlerByName(
    IN          IAppHostAdminManager *  pAdminMgr,
    IN          CONST WCHAR *           szConfigPath,
    IN          CONST WCHAR *           szName,
    OUT         ULONG *                 pIndex
    );

HRESULT
GetHandlersCollection(
    IN      IAppHostAdminManager *              pAdminMgr,
    IN      CONST WCHAR *                       szConfigPath,
    OUT     IAppHostElementCollection **        pHandlersCollection
    );


#endif

