// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <cassert>
#include <string>
#include "fx_ver.h"

namespace aspnet
{
    static bool validIdentifiers(const std::wstring& ids);

    size_t index_of_non_numeric(const std::wstring& str, size_t i)
    {
        return str.find_first_not_of(TEXT("0123456789"), i);
    }

    bool try_stou(const std::wstring& str, unsigned* num)
    {
        if (str.empty())
        {
            return false;
        }
        if (index_of_non_numeric(str, 0) != std::wstring::npos)
        {
            return false;
        }
        *num = (unsigned)std::stoul(str);
        return true;
    }

    fx_ver_t::fx_ver_t(int major, int minor, int patch, const std::wstring& pre, const std::wstring& build)
        : m_major(major)
        , m_minor(minor)
        , m_patch(patch)
        , m_pre(pre)
        , m_build(build)
    {
        // verify preconditions
        assert(is_empty() || m_major >= 0);
        assert(is_empty() || m_minor >= 0);
        assert(is_empty() || m_patch >= 0);
        assert(m_pre[0] == 0 || validIdentifiers(m_pre));
        assert(m_build[0] == 0 || validIdentifiers(m_build));
    }

    fx_ver_t::fx_ver_t(int major, int minor, int patch, const std::wstring& pre)
        : fx_ver_t(major, minor, patch, pre, TEXT(""))
    {
    }

    fx_ver_t::fx_ver_t(int major, int minor, int patch)
        : fx_ver_t(major, minor, patch, TEXT(""), TEXT(""))
    {
    }

    fx_ver_t::fx_ver_t()
        : fx_ver_t(-1, -1, -1, TEXT(""), TEXT(""))
    {
    }

    bool fx_ver_t::operator ==(const fx_ver_t& b) const
    {
        return compare(*this, b) == 0;
    }

    bool fx_ver_t::operator !=(const fx_ver_t& b) const
    {
        return !operator ==(b);
    }

    bool fx_ver_t::operator <(const fx_ver_t& b) const
    {
        return compare(*this, b) < 0;
    }

    bool fx_ver_t::operator >(const fx_ver_t& b) const
    {
        return compare(*this, b) > 0;
    }

    bool fx_ver_t::operator <=(const fx_ver_t& b) const
    {
        return compare(*this, b) <= 0;
    }

    bool fx_ver_t::operator >=(const fx_ver_t& b) const
    {
        return compare(*this, b) >= 0;
    }

    std::wstring fx_ver_t::as_str() const
    {
        std::wstringstream stream;
        stream << m_major << TEXT(".") << m_minor << TEXT(".") << m_patch;
        if (!m_pre.empty())
        {
            stream << m_pre;
        }
        if (!m_build.empty())
        {
            stream << m_build;
        }
        return stream.str();
    }

    std::wstring fx_ver_t::prerelease_glob() const
    {
        std::wstringstream stream;
        stream << m_major << TEXT(".") << m_minor << TEXT(".") << m_patch << TEXT("-*");
        return stream.str();
    }

    std::wstring fx_ver_t::patch_glob() const
    {
        std::wstringstream stream;
        stream << m_major << TEXT(".") << m_minor << TEXT(".*");
        return stream.str();
    }

    static std::wstring getId(const std::wstring& ids, size_t idStart)
    {
        size_t next = ids.find(TEXT('.'), idStart);

        return next == std::wstring::npos ? ids.substr(idStart) : ids.substr(idStart, next - idStart);
    }

    /* static */
    int fx_ver_t::compare(const fx_ver_t& a, const fx_ver_t& b)
    {
        // compare(u.v.w-p+b, x.y.z-q+c)
        if (a.m_major != b.m_major)
        {
            return (a.m_major > b.m_major) ? 1 : -1;
        }

        if (a.m_minor != b.m_minor)
        {
            return (a.m_minor > b.m_minor) ? 1 : -1;
        }

        if (a.m_patch != b.m_patch)
        {
            return (a.m_patch > b.m_patch) ? 1 : -1;
        }

        if (a.m_pre.empty() || b.m_pre.empty())
        {
            // Either a is empty or b is empty or both are empty
            return a.m_pre.empty() ? !b.m_pre.empty() : -1;
        }

        // Both are non-empty (may be equal)

        // First character of pre is '-' when it is not empty
        assert(a.m_pre[0] == TEXT('-'));
        assert(b.m_pre[0] == TEXT('-'));

        // First idenitifier starts at position 1
        size_t idStart = 1;
        for (size_t i = idStart; true; ++i)
        {
            if (a.m_pre[i] != b.m_pre[i])
            {
                // Found first character with a difference
                if (a.m_pre[i] == 0 && b.m_pre[i] == TEXT('.'))
                {
                    // identifiers both complete, b has an additional idenitifier
                    return -1;
                }

                if (b.m_pre[i] == 0 && a.m_pre[i] == TEXT('.'))
                {
                    // identifiers both complete, a has an additional idenitifier
                    return 1;
                }

                // identifiers must not be empty
                std::wstring ida = getId(a.m_pre, idStart);
                std::wstring idb = getId(b.m_pre, idStart);

                unsigned idanum = 0;
                bool idaIsNum = try_stou(ida, &idanum);
                unsigned idbnum = 0;
                bool idbIsNum = try_stou(idb, &idbnum);

                if (idaIsNum && idbIsNum)
                {
                    // Numeric comparison
                    return (idanum > idbnum) ? 1 : -1;
                }
                else if (idaIsNum || idbIsNum)
                {
                    // Mixed compare.  Spec: Number < Text
                    return idbIsNum ? 1 : -1;
                }
                // Ascii compare
                return ida.compare(idb);
            }
            else
            {
                // a.m_pre[i] == b.m_pre[i]
                if (a.m_pre[i] == 0)
                {
                    break;
                }
                if (a.m_pre[i] == TEXT('.'))
                {
                    idStart = i + 1;
                }
            }
        }

        return 0;
    }

    static bool validIdentifierCharSet(const std::wstring& id)
    {
        // ids must be of the set [0-9a-zA-Z-]

        // ASCII and Unicode ordering
        static_assert(TEXT('-') < TEXT('0'), "Code assumes ordering - < 0 < 9 < A < Z < a < z");
        static_assert(TEXT('0') < TEXT('9'), "Code assumes ordering - < 0 < 9 < A < Z < a < z");
        static_assert(TEXT('9') < TEXT('A'), "Code assumes ordering - < 0 < 9 < A < Z < a < z");
        static_assert(TEXT('A') < TEXT('Z'), "Code assumes ordering - < 0 < 9 < A < Z < a < z");
        static_assert(TEXT('Z') < TEXT('a'), "Code assumes ordering - < 0 < 9 < A < Z < a < z");
        static_assert(TEXT('a') < TEXT('z'), "Code assumes ordering - < 0 < 9 < A < Z < a < z");

        for (size_t i = 0; id[i] != 0; ++i)
        {
            if (id[i] >= TEXT('A'))
            {
                if ((id[i] > TEXT('Z') && id[i] < TEXT('a')) || id[i] > TEXT('z'))
                {
                    return false;
                }
            }
            else
            {
                if ((id[i] < TEXT('0') && id[i] != TEXT('-')) || id[i] > TEXT('9'))
                {
                    return false;
                }
            }
        }
        return true;
    }

    static bool validIdentifier(const std::wstring& id, bool buildMeta)
    {
        if (id.empty())
        {
            // Identifier must not be empty
            return false;
        }

        if (!validIdentifierCharSet(id))
        {
            // ids must be of the set [0-9a-zA-Z-]
            return false;
        }

        if (!buildMeta && id[0] == TEXT('0') && id[1] != 0 && index_of_non_numeric(id, 1) == std::wstring::npos)
        {
            // numeric identifiers must not be padded with 0s
            return false;
        }
        return true;
    }

    static bool validIdentifiers(const std::wstring& ids)
    {
        if (ids.empty())
        {
            return true;
        }

        bool prerelease = ids[0] == TEXT('-');
        bool buildMeta = ids[0] == TEXT('+');

        if (!(prerelease || buildMeta))
        {
            // ids must start with '-' or '+' for prerelease & build respectively
            return false;
        }

        size_t idStart = 1;
        size_t nextId;
        while ((nextId = ids.find(TEXT('.'), idStart)) != std::wstring::npos)
        {
            if (!validIdentifier(ids.substr(idStart, nextId - idStart), buildMeta))
            {
                return false;
            }
            idStart = nextId + 1;
        }

        if (!validIdentifier(ids.substr(idStart), buildMeta))
        {
            return false;
        }

        return true;
    }

    bool parse_internal(const std::wstring& ver, fx_ver_t* fx_ver, bool parse_only_production)
    {
        size_t maj_start = 0;
        size_t maj_sep = ver.find(TEXT('.'));
        if (maj_sep == std::wstring::npos)
        {
            return false;
        }
        unsigned major = 0;
        if (!try_stou(ver.substr(maj_start, maj_sep), &major))
        {
            return false;
        }
        if (maj_sep > 1 && ver[maj_start] == TEXT('0'))
        {
            // if leading character is 0, and strlen > 1
            // then the numeric substring has leading zeroes which is prohibited by the specification.
            return false;
        }

        size_t min_start = maj_sep + 1;
        size_t min_sep = ver.find(TEXT('.'), min_start);
        if (min_sep == std::wstring::npos)
        {
            return false;
        }

        unsigned minor = 0;
        if (!try_stou(ver.substr(min_start, min_sep - min_start), &minor))
        {
            return false;
        }
        if (min_sep - min_start > 1 && ver[min_start] == TEXT('0'))
        {
            // if leading character is 0, and strlen > 1
            // then the numeric substring has leading zeroes which is prohibited by the specification.
            return false;
        }

        unsigned patch = 0;
        size_t pat_start = min_sep + 1;
        size_t pat_sep = index_of_non_numeric(ver, pat_start);
        if (pat_sep == std::wstring::npos)
        {
            if (!try_stou(ver.substr(pat_start), &patch))
            {
                return false;
            }
            if (ver[pat_start + 1] != 0 && ver[pat_start] == TEXT('0'))
            {
                // if leading character is 0, and strlen != 1
                // then the numeric substring has leading zeroes which is prohibited by the specification.
                return false;
            }

            *fx_ver = fx_ver_t(major, minor, patch);
            return true;
        }

        if (parse_only_production)
        {
            // This is a prerelease or has build suffix.
            return false;
        }

        if (!try_stou(ver.substr(pat_start, pat_sep - pat_start), &patch))
        {
            return false;
        }
        if (pat_sep - pat_start > 1 && ver[pat_start] == TEXT('0'))
        {
            return false;
        }

        size_t pre_start = pat_sep;
        size_t pre_sep = ver.find(TEXT('+'), pat_sep);

        std::wstring pre = (pre_sep == std::wstring::npos) ? ver.substr(pre_start) : ver.substr(pre_start, pre_sep - pre_start);

        if (!validIdentifiers(pre))
        {
            return false;
        }

        std::wstring build;

        if (pre_sep != std::wstring::npos)
        {
            build = ver.substr(pre_sep);

            if (!validIdentifiers(build))
            {
                return false;
            }
        }

        *fx_ver = fx_ver_t(major, minor, patch, pre, build);
        return true;
    }

    /* static */
    bool fx_ver_t::parse(const std::wstring& ver, fx_ver_t* fx_ver, bool parse_only_production)
    {
        bool valid = parse_internal(ver, fx_ver, parse_only_production);
        assert(!valid || fx_ver->as_str() == ver);
        return valid;
    }
}
