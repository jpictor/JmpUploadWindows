#region using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion

namespace JmpUploadClient
{
    public class LogApi
    {
        private delegate string StringFormatDelegate ( string format, params object [ ] formatArgs );
        private static StringFormatDelegate StringFormat = new StringFormatDelegate ( string.Format );

        public delegate void LogWriteLineDelegate ( string msg );
        public static LogWriteLineDelegate LogWriteLine;

        public static void Info ( string msg )
        {
            if ( LogWriteLine != null )
            {
                LogWriteLine.Invoke ( msg );
            }
        }

        public static void Error ( string msg )
        {
            if ( LogWriteLine != null )
            {
                LogWriteLine.Invoke ( string.Format ( "ERROR: {0}", msg ) );
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
