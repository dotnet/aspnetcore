// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#define DEFAULT_HASH_BUCKETS 293

class APPLICATION_MANAGER
{
public:

    static 
    APPLICATION_MANAGER*
    GetInstance(
        VOID
    )
    {
        if( sm_pApplicationManager == NULL )
        {
            sm_pApplicationManager = new APPLICATION_MANAGER();
        }

        return  sm_pApplicationManager;
    }

    static
    VOID
    Cleanup(
        VOID
    )
    {
        if(sm_pApplicationManager != NULL)
        {
            delete sm_pApplicationManager;
            sm_pApplicationManager = NULL;
        }
    }

    HRESULT
    GetApplication(
        _In_ IHttpContext*         pContext,
        _Out_ APPLICATION **       ppApplication
    );

    HRESULT
    RecycleApplication( 
        _In_ LPCWSTR pszApplication 
    );

    HRESULT
    Get502ErrorPage(
        _Out_ HTTP_DATA_CHUNK**     ppErrorPage
    );

    ~APPLICATION_MANAGER()
    {
        if(m_pApplicationHash != NULL)
        {
            m_pApplicationHash->Clear();
            delete m_pApplicationHash;
            m_pApplicationHash = NULL;
        }

        if( m_pFileWatcher!= NULL )
        {
            delete m_pFileWatcher;
            m_pFileWatcher = NULL;
        }

        if(m_pHttp502ErrorPage != NULL)
        {
            delete m_pHttp502ErrorPage;
            m_pHttp502ErrorPage = NULL;
        }

    }

    FILE_WATCHER*
    GetFileWatcher()
    {
        return m_pFileWatcher;
    }

    HRESULT Initialize()
    {
        HRESULT hr = S_OK;

        if(m_pApplicationHash == NULL)
        {
            m_pApplicationHash = new APPLICATION_HASH();
            if(m_pApplicationHash == NULL)
            {
                hr = E_OUTOFMEMORY;
                goto Finished;
            }

            hr = m_pApplicationHash->Initialize(DEFAULT_HASH_BUCKETS);
            if(FAILED(hr))
            {
                goto Finished;
            }
        }

        if( m_pFileWatcher == NULL )
        {
            m_pFileWatcher = new FILE_WATCHER;
            if(m_pFileWatcher == NULL)
            {
                hr = HRESULT_FROM_WIN32( ERROR_NOT_ENOUGH_MEMORY );
                goto Finished;
            }

            m_pFileWatcher->Create();
        }

    Finished:
        return hr;
    }

private:
    //
    // we currently limit the size of m_pstrErrorInfo to 5000, be careful if you want to change its payload
    // 
    APPLICATION_MANAGER() : m_pApplicationHash(NULL), m_pFileWatcher(NULL), m_pHttp502ErrorPage(NULL), m_pstrErrorInfo(
        "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\"> \
        <html xmlns=\"http://www.w3.org/1999/xhtml\"> \
        <head> \
        <meta http-equiv=\"Content-Type\" content=\"text/html; charset=iso-8859-1\" /> \
        <title> IIS 502.5 Error </title><style type=\"text/css\"></style></head> \
        <body> <div id = \"content\"> \
          <div class = \"content-container\"><h3> HTTP Error 502.5 - Process Failure </h3></div>  \
          <div class = \"content-container\"> \
           <fieldset> <h4> Common causes of this issue: </h4> \
            <ul><li> The application process failed to start </li> \
             <li> The application process started but then stopped </li> \
             <li> The application process started but failed to listen on the configured port </li></ul></fieldset> \
          </div> \
          <div class = \"content-container\"> \
            <fieldset><h4> Troubleshooting steps: </h4> \
             <ul><li> Check the system event log for error messages </li> \
             <li> Enable logging the application process' stdout messages </li> \
             <li> Attach a debugger to the application process and inspect </li></ul></fieldset> \
             <fieldset><h4> For more information visit: \
             <a href=\"https://go.microsoft.com/fwlink/?linkid=808681\"> <cite> https://go.microsoft.com/fwlink/?LinkID=808681 </cite></a></h4> \
             </fieldset> \
          </div> \
       </div></body></html>")
    {
        InitializeSRWLock(&m_srwLock);
    }

    FILE_WATCHER               *m_pFileWatcher;
    APPLICATION_HASH           *m_pApplicationHash;
    static APPLICATION_MANAGER *sm_pApplicationManager;
    SRWLOCK                     m_srwLock;
    HTTP_DATA_CHUNK            *m_pHttp502ErrorPage;
    LPSTR                      m_pstrErrorInfo;
};
