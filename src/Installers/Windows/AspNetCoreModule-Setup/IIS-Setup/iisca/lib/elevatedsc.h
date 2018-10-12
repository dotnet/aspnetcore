// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

UINT 
WINAPI 
ScheduleMakeShortcutElevatedCA(
    IN MSIHANDLE hInstall
    );

UINT 
WINAPI 
ExecuteMakeShortcutElevatedCA(
    IN MSIHANDLE hInstall
    );