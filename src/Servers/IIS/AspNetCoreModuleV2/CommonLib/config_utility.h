// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include <httpserv.h>
#include "ahutil.h"
#include "stringu.h"
#include "exceptions.h"
#include "atlbase.h"

class ConfigUtility
{
    #define CS_ASPNETCORE_HANDLER_SETTINGS                   L"handlerSettings"
    #define CS_ASPNETCORE_HANDLER_VERSION                    L"handlerVersion"
    #define CS_ASPNETCORE_DEBUG_FILE                         L"debugFile"
    #define CS_ASPNETCORE_ENABLE_OUT_OF_PROCESS_CONSOLE_REDIRECTION L"enableOutOfProcessConsoleRedirection"
    #define CS_ASPNETCORE_FORWARD_RESPONSE_CONNECTION_HEADER L"forwardResponseConnectionHeader"
    #define CS_ASPNETCORE_DEBUG_LEVEL                        L"debugLevel"
    #define CS_ASPNETCORE_HANDLER_SETTINGS_NAME              L"name"
    #define CS_ASPNETCORE_HANDLER_SETTINGS_VALUE             L"value"

public:
    static
    HRESULT
    FindHandlerVersion(IAppHostElement* pElement, STRU& strHandlerVersionValue)
    {
        return FindKeyValuePair(pElement, CS_ASPNETCORE_HANDLER_VERSION, strHandlerVersionValue);
    }

    static
    HRESULT
    FindDebugFile(IAppHostElement* pElement, STRU& strDebugFile)
    {
        return FindKeyValuePair(pElement, CS_ASPNETCORE_DEBUG_FILE, strDebugFile);
    }

    static
    HRESULT
    FindDebugLevel(IAppHostElement* pElement, STRU& strDebugFile)
    {
        return FindKeyValuePair(pElement, CS_ASPNETCORE_DEBUG_LEVEL, strDebugFile);
    }

    static
    HRESULT
    FindEnableOutOfProcessConsoleRedirection(IAppHostElement* pElement, STRU& strEnableOutOfProcessConsoleRedirection)
    {
        return FindKeyValuePair(pElement, CS_ASPNETCORE_ENABLE_OUT_OF_PROCESS_CONSOLE_REDIRECTION, strEnableOutOfProcessConsoleRedirection);
    }

    static
    HRESULT
    FindForwardResponseConnectionHeader(IAppHostElement* pElement, STRU& strForwardResponseConnectionHeader)
    {
        return FindKeyValuePair(pElement, CS_ASPNETCORE_FORWARD_RESPONSE_CONNECTION_HEADER, strForwardResponseConnectionHeader);
    }

private:
    static
    HRESULT
    FindKeyValuePair(IAppHostElement* pElement, PCWSTR key, STRU& strHandlerVersionValue)
    {
        HRESULT hr;
        CComPtr<IAppHostElement>           pHandlerSettings = nullptr;
        CComPtr<IAppHostElementCollection> pHandlerSettingsCollection = nullptr;
        CComPtr<IAppHostElement>           pHandlerVar = nullptr;
        ENUM_INDEX                         index{};
        STRU strHandlerName;
        STRU strHandlerValue;

        // backwards complatibility with systems not having schema for handlerSettings
        if (FAILED_LOG(GetElementChildByName(pElement, CS_ASPNETCORE_HANDLER_SETTINGS, &pHandlerSettings)))
        {
            return S_OK;
        }

        RETURN_IF_FAILED(pHandlerSettings->get_Collection(&pHandlerSettingsCollection));

        RETURN_IF_FAILED(hr = FindFirstElement(pHandlerSettingsCollection, &index, &pHandlerVar));

        while (hr != S_FALSE)
        {
            RETURN_IF_FAILED(GetElementStringProperty(pHandlerVar, CS_ASPNETCORE_HANDLER_SETTINGS_NAME, &strHandlerName));
            RETURN_IF_FAILED(GetElementStringProperty(pHandlerVar, CS_ASPNETCORE_HANDLER_SETTINGS_VALUE, &strHandlerValue));

            if (strHandlerName.Equals(key, TRUE))
            {
                RETURN_IF_FAILED(strHandlerVersionValue.Copy(strHandlerValue));
                break;
            }

            strHandlerName.Reset();
            strHandlerValue.Reset();
            pHandlerVar.Release();

            RETURN_IF_FAILED(hr = FindNextElement(pHandlerSettingsCollection, &index, &pHandlerVar));
        }

        return S_OK;
    }
};

