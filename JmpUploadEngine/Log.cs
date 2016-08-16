#region using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion

namespace JmpUploadEngine
{
    public class Log
    {
        public delegate void LogMessageDelegate ( string msg );
        public static LogMessageDelegate LogInfoEvent;
        public static LogMessageDelegate LogErrorEvent;

        private delegate string StringFormatDelegate ( string format, params object [ ] formatArgs );
        private static StringFormatDelegate StringFormat = new StringFormatDelegate ( string.Format );

        public static void Info ( string msg )
        {
            if ( LogInfoEvent != null )
            {
                LogInfoEvent.Invoke ( msg );
            }
        }

        public static void Error ( string msg )
        {
            if ( LogInfoEvent != null )
            {
                LogInfoEvent.Invoke ( msg );
            }
        }

        public static void InfoFormat ( string format, params object [ ] formatArgs )
        {
            string msg = StringFormat.Invoke ( format, formatArgs );
            Info ( msg );
        }

        public static void ErrorFormat ( string format, params object [ ] formatArgs )
        {
            string msg = StringFormat.Invoke ( format, formatArgs );
            Error ( msg );
        }
    }
}
