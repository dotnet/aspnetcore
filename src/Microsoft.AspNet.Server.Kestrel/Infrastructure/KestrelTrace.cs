// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

//using System.Diagnostics.Tracing;

namespace Microsoft.AspNet.Server.Kestrel
{
    /// <summary>
    /// Summary description for KestrelTrace
    /// </summary>
    public class KestrelTrace //: EventSource
    {
        public static KestrelTrace Log = new KestrelTrace();
  //      static EventTask Connection = (EventTask)1;
    //    static EventTask Frame = (EventTask)1;


      //  [Event(13, Level = EventLevel.Informational, Message = "Id {0}")]
        public void ConnectionStart(long connectionId)
        {
         //   WriteEvent(13, connectionId);
        }

      //  [Event(14, Level = EventLevel.Informational, Message = "Id {0}")]
        public void ConnectionStop(long connectionId)
        {
       //     WriteEvent(14, connectionId);
        }


   //     [Event(4, Message = "Id {0} Status {1}")]
        internal void ConnectionRead(long connectionId, int status)
        {
     //       WriteEvent(4, connectionId, status);
        }

 //       [Event(5, Message = "Id {0}")]
        internal void ConnectionPause(long connectionId)
        {
   //         WriteEvent(5, connectionId);
        }

 //       [Event(6, Message = "Id {0}")]
        internal void ConnectionResume(long connectionId)
        {
   //         WriteEvent(6, connectionId);
        }

  //      [Event(7, Message = "Id {0}")]
        internal void ConnectionReadFin(long connectionId)
        {
    //        WriteEvent(7, connectionId);
        }

//        [Event(8, Message = "Id {0} Step {1}")]
        internal void ConnectionWriteFin(long connectionId, int step)
        {
  //          WriteEvent(8, connectionId, step);
        }

 //       [Event(9, Message = "Id {0}")]
        internal void ConnectionKeepAlive(long connectionId)
        {
   //         WriteEvent(9, connectionId);
        }

 //       [Event(10, Message = "Id {0}")]
        internal void ConnectionDisconnect(long connectionId)
        {
   //         WriteEvent(10, connectionId);
        }

  //      [Event(11, Message = "Id {0} Count {1}")]
        internal void ConnectionWrite(long connectionId, int count)
        {
    //        WriteEvent(11, connectionId, count);
        }

 //       [Event(12, Message = "Id {0} Status {1}")]
        internal void ConnectionWriteCallback(long connectionId, int status)
        {
   //         WriteEvent(12, connectionId, status);
        }
    }
}
