// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include <string>

// Note: This is not SemVer (esp., in comparing pre-release part, fx_ver_t does not
// compare multiple dot separated identifiers individually.) ex: 1.0.0-beta.2 vs. 1.0.0-beta.11
struct fx_ver_t
{
    fx_ver_t(int major, int minor, int patch);
    fx_ver_t(int major, int minor, int patch, const std::wstring& pre);
    fx_ver_t(int major, int minor, int patch, const std::wstring& pre, const std::wstring& build);

    int get_major() const noexcept { return m_major; }
    int get_minor() const noexcept { return m_minor; }
    int get_patch() const noexcept { return m_patch; }

    void set_major(int m) noexcept { m_major = m; }
    void set_minor(int m) noexcept { m_minor = m; }
    void set_patch(int p) noexcept { m_patch = p; }

    bool is_prerelease() const noexcept { return !m_pre.empty(); }

    std::wstring as_str() const;

    bool operator ==(const fx_ver_t& b) const;
    bool operator !=(const fx_ver_t& b) const;
    bool operator <(const fx_ver_t& b) const;
    bool operator >(const fx_ver_t& b) const;
    bool operator <=(const fx_ver_t& b) const;
    bool operator >=(const fx_ver_t& b) const;

    static bool parse(const std::wstring& ver, fx_ver_t* fx_ver, bool parse_only_production = false);

private:
    int m_major;
    int m_minor;
    int m_patch;
    std::wstring m_pre;
    std::wstring m_build;

    static int compare(const fx_ver_t&a, const fx_ver_t& b);
};
