// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text;

namespace AspNetCoreModule.Test.WebSocketClient
{
    public class Frame
    {
        private int startingIndex;  // This will be initialized as output parameter of GetFrameString() 
        public int DataLength = 0;  // This will be initialized as output parameter of GetFrameString() 

        public Frame(byte[] data)
        {
            Data = data;
            FrameType = WebSocketClientUtility.GetFrameType(Data);
            Content = WebSocketClientUtility.GetFrameString(Data, out startingIndex, out DataLength);
            IsMasked = WebSocketClientUtility.IsFrameMasked(Data);
        }

        public FrameType FrameType { get; set; }
        public byte[] Data { get; private set; }

        public string TextData
        {
            get
            {
                if (DataLength == 0)
                {
                    throw new System.Exception("DataLength is zero");
                }
                return Encoding.ASCII.GetString(Data, startingIndex, DataLength);
            }
        }

        public string Content { get; private set; }
        public bool IsMasked { get; private set; }

        public int IndexOfNextFrame
        {
            get
            {
                if (startingIndex > 0 && Data.Length > Content.Length + startingIndex)
                {
                    return Content.Length + startingIndex;
                }
                else
                {
                    return -1;
                }
            }
        }

        override public string ToString()
        {
            return FrameType + ": " + Content;
        }
    }
}
