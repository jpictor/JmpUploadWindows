#region using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using JmpUploadEngine;

#endregion

namespace JmpUploadClient
{
    static class Program
    {
        [STAThread]
        static void Main ( )
        {
            Application.EnableVisualStyles ( );
            Application.SetCompatibleTextRenderingDefault ( false );
            JmpUploadForm utvForm = new JmpUploadForm ( );
            LogApi.LogWriteLine = utvForm.LogLine;
            JmpUploadEngine.Log.LogInfoEvent = utvForm.LogLine;
            JmpUploadEngine.Log.LogErrorEvent = utvForm.LogLine;
            Application.Run ( utvForm );
        }
    }
}
