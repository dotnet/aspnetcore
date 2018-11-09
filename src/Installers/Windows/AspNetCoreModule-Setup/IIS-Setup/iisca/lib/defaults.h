// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#ifndef _DEFAULTS_H_
#define _DEFAULTS_H_


//
// Helper routines for setting-up default module config.
//

HRESULT
CreateStreamFromTextResource(
    IN      HINSTANCE           hInstance,
    IN      CONST WCHAR *       szResourceName,
    OUT     IStream **          ppStream
    );

HRESULT
ResetConfigSection(
    IN      IAppHostWritableAdminManager *  pAdminMgr,
    IN      CONST WCHAR *                   szSectionName,
    IN OUT  IStream *                       pStreamDefaults
    );

HRESULT
ResetConfigSectionFromResource(
    IN      CONST WCHAR *       szResourceName,
    IN      CONST WCHAR *       szSectionName
    );

HRESULT
ResetConfigSectionFromFile(
    IN      CONST WCHAR *       szFileName,
    IN      CONST WCHAR *       szSectionName
    );

HRESULT
AppendConfigSectionFromFile(
    IN      CONST WCHAR *       szFileName,
    IN      CONST WCHAR *       szSectionName
    );

HRESULT
AppendConfigSection(
    IN      IAppHostWritableAdminManager *  pAdminMgr,
    IN      CONST WCHAR *                   szSectionName,
    IN OUT  IStream *                       pStreamDefaults
    );

#endif

