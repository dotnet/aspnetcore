// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

class ENVIRONMENT_VAR_HELPERS
{

public:
    static
    VOID
    CopyToMultiSz(
        ENVIRONMENT_VAR_ENTRY *   pEntry,
        PVOID                     pvData
    )
    {
        STRU     strTemp;
        MULTISZ   *pMultiSz = static_cast<MULTISZ *>(pvData);
        DBG_ASSERT(pMultiSz);
        DBG_ASSERT(pEntry);
        strTemp.Copy(pEntry->QueryName());
        strTemp.Append(pEntry->QueryValue());
        pMultiSz->Append(strTemp.QueryStr());
    }

    static
    VOID
    CopyToTable(
        ENVIRONMENT_VAR_ENTRY *   pEntry,
        PVOID                     pvData
    )
    {
        // best effort copy, ignore the failure
        ENVIRONMENT_VAR_ENTRY *   pNewEntry = new ENVIRONMENT_VAR_ENTRY();
        if (pNewEntry != NULL)
        {
            pNewEntry->Initialize(pEntry->QueryName(), pEntry->QueryValue());
            ENVIRONMENT_VAR_HASH *pHash = static_cast<ENVIRONMENT_VAR_HASH *>(pvData);
            DBG_ASSERT(pHash);
            pHash->InsertRecord(pNewEntry);
            // Need to dereference as InsertRecord references it now
            pNewEntry->Dereference();
        }
    }

    static
    VOID
    AppendEnvironmentVariables
    (
        ENVIRONMENT_VAR_ENTRY *   pEntry,
        PVOID                     pvData
    )
    {
        HRESULT hr = S_OK;
        DWORD   dwResult = 0;
        DWORD   dwError = 0;
        STRU    struNameBuffer;
        STACK_STRU(struValueBuffer, 300);
        BOOL    fFound = FALSE;

        HRESULT* pHr = static_cast<HRESULT*>(pvData);

        // pEntry->QueryName includes the trailing =, remove it before calling stru
        if (FAILED(hr = struNameBuffer.Copy(pEntry->QueryName())))
        {
            goto Finished;
        }
        dwResult = struNameBuffer.LastIndexOf(L'=');
        if (dwResult != -1)
        {
            struNameBuffer.QueryStr()[dwResult] = L'\0';
            if (FAILED(hr = struNameBuffer.SyncWithBuffer()))
            {
                goto Finished;
            }
        }

        dwResult = GetEnvironmentVariable(struNameBuffer.QueryStr(), struValueBuffer.QueryStr(), struValueBuffer.QuerySizeCCH());
        if (dwResult == 0)
        {
            dwError = GetLastError();
            // Windows API (e.g., CreateProcess) allows variable with empty string value
            // in such case dwResult will be 0 and dwError will also be 0
            // As UI and CMD does not allow empty value, ignore this environment var
            if (dwError != ERROR_ENVVAR_NOT_FOUND && dwError != ERROR_SUCCESS)
            {
                hr = HRESULT_FROM_WIN32(dwError);
                goto Finished;
            }
        }
        else if (dwResult > struValueBuffer.QuerySizeCCH())
        {
            // have to increase the buffer and try get environment var again
            struValueBuffer.Reset();
            struValueBuffer.Resize(dwResult + (DWORD)wcslen(pEntry->QueryValue()) + 2); // for null char and semicolon
            dwResult = GetEnvironmentVariable(struNameBuffer.QueryStr(),
                struValueBuffer.QueryStr(),
                struValueBuffer.QuerySizeCCH());

            if (dwResult <= 0)
            {
                hr = HRESULT_FROM_WIN32(GetLastError());
                goto Finished;
            }
            fFound = TRUE;
        }
        else
        {
            fFound = TRUE;
        }

        if (FAILED(hr = struValueBuffer.SyncWithBuffer()))
        {
            goto Finished;
        }

        if (fFound)
        {
            if (FAILED(hr = struValueBuffer.Append(L";")))
            {
                goto Finished;
            }
        }
        if (FAILED(hr = struValueBuffer.Append(pEntry->QueryValue())))
        {
            goto Finished;
        }

        if (FAILED(hr = pEntry->Initialize(pEntry->QueryName(), struValueBuffer.QueryStr())))
        {
            goto Finished;
        }

    Finished:
        if (FAILED(hr))
        {
            *pHr = hr;
        }
        return;
    }

    static
    VOID
    SetEnvironmentVariables
    (
        ENVIRONMENT_VAR_ENTRY *   pEntry,
        PVOID                     pvData
    )
    {
        UNREFERENCED_PARAMETER(pvData);
        HRESULT hr = S_OK;
        DWORD dwResult = 0;
        STRU struNameBuffer;

        HRESULT* pHr = static_cast<HRESULT*>(pvData);

        // pEntry->QueryName includes the trailing =, remove it before calling SetEnvironmentVariable.
        if (FAILED(hr = struNameBuffer.Copy(pEntry->QueryName())))
        {
            goto Finished;
        }
        dwResult = struNameBuffer.LastIndexOf(L'=');
        if (dwResult != -1)
        {
            struNameBuffer.QueryStr()[dwResult] = L'\0';
            if (FAILED(hr = struNameBuffer.SyncWithBuffer()))
            {
                goto Finished;
            }
        }

        dwResult = SetEnvironmentVariable(struNameBuffer.QueryStr(), pEntry->QueryValue());
        if (dwResult == 0)
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
        }

    Finished:
        if (FAILED(hr))
        {
            *pHr = hr;
        }
        return;
    }

    static
    HRESULT
    InitEnvironmentVariablesTable
    (
        _In_ ENVIRONMENT_VAR_HASH*          pInEnvironmentVarTable,
        _In_ BOOL                           fWindowsAuthEnabled,
        _In_ BOOL                           fBasicAuthEnabled,
        _In_ BOOL                           fAnonymousAuthEnabled,
        _In_ PCWSTR                         pApplicationPhysicalPath,
        _Out_ ENVIRONMENT_VAR_HASH**        ppEnvironmentVarTable
    )
    {
        HRESULT hr = S_OK;
        BOOL    fFound = FALSE;
        DWORD   dwResult, dwError;
        STRU    strIisAuthEnvValue;
        STACK_STRU(strStartupAssemblyEnv, 1024);
        ENVIRONMENT_VAR_ENTRY* pHostingEntry = NULL;
        ENVIRONMENT_VAR_ENTRY* pIISAuthEntry = NULL;
        ENVIRONMENT_VAR_ENTRY* pIISPathEntry = NULL;
        ENVIRONMENT_VAR_HASH* pEnvironmentVarTable = NULL;

        pEnvironmentVarTable = new ENVIRONMENT_VAR_HASH();

        //
        // few environment variables expected, small bucket size for hash table
        //
        if (FAILED(hr = pEnvironmentVarTable->Initialize(37 /*prime*/)))
        {
            goto Finished;
        }

        // copy the envirable hash table (from configuration) to a temp one as we may need to remove elements
        pInEnvironmentVarTable->Apply(ENVIRONMENT_VAR_HELPERS::CopyToTable, pEnvironmentVarTable);
        if (pEnvironmentVarTable->Count() != pInEnvironmentVarTable->Count())
        {
            // hash table copy failed
            hr = E_UNEXPECTED;
            goto Finished;
        }

        pEnvironmentVarTable->FindKey((PWSTR)ASPNETCORE_IIS_PHYSICAL_PATH_ENV_STR, &pIISPathEntry);
        if (pIISPathEntry != NULL)
        {
            // user defined ASPNETCORE_IIS_PHYSICAL_PATH in configuration, wipe it off
            pIISPathEntry->Dereference();
            pEnvironmentVarTable->DeleteKey((PWSTR)ASPNETCORE_IIS_PHYSICAL_PATH_ENV_STR);
        }

        pIISPathEntry = new ENVIRONMENT_VAR_ENTRY();

        if (FAILED(hr = pIISPathEntry->Initialize(ASPNETCORE_IIS_PHYSICAL_PATH_ENV_STR, pApplicationPhysicalPath)) ||
            FAILED(hr = pEnvironmentVarTable->InsertRecord(pIISPathEntry)))
        {
            goto Finished;
        }

        pEnvironmentVarTable->FindKey((PWSTR)ASPNETCORE_IIS_AUTH_ENV_STR, &pIISAuthEntry);
        if (pIISAuthEntry != NULL)
        {
            // user defined ASPNETCORE_IIS_HTTPAUTH in configuration, wipe it off
            pIISAuthEntry->Dereference();
            pEnvironmentVarTable->DeleteKey((PWSTR)ASPNETCORE_IIS_AUTH_ENV_STR);
        }

        if (fWindowsAuthEnabled)
        {
            strIisAuthEnvValue.Copy(ASPNETCORE_IIS_AUTH_WINDOWS);
        }

        if (fBasicAuthEnabled)
        {
            strIisAuthEnvValue.Append(ASPNETCORE_IIS_AUTH_BASIC);
        }

        if (fAnonymousAuthEnabled)
        {
            strIisAuthEnvValue.Append(ASPNETCORE_IIS_AUTH_ANONYMOUS);
        }

        if (strIisAuthEnvValue.IsEmpty())
        {
            strIisAuthEnvValue.Copy(ASPNETCORE_IIS_AUTH_NONE);
        }

        pIISAuthEntry = new ENVIRONMENT_VAR_ENTRY();

        if (FAILED(hr = pIISAuthEntry->Initialize(ASPNETCORE_IIS_AUTH_ENV_STR, strIisAuthEnvValue.QueryStr())) ||
            FAILED(hr = pEnvironmentVarTable->InsertRecord(pIISAuthEntry)))
        {
            goto Finished;
        }

        // Compiler is complaining about conversion between PCWSTR and PWSTR here.
        // Explictly casting.
        pEnvironmentVarTable->FindKey((PWSTR)HOSTING_STARTUP_ASSEMBLIES_NAME, &pHostingEntry);
        if (pHostingEntry != NULL)
        {
            // user defined ASPNETCORE_HOSTINGSTARTUPASSEMBLIES in configuration
            // the value will be used in OutputEnvironmentVariables. Do nothing here
            pHostingEntry->Dereference();
            pHostingEntry = NULL;
            goto Skipped;
        }

        //check whether ASPNETCORE_HOSTINGSTARTUPASSEMBLIES is defined in system
        dwResult = GetEnvironmentVariable(HOSTING_STARTUP_ASSEMBLIES_ENV_STR,
            strStartupAssemblyEnv.QueryStr(),
            strStartupAssemblyEnv.QuerySizeCCH());
        if (dwResult == 0)
        {
            dwError = GetLastError();
            // Windows API (e.g., CreateProcess) allows variable with empty string value
            // in such case dwResult will be 0 and dwError will also be 0
            // As UI and CMD does not allow empty value, ignore this environment var
            if (dwError != ERROR_ENVVAR_NOT_FOUND && dwError != ERROR_SUCCESS)
            {
                hr = HRESULT_FROM_WIN32(dwError);
                goto Finished;
            }
        }
        else if (dwResult > strStartupAssemblyEnv.QuerySizeCCH())
        {
            // have to increase the buffer and try get environment var again
            strStartupAssemblyEnv.Reset();
            strStartupAssemblyEnv.Resize(dwResult + (DWORD)wcslen(HOSTING_STARTUP_ASSEMBLIES_VALUE) + 1);
            dwResult = GetEnvironmentVariable(HOSTING_STARTUP_ASSEMBLIES_ENV_STR,
                strStartupAssemblyEnv.QueryStr(),
                strStartupAssemblyEnv.QuerySizeCCH());
            if (dwResult <= 0)
            {
                hr = E_UNEXPECTED;
                goto Finished;
            }
            fFound = TRUE;
        }
        else
        {
            fFound = TRUE;
        }

        strStartupAssemblyEnv.SyncWithBuffer();
        if (strStartupAssemblyEnv.IndexOf(HOSTING_STARTUP_ASSEMBLIES_VALUE) == -1)
        {
        if (fFound)
        {
            strStartupAssemblyEnv.Append(L";");
        }
        strStartupAssemblyEnv.Append(HOSTING_STARTUP_ASSEMBLIES_VALUE);
        }

        // the environment variable was not defined, create it and add to hashtable
        pHostingEntry = new ENVIRONMENT_VAR_ENTRY();

        if (FAILED(hr = pHostingEntry->Initialize(HOSTING_STARTUP_ASSEMBLIES_NAME, strStartupAssemblyEnv.QueryStr())) ||
            FAILED(hr = pEnvironmentVarTable->InsertRecord(pHostingEntry)))
        {
            goto Finished;
        }

    Skipped:
        *ppEnvironmentVarTable = pEnvironmentVarTable;
        pEnvironmentVarTable = NULL;

    Finished:
        if (pHostingEntry != NULL)
        {
            pHostingEntry->Dereference();
            pHostingEntry = NULL;
        }

        if (pIISAuthEntry != NULL)
        {
            pIISAuthEntry->Dereference();
            pIISAuthEntry = NULL;
        }

        if (pEnvironmentVarTable != NULL)
        {
            pEnvironmentVarTable->Clear();
            delete pEnvironmentVarTable;
            pEnvironmentVarTable = NULL;
        }
        return hr;
    }

    static
    HRESULT
    AddWebsocketEnabledToEnvironmentVariables
    (
        _Inout_ ENVIRONMENT_VAR_HASH*       pInEnvironmentVarTable,
        _In_ BOOL                           fWebsocketsEnabled
    )
    {
        HRESULT hr = S_OK;
        ENVIRONMENT_VAR_ENTRY* pIISWebsocketEntry = NULL;
        STACK_STRU(strIISWebsocketEnvValue, 40);

        // We only need to set the WEBSOCKET_SUPPORTED environment variable for out of process
        pInEnvironmentVarTable->FindKey((PWSTR)ASPNETCORE_IIS_WEBSOCKETS_SUPPORTED_ENV_STR, &pIISWebsocketEntry);
        if (pIISWebsocketEntry != NULL)
        {
            // user defined ASPNETCORE_IIS_WEBSOCKETS in configuration, wipe it off
            pIISWebsocketEntry->Dereference();
            pInEnvironmentVarTable->DeleteKey((PWSTR)ASPNETCORE_IIS_WEBSOCKETS_SUPPORTED_ENV_STR);
        }
        // Set either true or false for the WebsocketEnvValue.
        if (fWebsocketsEnabled)
        {
            if (FAILED(hr = strIISWebsocketEnvValue.Copy(L"true")))
            {
                goto Finished;
            }
        }
        else
        {
            if (FAILED(hr = strIISWebsocketEnvValue.Copy(L"false")))
            {
                goto Finished;
            }
        }

        pIISWebsocketEntry = new ENVIRONMENT_VAR_ENTRY();

        if (FAILED(hr = pIISWebsocketEntry->Initialize(ASPNETCORE_IIS_WEBSOCKETS_SUPPORTED_ENV_STR, strIISWebsocketEnvValue.QueryStr())) ||
            FAILED(hr = pInEnvironmentVarTable->InsertRecord(pIISWebsocketEntry)))
        {
            goto Finished;
        }

    Finished:
        return hr;
    }
public:
    ENVIRONMENT_VAR_HELPERS();
    ~ENVIRONMENT_VAR_HELPERS();
};

