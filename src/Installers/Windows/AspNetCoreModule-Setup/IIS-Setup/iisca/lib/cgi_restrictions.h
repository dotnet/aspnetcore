// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.


#ifndef _CGI_RESTRICTIONS_H_
#define _CGI_RESTRICTIONS_H_


HRESULT
RegisterCgiRestriction(
    IN          IAppHostAdminManager *  pAdminMgr,
    IN          CONST WCHAR *           szConfigPath,
    IN          CONST WCHAR *           szPath,
    IN                BOOL              fAllowed,
    IN OPTIONAL CONST WCHAR *           szGroupId,
    IN OPTIONAL CONST WCHAR *           szDescription
    );


HRESULT
UnRegisterCgiRestriction(
    IN          IAppHostAdminManager *  pAdminMgr,
    IN          CONST WCHAR *           szConfigPath,
    IN          CONST WCHAR *           szPath,
    IN          BOOL                    fExpandPath = FALSE
    );


#endif

