// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.QPack
{
    public struct HeaderBlock
    {
        // first int is required insert count, 8bit prefix
        // The Base is encoded as sign-
        // and-modulus integer, using a single sign bit and a value with a 7-bit
        // prefix(see Section 4.5.1.2).

        /*
         *     0   1   2   3   4   5   6   7
               +---+---+---+---+---+---+---+---+
               |   Required Insert Count (8+)  |
               +---+---------------------------+
               | S |      Delta Base (7+)      |
               +---+---------------------------+
               |      Compressed Headers     ...
               +-------------------------------+
         *
         */

        // Prefix
        // Required insert count is used to determine when it is safe to process the rest of the block


        //if ReqInsertCount == 0:
        //   EncInsertCount = 0
        //else:
        //   EncInsertCount = (ReqInsertCount mod (2 * MaxEntries)) + 1

        //MaxEntries is max number of dynamic table entries
        //       MaxEntries = floor( MaxTableCapacity / 32 )

        //The decoder can reconstruct the Required Insert Count using an
        //algorithm such as the following.If the decoder encounters a value
        //of EncodedInsertCount that could not have been produced by a
        //conformant encoder, it MUST treat this as a connection error of type
        //"HTTP_QPACK_DECOMPRESSION_FAILED".

        //   TotalNumberOfInserts is the total number of inserts into the decoder's dynamic table.

        //        FullRange = 2 * MaxEntries
        //      if EncodedInsertCount == 0:
        //         ReqInsertCount = 0
        //      else:
        //         if EncodedInsertCount > FullRange:
        //            Error
        //         MaxValue = TotalNumberOfInserts + MaxEntries

        //         # MaxWrapped is the largest possible value of
        //# ReqInsertCount that is 0 mod 2*MaxEntries
        //        MaxWrapped = floor(MaxValue / FullRange) * FullRange
        //         ReqInsertCount = MaxWrapped + EncodedInsertCount - 1

        //         # If ReqInsertCount exceeds MaxValue, the Encoder's value
        //         # must have wrapped one fewer time
        //         if ReqInsertCount > MaxValue:
        //            if ReqInsertCount<FullRange:
        //               Error
        //            ReqInsertCount -= FullRange

        //if S == 0:
        //   Base = ReqInsertCount + DeltaBase
        //else:
        //   Base = ReqInsertCount - DeltaBase - 1

        // Now we have 5 different headers

        // indexed header field
        //0   1   2   3   4   5   6   7
        //+---+---+---+---+---+---+---+---+
        //| 1 | S |      Index(6+)       |
        //+---+---+-----------------------+

        // Indexed header field with post-base index
        //0   1   2   3   4   5   6   7
        //+---+---+---+---+---+---+---+---+
        //| 0 | 0 | 0 | 1 |  Index(4+)   |
        //+---+---+---+---+---------------+

        // Literal header field with name reference
        //0   1   2   3   4   5   6   7
        //+---+---+---+---+---+---+---+---+
        //| 0 | 1 | N | S |Name Index(4+)|
        //+---+---+---+---+---------------+
        //| H |     Value Length(7+)     |
        //+---+---------------------------+
        //|  Value String(Length bytes)  |
        //+-------------------------------+

        // 4.5.5.  Literal Header Field With Post-Base Name Reference

        //0   1   2   3   4   5   6   7
        //+---+---+---+---+---+---+---+---+
        //| 0 | 0 | 0 | 0 | N |NameIdx(3+)|
        //+---+---+---+---+---+-----------+
        //| H |     Value Length(7+)     |
        //+---+---------------------------+
        //|  Value String(Length bytes)  |
        //+-------------------------------+
        // 4.5.6.  Literal Header Field Without Name Reference
        //0   1   2   3   4   5   6   7
        //+---+---+---+---+---+---+---+---+
        //| 0 | 0 | 1 | N | H |NameLen(3+)|
        //+---+---+---+---+---+-----------+
        //|  Name String(Length bytes)   |
        //+---+---------------------------+
        //| H |     Value Length(7+)     |
        //+---+---------------------------+
        //|  Value String(Length bytes)  |
        //+-------------------------------+
    }
}
