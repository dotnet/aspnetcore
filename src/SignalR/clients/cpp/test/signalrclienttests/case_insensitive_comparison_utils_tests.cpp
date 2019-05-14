// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "case_insensitive_comparison_utils.h"

using namespace signalr;

TEST(case_insensitive_equals_functor, basic_comparison_tests)
{
    case_insensitive_equals case_insensitive_compare;

    ASSERT_TRUE(case_insensitive_compare(_XPLATSTR(""), _XPLATSTR("")));
    ASSERT_TRUE(case_insensitive_compare(_XPLATSTR("abc"), _XPLATSTR("ABC")));
    ASSERT_TRUE(case_insensitive_compare(_XPLATSTR("abc123!@"), _XPLATSTR("ABC123!@")));

    ASSERT_FALSE(case_insensitive_compare(_XPLATSTR("abc"), _XPLATSTR("ABCD")));
    ASSERT_FALSE(case_insensitive_compare(_XPLATSTR("abce"), _XPLATSTR("ABCD")));
}

TEST(case_insensitive_hash_functor, basic_hash_tests)
{
    case_insensitive_hash case_insensitive_hasher;

    ASSERT_EQ(0U, case_insensitive_hasher(_XPLATSTR("")));

    ASSERT_EQ(case_insensitive_hasher(_XPLATSTR("abc")), case_insensitive_hasher(_XPLATSTR("ABC")));
    ASSERT_EQ(case_insensitive_hasher(_XPLATSTR("abc123!@")), case_insensitive_hasher(_XPLATSTR("ABC123!@")));
    ASSERT_NE(case_insensitive_hasher(_XPLATSTR("abcd")), case_insensitive_hasher(_XPLATSTR("ABC")));
}