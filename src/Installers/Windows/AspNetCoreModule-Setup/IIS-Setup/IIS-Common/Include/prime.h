// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

#include <math.h>
#include <stdlib.h>

//
// Pre-calculated prime numbers (up to 10,049,369).
//
extern __declspec(selectany) const DWORD g_Primes [] = {
    3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631,
    761, 919, 1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103,
    12143, 14591, 17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631,
    130363, 156437, 187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403,
    968897, 1162687, 1395263, 1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559,
    5999471, 7199369, 7849369, 8649369, 9249369, 10049369
};

class PRIME
{
public:

    static
    DWORD
    GetPrime(
        DWORD dwMinimum
    )
    {
        //
        // Try to use the precalculated numbers.
        //
        for ( DWORD Index = 0; Index < _countof( g_Primes ); Index++ )
        {
            DWORD dwCandidate = g_Primes[Index];
            if ( dwCandidate >= dwMinimum )
            {
                return dwCandidate;
            }
        }

        //
        // Do calculation.
        //
        for ( DWORD dwCandidate = dwMinimum | 1;
             dwCandidate < MAXDWORD; 
             dwCandidate += 2 )
        {
            if ( IsPrime( dwCandidate ) )
            {
                return dwCandidate;
            }
        }
        return dwMinimum;
    }

private:

    static
    BOOL
    IsPrime(
        DWORD dwCandidate
    )
    {
        if ((dwCandidate & 1) == 0)
        {
            return ( dwCandidate == 2 );
        }

        DWORD dwMax = static_cast<DWORD>(sqrt(static_cast<double>(dwCandidate)));

        for ( DWORD Index = 3; Index <= dwMax; Index += 2 )
        {
            if ( (dwCandidate % Index) == 0 )
            {
                return FALSE;
            }
        }
        return TRUE;
    }

    PRIME() {}
    ~PRIME() {}
};