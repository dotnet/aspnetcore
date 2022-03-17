// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
#include "stringu.h"
#include<Windows.h>

HRESULT
SetElementProperty(
    IN          IAppHostElement *   pElement,
    IN          CONST WCHAR *       szPropName,
    IN          CONST VARIANT *     varPropValue
    );

HRESULT
SetElementStringProperty(
    IN          IAppHostElement *   pElement,
    IN          CONST WCHAR *       szPropName,
    IN          CONST WCHAR *       szPropValue
    );

HRESULT
GetElementStringProperty(
    IN          IAppHostElement *   pElement,
    IN          CONST WCHAR *       szPropName,
    OUT         BSTR *              pbstrPropValue
    );

HRESULT
GetElementStringProperty(
    IN          IAppHostElement *   pElement,
    IN          CONST WCHAR *       szPropName,
    OUT         STRU *              pstrPropValue
    );

HRESULT
GetElementBoolProperty(
    IN IAppHostElement * pElement,
    IN LPCWSTR           pszPropertyName,
    OUT BOOL *           pBool
    );

HRESULT
GetElementBoolProperty(
    IN IAppHostElement * pElement,
    IN LPCWSTR           pszPropertyName,
    OUT bool *           pBool
    );

HRESULT
GetElementChildByName(
    IN IAppHostElement *    pElement,
    IN LPCWSTR              pszElementName,
    OUT IAppHostElement **  ppChildElement
    );

HRESULT
GetElementDWORDProperty(
    IN IAppHostElement * pElement,
    IN LPCWSTR           pszPropertyName,
    OUT DWORD *          pdwValue
    );

HRESULT
GetElementLONGLONGProperty(
    IN IAppHostElement * pElement,
    IN LPCWSTR           pszPropertyName,
    OUT LONGLONG *       pllValue
);


HRESULT
GetElementRawTimeSpanProperty(
    IN IAppHostElement * pElement,
    IN LPCWSTR           pszPropertyName,
    OUT ULONGLONG *      pulonglong
    );

constexpr auto FIND_ELEMENT_CASE_SENSITIVE = 0x00000000;
constexpr auto FIND_ELEMENT_CASE_INSENSITIVE = 0x00000001;

HRESULT
DeleteElementFromCollection(
    IAppHostElementCollection           *pCollection,
    CONST WCHAR *                       szKeyName,
    CONST WCHAR *                       szKeyValue,
    ULONG                               BehaviorFlags,
    BOOL *                              pfDeleted
    );

HRESULT
DeleteAllElementsFromCollection(
    IAppHostElementCollection           *pCollection,
    CONST WCHAR *                       szKeyName,
    CONST WCHAR *                       szKeyValue,
    ULONG                               BehaviorFlags,
    UINT *                              pNumDeleted
    );

HRESULT
FindElementInCollection(
    IAppHostElementCollection           *pCollection,
    CONST WCHAR *                       szKeyName,
    CONST WCHAR *                       szKeyValue,
    ULONG                               BehaviorFlags,
    OUT   ULONG *                       pIndex
    );

HRESULT
VariantAssign(
    IN OUT      VARIANT *       pv,
    IN          CONST WCHAR *   sz
    );

HRESULT
GetLocationFromFile(
    IN      IAppHostAdminManager *      pAdminMgr,
    IN      CONST WCHAR *               szConfigPath,
    IN      CONST WCHAR *               szLocationPath,
    OUT     IAppHostConfigLocation **   ppLocation,
    OUT     BOOL *                      pFound
    );

HRESULT
GetSectionFromLocation(
    IN      IAppHostConfigLocation *            pLocation,
    IN      CONST WCHAR *                       szSectionName,
    OUT     IAppHostElement **                  ppSectionElement,
    OUT     BOOL *                              pFound
    );

HRESULT
GetAdminElement(
    IN      IAppHostAdminManager *      pAdminMgr,
    IN      CONST WCHAR *               szConfigPath,
    IN      CONST WCHAR *               szElementName,
    OUT     IAppHostElement **          pElement
    );

HRESULT
ClearAdminElement(
    IN      IAppHostAdminManager *      pAdminMgr,
    IN      CONST WCHAR *               szConfigPath,
    IN      CONST WCHAR *               szElementName
    );

HRESULT
ClearElementFromAllSites(
    IN      IAppHostAdminManager *      pAdminMgr,
    IN      CONST WCHAR *               szConfigPath,
    IN      CONST WCHAR *               szElementName
    );

HRESULT
ClearElementFromAllLocations(
    IN      IAppHostAdminManager *      pAdminMgr,
    IN      CONST WCHAR *               szConfigPath,
    IN      CONST WCHAR *               szElementName
    );

HRESULT
ClearLocationElements(
    IN      IAppHostConfigLocation *    pLocation,
    IN      CONST WCHAR *               szElementName
    );

HRESULT
CompareElementName(
    IN      IAppHostElement *           pElement,
    IN      CONST WCHAR *               szNameToMatch,
    OUT     BOOL *                      pMatched
    );

HRESULT
ClearChildElementsByName(
    IN      IAppHostChildElementCollection *    pCollection,
    IN      CONST WCHAR *                       szElementName,
    OUT     BOOL *                              pFound
    );

HRESULT
GetSitesCollection(
    IN      IAppHostAdminManager *              pAdminMgr,
    IN      CONST WCHAR *                       szConfigPath,
    OUT     IAppHostElementCollection **        pSitesCollection
    );

HRESULT
GetLocationCollection(
    IN      IAppHostAdminManager *              pAdminMgr,
    IN      CONST WCHAR *                       szConfigPath,
    OUT     IAppHostConfigLocationCollection ** pLocationCollection
    );

struct ENUM_INDEX
{
    VARIANT Index;
    ULONG Count;
};

HRESULT
FindFirstElement(
    IN      IAppHostElementCollection *         pCollection,
    OUT     ENUM_INDEX *                        pIndex,
    OUT     IAppHostElement **                  pElement
    );

HRESULT
FindNextElement(
    IN      IAppHostElementCollection *         pCollection,
    IN OUT  ENUM_INDEX *                        pIndex,
    OUT     IAppHostElement **                  pElement
    );

HRESULT
FindFirstChildElement(
    IN      IAppHostChildElementCollection *    pCollection,
    OUT     ENUM_INDEX *                        pIndex,
    OUT     IAppHostElement **                  pElement
    );

HRESULT
FindNextChildElement(
    IN      IAppHostChildElementCollection *    pCollection,
    IN OUT  ENUM_INDEX *                        pIndex,
    OUT     IAppHostElement **                  pElement
    );

HRESULT
FindFirstLocation(
    IN      IAppHostConfigLocationCollection *  pCollection,
    OUT     ENUM_INDEX *                        pIndex,
    OUT     IAppHostConfigLocation **           pLocation
    );

HRESULT
FindNextLocation(
    IN      IAppHostConfigLocationCollection *  pCollection,
    IN OUT  ENUM_INDEX *                        pIndex,
    OUT     IAppHostConfigLocation **           pLocation
    );

HRESULT
FindFirstLocationElement(
    IN      IAppHostConfigLocation *            pLocation,
    OUT     ENUM_INDEX *                        pIndex,
    OUT     IAppHostElement **                  pElement
    );

HRESULT
FindNextLocationElement(
    IN      IAppHostConfigLocation *            pLocation,
    IN OUT  ENUM_INDEX *                        pIndex,
    OUT     IAppHostElement **                  pElement
    );

HRESULT GetSharedConfigEnabled(
    BOOL * pfIsSharedConfig
);
