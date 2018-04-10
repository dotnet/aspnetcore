// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#ifdef _ASSERTE
    #undef _ASSERTE
#endif

#ifdef ASSERT
    #undef ASSERT
#endif

#if defined( DBG ) && DBG
    #define SX_ASSERT( _x )         ( (VOID)( ( ( _x ) ) ? TRUE : ( __annotation( L"Debug", L"AssertFail", L#_x  ), DbgRaiseAssertionFailure(), FALSE ) ) )
    #define SX_ASSERTMSG( _m, _x )  ( (VOID)( ( ( _x ) ) ? TRUE : ( __annotation( L"Debug", L"AssertFail", L##_m ), DbgRaiseAssertionFailure(), FALSE ) ) )
    #define SX_VERIFY( _x )         SX_ASSERT( _x )
    #define _ASSERTE( _x )          SX_ASSERT( _x )
    #define ASSERT( _x )            SX_ASSERT( _x )
    #define assert( _x )            SX_ASSERT( _x )
    #define DBG_ASSERT( _x )        SX_ASSERT( _x )
    #define DBG_REQUIRE( _x )       SX_ASSERT( _x )
#else
    #define SX_ASSERT( _x )         ( (VOID)0 )
    #define SX_ASSERTMSG( _m, _x )  ( (VOID)0 )
    #define SX_VERIFY( _x )         ( (VOID)( ( _x ) ? TRUE : FALSE ) )
    #define _ASSERTE( _x )          ( (VOID)0 )
    #define assert( _x )            ( (VOID)0 )
    #define DBG_ASSERT( _x )        ( (VOID)0 )
    #define DBG_REQUIRE( _x )       ((VOID)(_x))
#endif

