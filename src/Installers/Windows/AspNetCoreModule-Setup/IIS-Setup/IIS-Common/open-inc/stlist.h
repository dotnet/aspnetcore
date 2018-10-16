// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#ifndef _STLIST_H
#define _STLIST_H

#ifndef IsListEmpty

#include <windows.h>

#define IsListEmpty(ListHead) ((ListHead)->Flink == (ListHead))

VOID
inline
InitializeListHead(
    LIST_ENTRY *    ListHead
    )
{
    ListHead->Flink = ListHead->Blink = ListHead;
}

BOOL
inline
RemoveEntryList(
    LIST_ENTRY *    Entry
    )
{
    LIST_ENTRY *    Blink;
    LIST_ENTRY *    Flink;

    Flink = Entry->Flink;
    Blink = Entry->Blink;
    Blink->Flink = Flink;
    Flink->Blink = Blink;
    return (Flink == Blink);
}

PLIST_ENTRY
inline
RemoveHeadList(
    LIST_ENTRY *    ListHead
    )
{
    LIST_ENTRY *    Flink;
    LIST_ENTRY *    Entry;

    Entry = ListHead->Flink;
    Flink = Entry->Flink;
    ListHead->Flink = Flink;
    Flink->Blink = ListHead;
    return Entry;
}

VOID
inline
InsertHeadList(
    LIST_ENTRY *    ListHead,
    LIST_ENTRY *    Entry
    )
{
    LIST_ENTRY *    Flink;

    Flink = ListHead->Flink;
    Entry->Flink = Flink;
    Entry->Blink = ListHead;
    Flink->Blink = Entry;
    ListHead->Flink = Entry;
}

VOID
inline
InsertTailList(
    LIST_ENTRY *    ListHead,
    LIST_ENTRY *    Entry
    )
{
    LIST_ENTRY *    Blink;

    Blink = ListHead->Blink;
    Entry->Flink = ListHead;
    Entry->Blink = Blink;
    Blink->Flink = Entry;
    ListHead->Blink = Entry;
}

#endif // IsListEmpty
#endif // _STLIST_H
