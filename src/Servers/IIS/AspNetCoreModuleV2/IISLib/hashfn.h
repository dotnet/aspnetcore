// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#ifndef __HASHFN_H__
#define __HASHFN_H__


// Produce a scrambled, randomish number in the range 0 to RANDOM_PRIME-1.
// Applying this to the results of the other hash functions is likely to
// produce a much better distribution, especially for the identity hash
// functions such as Hash(char c), where records will tend to cluster at
// the low end of the hashtable otherwise.  LKRhash applies this internally
// to all hash signatures for exactly this reason.

inline constexpr DWORD
HashScramble(DWORD dwHash)
{
    // Here are 10 primes slightly greater than 10^9
    //  1000000007, 1000000009, 1000000021, 1000000033, 1000000087,
    //  1000000093, 1000000097, 1000000103, 1000000123, 1000000181.

    // default value for "scrambling constant"
    constexpr DWORD RANDOM_CONSTANT = 314159269UL;
    // large prime number, also used for scrambling
    constexpr DWORD RANDOM_PRIME =   1000000007UL;

    return (RANDOM_CONSTANT * dwHash) % RANDOM_PRIME ;
}


// Faster scrambling function suggested by Eric Jacobsen

inline DWORD constexpr
HashRandomizeBits(DWORD dw)
{
	return (((dw * 1103515245 + 12345) >> 16)
            | ((dw * 69069 + 1) & 0xffff0000));
}


// Small prime number used as a multiplier in the supplied hash functions
constexpr DWORD HASH_MULTIPLIER = 101;

#undef HASH_SHIFT_MULTIPLY

#ifdef HASH_SHIFT_MULTIPLY
# define HASH_MULTIPLY(dw)   (((dw) << 7) - (dw))
#else
# define HASH_MULTIPLY(dw)   ((dw) * HASH_MULTIPLIER)
#endif

// Fast, simple hash function that tends to give a good distribution.
// Apply HashScramble to the result if you're using this for something
// other than LKRhash.

inline DWORD
HashString(
    const char* psz,
    DWORD       dwHash = 0)
{
    // force compiler to use unsigned arithmetic
    const unsigned char* upsz = (const unsigned char*) psz;

    for ( ; *upsz; ++upsz)
        dwHash = HASH_MULTIPLY(dwHash) + *upsz;

    return dwHash;
}

inline DWORD
HashString(
    __in_ecount(cch) const char* psz,
    __in DWORD cch,
    __in DWORD dwHash
)
{
    // force compiler to use unsigned arithmetic
    const unsigned char* upsz = (const unsigned char*) psz;

    for (DWORD Index = 0; 
         Index < cch;
         ++Index, ++upsz)
    {
        dwHash = HASH_MULTIPLY(dwHash) + *upsz;
    }

    return dwHash;
}


// Unicode version of above

inline DWORD
HashString(
    const wchar_t* pwsz,
    DWORD          dwHash = 0)
{
    for (  ;  *pwsz;  ++pwsz)
        dwHash = HASH_MULTIPLY(dwHash) + *pwsz;

    return dwHash;
}

// Based on length of the string instead of null-terminating character

inline DWORD
HashString(
    __in_ecount(cch) const wchar_t* pwsz,
    __in DWORD          cch,
    __in DWORD          dwHash
)
{
    for (DWORD Index = 0; 
         Index < cch;
         ++Index, ++pwsz)
    {
        dwHash = HASH_MULTIPLY(dwHash) + *pwsz;
    }

    return dwHash;
}


// Quick-'n'-dirty case-insensitive string hash function.
// Make sure that you follow up with _stricmp or _mbsicmp.  You should
// also cache the length of strings and check those first.  Caching
// an uppercase version of a string can help too.
// Again, apply HashScramble to the result if using with something other
// than LKRhash.
// Note: this is not really adequate for MBCS strings.

inline DWORD
HashStringNoCase(
    const char* psz,
    DWORD       dwHash = 0)
{
    const unsigned char* upsz = (const unsigned char*) psz;

    for (  ;  *upsz;  ++upsz)
        dwHash = HASH_MULTIPLY(dwHash)
                    + (*upsz & 0xDF);  // strip off lowercase bit

    return dwHash;
}

inline DWORD
HashStringNoCase(
    __in_ecount(cch)
    const char* psz,
    SIZE_T      cch,
    DWORD       dwHash)
{
    const unsigned char* upsz = (const unsigned char*) psz;

    for (SIZE_T Index = 0;
         Index < cch;
         ++Index, ++upsz)
    {
        dwHash = HASH_MULTIPLY(dwHash)
                    + (*upsz & 0xDF);  // strip off lowercase bit
    }
    return dwHash;
}


// Unicode version of above

inline DWORD
HashStringNoCase(
    const wchar_t* pwsz,
    DWORD          dwHash = 0)
{
    for (  ;  *pwsz;  ++pwsz)
        dwHash = HASH_MULTIPLY(dwHash) + (*pwsz & 0xFFDF);

    return dwHash;
}

// Unicode version of above with length

inline DWORD
HashStringNoCase(
    __in_ecount(cch)
    const wchar_t* pwsz,
    SIZE_T         cch,
    DWORD          dwHash)
{
    for (SIZE_T Index = 0;
         Index < cch;
         ++Index, ++pwsz)
    {
        dwHash = HASH_MULTIPLY(dwHash) + (*pwsz & 0xFFDF);
    }
    return dwHash;
}


// HashBlob returns the hash of a blob of arbitrary binary data.
// 
// Warning: HashBlob is generally not the right way to hash a class object.
// Consider:
//     class CFoo {
//     public:
//         char   m_ch;
//         double m_d;
//         char*  m_psz;
//     };
// 
//     inline DWORD Hash(const CFoo& rFoo)
//     { return HashBlob(&rFoo, sizeof(CFoo)); }
//
// This is the wrong way to hash a CFoo for two reasons: (a) there will be
// a 7-byte gap between m_ch and m_d imposed by the alignment restrictions
// of doubles, which will be filled with random data (usually non-zero for
// stack variables), and (b) it hashes the address (rather than the
// contents) of the string m_psz.  Similarly,
// 
//     bool operator==(const CFoo& rFoo1, const CFoo& rFoo2)
//     { return memcmp(&rFoo1, &rFoo2, sizeof(CFoo)) == 0; }
//
// does the wrong thing.  Much better to do this:
//
//     DWORD Hash(const CFoo& rFoo)
//     {
//         return HashString(rFoo.m_psz,
//                           HASH_MULTIPLIER * Hash(rFoo.m_ch)
//                              + Hash(rFoo.m_d));
//     }
//
// Again, apply HashScramble if using with something other than LKRhash.

inline DWORD
HashBlob(
    const void* pv,
    size_t      cb,
    DWORD       dwHash = 0)
{
    const BYTE * pb = static_cast<const BYTE *>(pv);

    while (cb-- > 0)
        dwHash = HASH_MULTIPLY(dwHash) + *pb++;

    return dwHash;
}



//
// Overloaded hash functions for all the major builtin types.
// Again, apply HashScramble to result if using with something other than
// LKRhash.
//

inline DWORD Hash(const char* psz)
{ return HashString(psz); }

inline DWORD Hash(const unsigned char* pusz)
{ return HashString(reinterpret_cast<const char*>(pusz)); }

inline DWORD Hash(const signed char* pssz)
{ return HashString(reinterpret_cast<const char*>(pssz)); }

inline DWORD Hash(const wchar_t* pwsz)
{ return HashString(pwsz); }

inline DWORD
Hash(
    const GUID* pguid,
    DWORD       dwHash = 0)
{
    
    return * reinterpret_cast<const DWORD *>(const_cast<GUID*>(pguid)) + dwHash;
}

// Identity hash functions: scalar values map to themselves
inline constexpr DWORD Hash(char c)
{ return c; }

inline constexpr DWORD Hash(unsigned char uc)
{ return uc; }

inline constexpr DWORD Hash(signed char sc)
{ return sc; }

inline constexpr DWORD Hash(short sh)
{ return sh; }

inline constexpr DWORD Hash(unsigned short ush)
{ return ush; }

inline constexpr DWORD Hash(int i)
{ return i; }

inline constexpr DWORD Hash(unsigned int u)
{ return u; }

inline constexpr DWORD Hash(long l)
{ return l; }

inline constexpr DWORD Hash(unsigned long ul)
{ return ul; }

inline constexpr DWORD Hash(float f)
{
    // be careful of rounding errors when computing keys
    union {
        float f;
        DWORD dw;
    } u{};
    u.f = f;
    return u.dw;
}

inline constexpr DWORD Hash(double dbl)
{
    // be careful of rounding errors when computing keys
    union {
        double dbl;
        DWORD  dw[2];
    } u{};
    u.dbl = dbl;
    return u.dw[0] * HASH_MULTIPLIER + u.dw[1];
}

#endif // __HASHFN_H__
