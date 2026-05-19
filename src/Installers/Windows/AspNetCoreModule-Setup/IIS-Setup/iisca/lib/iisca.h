// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

#define _UNITEXT(quote) L##quote
#define UNITEXT(quote) _UNITEXT(quote)


// IIS Custom action Types
enum IIS_COSTOM_ACTION_TYPE
{
    IIS_INSTALL_MODULE = 1,
    IIS_UNINSTALL_MODULE,
    IIS_INSTALL_UIMODULE,
    IIS_UNINSTALL_UIMODULE,
    IIS_INSTALL_HANDLER,
    IIS_UNINSTALL_HANDLER,
    IIS_INSTALL_SECTIONSCHEMA,
    IIS_UNINSTALL_SECTIONSCHEMA,
    IIS_INSTALL_TRACEAREA,
    IIS_INSTALL_MOFFILE,
    IIS_INSTALL_DEFAULTS,
    IIS_INSTALL_SECTION_ADDITIONS,
    IIS_INSTALL_CGIRESTRICTIONS,
    IIS_UNINSTALL_CGIRESTRICTIONS,
    IIS_INSTALL_,
    IIS_UNINSTALL_,
    IIS_END
};


HRESULT
InstallModule(
    IN          CONST WCHAR *   szName,
    IN          CONST WCHAR *   szImage,
    IN OPTIONAL CONST WCHAR *   szPreCondition,
    IN OPTIONAL CONST WCHAR *   szTYpe
    );


HRESULT
UnInstallModule(
    IN          CONST WCHAR *   szName,
    IN OPTIONAL CONST WCHAR *   szPreCondition
    );

HRESULT
RegisterSectionSchema(
    IN           CONST BOOL     isSectionInAdminSchema,
    IN           CONST WCHAR *  szSectionName,
    IN           CONST WCHAR *  szOverideModeDefault,
    IN  OPTIONAL CONST WCHAR *  szAllowDefinition,
    IN  OPTIONAL CONST WCHAR *  szType
    );

HRESULT
UnRegisterSectionSchema(
    IN  CONST BOOL      isSectionInAdminSchema,
    IN  CONST WCHAR *   szSectionName
    );

HRESULT
RegisterUIModule(
    IN          CONST WCHAR *   szModuleName,
    IN          CONST WCHAR *   szModuleTypeInfo,
    IN OPTIONAL CONST WCHAR *   szRegisterInModulesSection,
    IN OPTIONAL CONST WCHAR *   szPrependToList
    );

HRESULT
UnRegisterUIModule(
    IN          CONST WCHAR *   szModuleName,
    IN          CONST WCHAR *   szModuleTypeInfo
    );

UINT 
WINAPI 
CheckForAdminSIDCA(
    MSIHANDLE hInstall
    );

UINT
LogMsiCustomActionError(
    IN MSIHANDLE hInstall,
    UINT messageId
    ); 

HRESULT
InitAdminMgrForAdminConfig(
    IN IAppHostWritableAdminManager *        pAdminMgr,
    IN CONST WCHAR *                         szCommitPath
);

HRESULT
RegisterMofFile(
    IN PWSTR                    pszFileName
);

HRESULT
ScheduleInstallModuleCA(
    IN  MSIHANDLE   hInstall,
    IN  CA_DATA_WRITER  * cadata
);

HRESULT
ScheduleUnInstallModuleCA(
    IN  MSIHANDLE   hInstall,
    IN CA_DATA_WRITER  * cadata   
);

HRESULT
ExecuteInstallModuleCA(
    IN CA_DATA_READER  * cadata    
);

HRESULT
ExecuteUnInstallModuleCA(
    IN  CA_DATA_READER * cadata
);


HRESULT
ExecuteUnRegisterUIModuleCA(
    IN  CA_DATA_READER * cadata
);


HRESULT
ScheduleRegisterUIModuleCA(
    IN  MSIHANDLE   hInstall,
    IN  CA_DATA_WRITER  * cadata
);
HRESULT
ExecuteRegisterUIModuleCA(
    IN CA_DATA_READER  * cadata    
);

HRESULT
ScheduleUnRegisterUIModuleCA(
    IN  MSIHANDLE   hInstall,
    IN CA_DATA_WRITER  * cadata   
);


HRESULT
ScheduleInstallHandlerCA(
    IN  MSIHANDLE   hInstall,
    IN CA_DATA_WRITER  * cadata 
    );

HRESULT
ScheduleUnInstallHandlerCA(
    IN  MSIHANDLE   hInstall,
    IN CA_DATA_WRITER  * cadata 
    );
        
HRESULT
ExecuteInstallHandlerCA(
    IN  CA_DATA_READER * cadata
    );

HRESULT
ExecuteUnInstallHandlerCA(
    IN  CA_DATA_READER * cadata
    );    

HRESULT
ExecuteUnRegisterSectionSchemaCA(
    IN  CA_DATA_READER * cadata
);


HRESULT
ScheduleRegisterSectionSchemaCA(
    IN  MSIHANDLE   hInstall,
    IN  CA_DATA_WRITER  * cadata
);

HRESULT
ExecuteRegisterSectionSchemaCA(
    IN CA_DATA_READER  * cadata    
);

HRESULT
ScheduleUnRegisterSectionSchemaCA(
    IN  MSIHANDLE   hInstall,
    IN CA_DATA_WRITER  * cadata   
);

HRESULT
ScheduleRegisterTraceAreaCA(
    IN  MSIHANDLE   hInstall,
    IN  CA_DATA_WRITER * cadata
);

HRESULT
ExecuteRegisterTraceAreaCA(
    IN  CA_DATA_READER * cadata
);

HRESULT
ScheduleRegisterMofFileCA(
    IN  MSIHANDLE   hInstall,
    IN  CA_DATA_WRITER * cadata
);

HRESULT
ExecuteRegisterMofFileCA(
    IN  CA_DATA_READER * cadata
);

HRESULT
ScheduleInstallSectionDefaultsCA(
    IN  MSIHANDLE   hInstall,
    IN  CA_DATA_WRITER * cadata
);

HRESULT
ExecuteInstallSectionDefaultsCA(
    IN  CA_DATA_READER * cadata
);

HRESULT
ScheduleInstallSectionAdditionsCA(
    IN  MSIHANDLE   hInstall,
    IN  CA_DATA_WRITER * cadata
);

HRESULT
ExecuteInstallSectionAdditionsCA(
    IN  CA_DATA_READER * cadata
);

HRESULT
ScheduleInstallCgiRestrictionsCA(
    IN  MSIHANDLE   hInstall,
    IN  CA_DATA_WRITER * cadata
);

HRESULT
ScheduleUnInstallCgiRestrictionsCA(
    IN  MSIHANDLE   hInstall,
    IN  CA_DATA_WRITER * cadata
);
    
HRESULT
ExecuteInstallCgiRestrictionsCA(
    IN  CA_DATA_READER * cadata
);    

HRESULT
ExecuteUnInstallCgiRestrictionsCA(
    IN  CA_DATA_READER * cadata
);  
