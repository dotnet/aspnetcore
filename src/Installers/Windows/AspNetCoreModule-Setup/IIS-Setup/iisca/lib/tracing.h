// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once 

HRESULT
RegisterTraceArea(
    IN          CONST WCHAR *   szTraceProviderName,
    IN          CONST WCHAR *   szTraceProviderGuid,
    IN          CONST WCHAR *   szAreaName,
    IN          CONST WCHAR *   szAreaValue
);

HRESULT
GetElementFromCollection(
    IN  IAppHostElementCollection * pCollection,
    IN  CONST WCHAR *               szPropertyName,
    IN  CONST WCHAR *               szExpectedPropertyValue,
    OUT IAppHostElement **          ppElement,
    OUT DWORD *                     pIndex = NULL
);

struct NAME_VALUE_PAIR
{
    LPCWSTR         Name;
    CComVariant     Value;
};

HRESULT
AddElementToCollection(
    IN IAppHostElementCollection *  pCollection,
    IN CONST WCHAR *                pElementName,
    IN NAME_VALUE_PAIR              rgProperties[],
    IN DWORD                        cProperties,
    OUT IAppHostElement **          ppElement
);
