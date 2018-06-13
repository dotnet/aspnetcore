// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include "stdafx.h"

class ConfigUtility
{
    #define CS_ASPNETCORE_HANDLER_SETTINGS                   L"handlerSettings"
    #define CS_ASPNETCORE_HANDLER_VERSION                    L"handlerVersion"
    #define CS_ASPNETCORE_HANDLER_SETTINGS_NAME              L"name"
    #define CS_ASPNETCORE_HANDLER_SETTINGS_VALUE             L"value"

public:
    static
    HRESULT
    FindHandlerVersion(IAppHostElement* pElement, STRU* strHandlerVersionValue)
    {
        HRESULT hr = S_OK;
        IAppHostElement                *pHandlerSettings = NULL;
        IAppHostElementCollection      *pHandlerSettingsCollection = NULL;
        ENUM_INDEX                      index;
        IAppHostElement                *pHandlerVar = NULL;
        STRU strHandlerName;
        STRU strHandlerValue;

        hr = GetElementChildByName(pElement,
            CS_ASPNETCORE_HANDLER_SETTINGS,
            &pHandlerSettings);
        if (FAILED(hr))
        {
            goto Finished;
        }

        hr = pHandlerSettings->get_Collection(&pHandlerSettingsCollection);
        if (FAILED(hr))
        {
            goto Finished;
        }

        for (hr = FindFirstElement(pHandlerSettingsCollection, &index, &pHandlerVar);
            SUCCEEDED(hr);
            hr = FindNextElement(pHandlerSettingsCollection, &index, &pHandlerVar))
        {
            if (hr == S_FALSE)
            {
                hr = S_OK;
                break;
            }

            hr = GetElementStringProperty(pHandlerVar,
                CS_ASPNETCORE_HANDLER_SETTINGS_NAME,
                &strHandlerName);

            if (FAILED(hr))
            {
                goto Finished;
            }

            hr = GetElementStringProperty(pHandlerVar,
                CS_ASPNETCORE_HANDLER_SETTINGS_VALUE,
                &strHandlerValue);

            if (FAILED(hr))
            {
                goto Finished;

            }

            if (strHandlerName.Equals(CS_ASPNETCORE_HANDLER_VERSION))
            {
                hr = strHandlerVersionValue->Copy(strHandlerValue);
                goto Finished;
            }

            strHandlerName.Reset();
            strHandlerValue.Reset();

            pHandlerVar->Release();
            pHandlerVar = NULL;
        }
    Finished:

        if (pHandlerVar != NULL)
        {
            pHandlerVar->Release();
            pHandlerVar = NULL;
        }

        if (pHandlerSettingsCollection != NULL)
        {
            pHandlerSettingsCollection->Release();
            pHandlerSettingsCollection = NULL;
        }

        if (pHandlerSettings != NULL)
        {
            pHandlerSettings->Release();
            pHandlerSettings = NULL;
        }

        return hr;
    }
};

