#region using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion

namespace JmpUploadEngine
{
    public enum UploadEntryState
    {
        Start,
        UploadProcessStart,
        UploadProcessRunning,
        UploadProcessComplete,
        Aborting,
        Completed
    };

    public enum UploadEntryCompletedState
    {
        None,
        Success,
        Error,
        Aborted
    }

    public class UploadEntry
    {
        public UploadEntryState State = UploadEntryState.Start;
        public UploadEntryCompletedState CompletedState = UploadEntryCompletedState.None;

        public string Name;
        public string UploadSourceDirectory;
        public string UploadFilePath;
        public double UploadBandwidthMBSec;
        public bool UploadSuccess;
        public ChunkFileInfo ChunkFileInfo;

        public void Abort ( )
        {
            State = UploadEntryState.Aborting;
        }

        public bool IsAborted
        {
            get { return State == UploadEntryState.Aborting; }
        }
    }
}
