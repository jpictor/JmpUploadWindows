#region using

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

using ICSharpCode.SharpZipLib.Zip; 

#endregion

namespace JmpUploadEngine
{
    public class JmpUploadClientEngine
    {
        #region Members

        // default number of concurrent HTTP connections to a server from a client from RFC-something (need ref)
        const int DefaultNumberOfUploadChannels = 2;

        public int NumberOfUploadChannels = DefaultNumberOfUploadChannels;
        public string UploadUrl;

        public delegate void UploadEntryStateChangedDelegate ( UploadEntry ue );
        public UploadEntryStateChangedDelegate UploadEntryStateChangedEvent;

        public delegate void UpdateProgressDelegate ( UploadEntry ue, int unitsCompleted, int unitsTotal );
        public UpdateProgressDelegate UpdateUploadProgressEvent;

        public List<UploadEntry> UploadEntries;
        private string TempDirectory;

        #endregion

        #region Constructors

        public JmpUploadClientEngine ( )
        {
            UploadEntries = new List<UploadEntry> ( );
            TempDirectory = Path.Combine ( Path.GetTempPath ( ), "JmpUploadClient" );
            ControlJobProcess = new JobProcess ( "ControlJob", new ThreadStart ( ControlJobProcessThreadMain ) );
            UploadJobProcess = new JobProcess ( "UploadJob", new ThreadStart ( UploadJobProcessThreadMain ) );
            UploadChunkProcess = new JobProcess ( "UploadChunkJob", new ThreadStart ( UploadChunkProcessThreadMain ), DefaultNumberOfUploadChannels );
        }

        #endregion

        #region API Methods

        public void SetNumberOfUploadChannels ( int n )
        {
            // only allow set when there are no running jobs
            if ( UploadEntries.Count > 0 )
            {
                return;
            }
            NumberOfUploadChannels = n;

            // shutdown and restart the chunk sending processes
            UploadChunkProcess.SignalShutdown ( );
            UploadChunkProcess.WaitForShutdown ( );
            UploadChunkProcess = new JobProcess ( "UploadChunkJob", new ThreadStart ( UploadChunkProcessThreadMain ), NumberOfUploadChannels );

            // adjust connection limit
            ServicePointManager.DefaultConnectionLimit = Math.Max ( n, 2 );
        }

        public void AddUploadEntry ( UploadEntry ue )
        {
            FileInfo fi = new FileInfo ( ue.UploadFilePath );
            ue.UploadFilePath = fi.FullName;
            ue.Name = fi.Name;
            ue.ChunkFileInfo = new ChunkFileInfo ( fi.Length );
            UploadEntries.Add ( ue );
            ControlJobProcess.AddJob ( new Job ( ue ) );
        }

        public void Shutdown ( )
        {
            foreach ( UploadEntry ue in UploadEntries )
            {
                ue.Abort ( );
            }
            ControlJobProcess.SignalShutdown ( );
            UploadJobProcess.SignalShutdown ( );
            UploadChunkProcess.SignalShutdown ( );
            ControlJobProcess.WaitForShutdown ( );
            UploadJobProcess.WaitForShutdown ( );
            UploadChunkProcess.WaitForShutdown ( );
        }

        #endregion

        #region Control Thread

        private JobProcess ControlJobProcess;
        
        private void ControlJobProcessThreadMain ( )
        {
            while ( true )
            {
                Job job = ControlJobProcess.GetNextJob ( );
                if ( job == null )
                {
                    break;
                }
                UploadJobProcess.AddJob ( job );
            }
        }

        #endregion

        #region Event Notification

        private void NotifyUploadProgress ( UploadEntry ue, int unitsCompleted, int unitsTotal )
        {
            try
            {
                UpdateUploadProgressEvent ( ue, unitsCompleted, unitsTotal );
            }
            catch { }
        }

        private void NotifyUploadEntryStateChanged ( UploadEntry ue )
        {
            try
            {
                UploadEntryStateChangedEvent ( ue );
            }
            catch { }
        }

        #endregion

        #region HTTP Headers 

        private void AddCommonHeaders ( HttpWebRequest webRequest )
        {
            webRequest.Headers.Add ( "JMP-VERSION", "1.0" );

            string machineName = System.Environment.MachineName;
            if ( !string.IsNullOrEmpty ( machineName ) )
            {
                webRequest.Headers.Add ( "JMP-MACHINE-NAME", machineName );
            }

            string userName = System.Environment.UserName;
            if ( !string.IsNullOrEmpty ( userName ) )
            {
                webRequest.Headers.Add ( "JMP-USER-NAME", userName );
            }
        }

        #endregion

        #region Upload Process

        private JobProcess UploadJobProcess;

        private void UploadJobProcessThreadMain ( )
        {
            while ( true )
            {
                Job job = UploadJobProcess.GetNextJob ( );
                if ( job == null )
                {
                    break;
                }
                UploadFile ( job.UploadEntry );
            }
        }

        //int MaxRetries = 3;
        int RetrySleepMS = 1000;

        private void UploadFile ( UploadEntry ue )
        {
            if ( ue.IsAborted )
            {
                return;
            }

            Log.InfoFormat ( "UploadFile: {0} size={1:0.0}MB", ue.Name, ue.ChunkFileInfo.FileSizeMB );
            ue.State = UploadEntryState.UploadProcessStart;
            NotifyUploadEntryStateChanged ( ue );
            DateTime startTime = DateTime.Now;

            // make initial upload request to server
            bool openSuccess = false;
            while ( !ue.IsAborted )
            {
                bool canRetryOpen;
                openSuccess = OpenUpload ( ue, out canRetryOpen );
                if ( openSuccess )
                {
                    Log.InfoFormat ( "UploadFile: {0} size={1:0.0}MB chunksize={2:0.0}MB numChunks={3}", ue.Name, ue.ChunkFileInfo.FileSizeMB, (double) ue.ChunkFileInfo.ChunkSize / (double) ChunkFileInfo.OneMB, ue.ChunkFileInfo.NumChunks );
                    break;
                }
                Thread.Sleep ( RetrySleepMS );
            }

            // send file in chunks
            bool allChunksSent = false;
            if ( !ue.IsAborted && openSuccess )
            {
                using ( FileStream fs = File.Open ( ue.UploadFilePath, FileMode.Open, FileAccess.Read ) )
                {
                    JobQueue completedJobQueue = new JobQueue ( );
                    
                    for ( int i = 0; i < ue.ChunkFileInfo.NumChunks; ++i )
                    {
                        UploadChunkJob job = new UploadChunkJob ( ue, completedJobQueue, fs, i );
                        UploadChunkProcess.AddJob ( job );
                    }

                    int numChunksCompleted = 0;
                    long bytesWritten = 0;
                    while ( !ue.IsAborted && numChunksCompleted < ue.ChunkFileInfo.NumChunks )
                    {
                        UploadChunkJob job = ( UploadChunkJob ) completedJobQueue.GetNextJob ( );
                        if ( job == null )
                        {
                            break;
                        }
                        numChunksCompleted++;
                        bytesWritten += job.BytesWritten;
                        double elapsedSeconds = ( DateTime.Now - startTime ).TotalSeconds;
                        ue.UploadBandwidthMBSec = ( ( double ) bytesWritten / ( double ) ChunkFileInfo.OneMB ) / elapsedSeconds;
                        NotifyUploadProgress ( ue, numChunksCompleted, ue.ChunkFileInfo.NumChunks );
                    }

                    if ( numChunksCompleted == ue.ChunkFileInfo.NumChunks )
                    {
                        allChunksSent = true;
                    }
                    else
                    {
                        UploadChunkProcess.ClearAllJobs ( );
                    }
                }
            }

            // close upload on server
            bool closeSuccess = false;
            while ( !ue.IsAborted )
            {
                bool canRetryClose = false;
                closeSuccess = CloseUpload ( ue, out canRetryClose );
                if ( closeSuccess )
                {
                    break;
                }
                Thread.Sleep ( RetrySleepMS );
            }

            // the upload is complete, we're done with the upload entry
            ue.State = UploadEntryState.Completed;
            bool uploadSuccess = openSuccess && allChunksSent && closeSuccess;
            if ( openSuccess && allChunksSent && closeSuccess )
            {
                ue.CompletedState = UploadEntryCompletedState.Success;
                ue.UploadSuccess = true;
            }
            else if ( ue.IsAborted )
            {
                ue.CompletedState = UploadEntryCompletedState.Aborted;
                ue.UploadSuccess = false;
            }
            else
            {
                ue.CompletedState = UploadEntryCompletedState.Error;
                ue.UploadSuccess = false;
            }
            UploadEntries.Remove ( ue );
            NotifyUploadEntryStateChanged ( ue );
        }

        JobProcess UploadChunkProcess;

        class UploadChunkJob : Job
        {
            public JobQueue CompletedJobQueue;
            public FileStream FileStream;
            public int ChunkNumber;
            public int BytesWritten;
            public double WriteTime;
            public UploadChunkJob ( UploadEntry ue, JobQueue jq, FileStream fs, int cn ) : base(ue)
            {
                CompletedJobQueue = jq;
                FileStream = fs;
                ChunkNumber = cn;
            }
        }

        private void UploadChunkProcessThreadMain ( )
        {
            while ( true )
            {
                UploadChunkJob job = ( UploadChunkJob ) UploadChunkProcess.GetNextJob ( );
                if ( job == null )
                {
                    break;
                }

                bool uploadSuccess;
                while ( !job.UploadEntry.IsAborted )
                {
                    bool canRetry;
                    uploadSuccess = UploadFileChunk ( job.UploadEntry, job.FileStream, job.ChunkNumber, out job.BytesWritten, out job.WriteTime, out canRetry );
                    if ( uploadSuccess )
                    {
                        job.CompletedJobQueue.AddJob ( job );
                        break;
                    }
                    if ( ! job.UploadEntry.IsAborted )
                    {
                        Log.ErrorFormat ( "UploadFileChunk: {0} upload chunk={1} failed", job.UploadEntry.Name, job.ChunkNumber );
                    }
                }
            }
        }

        private bool OpenUpload ( UploadEntry ue, out bool canRetry )
        {
            // only some connection errors can result in a retry
            canRetry = false;

            // HTTP request
            HttpWebRequest webRequest = ( HttpWebRequest ) WebRequest.Create ( UploadUrl );
            webRequest.Method = "POST";
            webRequest.Accept = "*/*";
            webRequest.KeepAlive = true;
            webRequest.Pipelined = false;
            webRequest.ContentType = "binary/octet-stream";

            AddCommonHeaders ( webRequest );
            webRequest.Headers.Add ( "JMP-METHOD", "OPEN.UPLOAD" );
            webRequest.Headers.Add ( "JMP-FILE-NAME", ue.Name );
            webRequest.Headers.Add ( "JMP-UPLOAD-FILE-SIZE", ue.ChunkFileInfo.FileSize.ToString ( ) );

            // do HTTP POST here
            HttpWebResponse webResponse = null;
            string response = string.Empty;
            bool noRequestError = true;

            DateTime startTime = DateTime.Now;

            try
            {
                webResponse = ( HttpWebResponse ) webRequest.GetResponse ( );
            }
            catch ( WebException e )
            {
                if ( !ue.IsAborted )
                {
                    Log.ErrorFormat ( "OpenUpload: WebException {0} {1}", e.Status.ToString(), e.Message);
                }
                canRetry = true;
                noRequestError = false;
            }
            catch ( Exception e )
            {
                if ( !ue.IsAborted )
                {
                    Log.ErrorFormat ( "OpenUpload: Exception {1}", e.Message );
                }
                canRetry = true;
                noRequestError = false;
            }
            finally
            {
                if ( webResponse != null )
                {
                    webResponse.GetResponseStream ( ).Close ( );
                    webResponse.Close ( );
                }
            }

            DateTime endTime = DateTime.Now;

            // error out here on request error
            if ( !noRequestError )
            {
                return false;
            }

            // check HTTP response code for errors
            if ( webResponse.StatusCode != HttpStatusCode.OK )
            {
                Log.ErrorFormat ( "OpenUpload: unexpected http response {0}", webResponse.StatusCode.ToString ( ) );
                return false;
            }

            // check upload ID
            string uploadId = null;

            try
            {
                uploadId = webResponse.Headers.Get ( "JMP-UPLOAD-ID" );
            }
            catch
            {
                Log.ErrorFormat ( "OpenUpload: server did not send JMP-UPLOAD-ID" );
                return false;
            }

            int chunkSize;
            if ( !int.TryParse ( webResponse.Headers.Get ( "JMP-CHUNK-SIZE" ), out chunkSize ) )
            {
                Log.ErrorFormat ( "OpenUpload: server did not send JMP-CHUNK-SIZE" );
                return false;
            }

            // set upload ID and chunk size if (XXX: checck ID is valid, non-null)
            ue.ChunkFileInfo.UploadId = uploadId;
            ue.ChunkFileInfo.SetChunkSize ( chunkSize );

            // log upload bandwidth
            double seconds = ( endTime - startTime ).TotalSeconds;
            Log.InfoFormat ( "OpenUpload: {0} size={1:0.00}MB time={2:0.00}sec", ue.Name, ue.ChunkFileInfo.FileSizeMB, seconds );

            return true;
        }

        private bool UploadFileChunk ( UploadEntry ue, FileStream fs, int uploadChunkNum, out int bytesWritten, out double writeTime, out bool canRetry )
        {
            bytesWritten = 0;
            writeTime = 0;

            // only some connection errors can result in a retry
            canRetry = false;

            // figure out file chunk info
            int readBytes = ue.ChunkFileInfo.GetChunkSize ( uploadChunkNum );
            byte [ ] chunk = new byte [ readBytes ];
            long offset = ue.ChunkFileInfo.ChunkSeekOffset ( uploadChunkNum );

            // read chunk from file
            fs.Seek ( offset, SeekOrigin.Begin );
            fs.Read ( chunk, 0, readBytes );

            // HTTP connection request for upload
            HttpWebRequest webRequest = ( HttpWebRequest ) WebRequest.Create ( UploadUrl );
            webRequest.Method = "POST";
            webRequest.Accept = "*/*";
            webRequest.KeepAlive = true;
            webRequest.Pipelined = false;
            webRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            webRequest.ContentType = "binary/octet-stream";
            webRequest.ContentLength = readBytes;

            AddCommonHeaders ( webRequest );
            webRequest.Headers.Add ( "JMP-METHOD", "UPLOAD.CHUNK" );
            webRequest.Headers.Add ( "JMP-UPLOAD-ID", ue.ChunkFileInfo.UploadId );
            webRequest.Headers.Add ( "JMP-FILE-NAME", ue.Name );
            webRequest.Headers.Add ( "JMP-UPLOAD-FILE-SIZE", ue.ChunkFileInfo.FileSize.ToString ( ) );
            webRequest.Headers.Add ( "JMP-CHUNK-SIZE", ue.ChunkFileInfo.ChunkSize.ToString ( ) );
            webRequest.Headers.Add ( "JMP-NUM-CHUNKS", ue.ChunkFileInfo.NumChunks.ToString ( ) );
            webRequest.Headers.Add ( "JMP-CHUNK-NUMBER", uploadChunkNum.ToString ( ) );

            // do HTTP POST here
            Stream requestStream = null;
            HttpWebResponse webResponse = null;
            bool noRequestError = true;
            DateTime startTime = DateTime.Now;

            try
            {
                requestStream = webRequest.GetRequestStream ( );
                requestStream.Write ( chunk, 0, readBytes );
                requestStream.Close ( );
                requestStream.Dispose ( );
                webResponse = ( HttpWebResponse ) webRequest.GetResponse ( );
            }
            catch ( WebException e )
            {
                if ( !ue.IsAborted )
                {
                    Log.ErrorFormat ( "OpenUpload: WebException {0} {1}", e.Status.ToString ( ), e.Message );
                }
                canRetry = true;
                noRequestError = false;
            }
            catch ( Exception e )
            {
                if ( !ue.IsAborted )
                {
                    Log.ErrorFormat ( "OpenUpload: Exception {1}", e.Message );
                }
                canRetry = true;
                noRequestError = false;
            }
            finally
            {
                if ( webResponse != null )
                {
                    webResponse.GetResponseStream ( ).Close ( );
                    webResponse.Close ( );
                }
            }

            DateTime endTime = DateTime.Now;
            double seconds = ( endTime - startTime ).TotalSeconds;

            // error out here on request error
            if ( !noRequestError )
            {
                return false;
            }

            // check HTTP response code for errors
            if ( webResponse.StatusCode != HttpStatusCode.OK )
            {
                Log.ErrorFormat ( "UploadFileChunk: unexpected http response {0}", webResponse.StatusCode.ToString () );
                return false;
            }

            // log upload bandwidth
            bytesWritten = readBytes;
            writeTime = seconds;
            double bandwidthEstimateMBSec = ( ( double ) readBytes / ( double ) ChunkFileInfo.OneMB ) / seconds;
            Log.InfoFormat ( "UploadFileChunk: {0} chunk={1} bandwidth={2:0.00}MB/sec", ue.Name, uploadChunkNum, bandwidthEstimateMBSec );

            return true;
        }

        private bool CloseUpload ( UploadEntry ue, out bool canRetry )
        {
            // only some connection errors can result in a retry
            canRetry = false;

            // HTTP request
            HttpWebRequest webRequest = ( HttpWebRequest ) WebRequest.Create ( UploadUrl );
            webRequest.Method = "POST";
            webRequest.Accept = "*/*";
            webRequest.KeepAlive = true;
            webRequest.Pipelined = false;
            webRequest.ContentType = "binary/octet-stream";

            AddCommonHeaders ( webRequest );
            webRequest.Headers.Add ( "JMP-METHOD", "CLOSE.UPLOAD" );
            webRequest.Headers.Add ( "JMP-UPLOAD-ID", ue.ChunkFileInfo.UploadId );
            webRequest.Headers.Add ( "JMP-FILE-NAME", ue.Name );
            webRequest.Headers.Add ( "JMP-UPLOAD-FILE-SIZE", ue.ChunkFileInfo.FileSize.ToString ( ) );

            // do HTTP POST here
            HttpWebResponse webResponse = null;
            string response = string.Empty;
            bool noRequestError = true;

            DateTime startTime = DateTime.Now;

            try
            {
                webResponse = ( HttpWebResponse ) webRequest.GetResponse ( );
            }
            catch ( WebException e )
            {
                if ( !ue.IsAborted )
                {
                    Log.ErrorFormat ( "OpenUpload: WebException {0} {1}", e.Status.ToString ( ), e.Message );
                }
                canRetry = true;
                noRequestError = false;
            }
            catch ( Exception e )
            {
                if ( !ue.IsAborted )
                {
                    Log.ErrorFormat ( "OpenUpload: Exception {1}", e.Message );
                }
                canRetry = true;
                noRequestError = false;
            }
            finally
            {
                if ( webResponse != null )
                {
                    webResponse.GetResponseStream ( ).Close ( );
                    webResponse.Close ( );
                }
            }

            DateTime endTime = DateTime.Now;

            // error out here on request error
            if ( !noRequestError )
            {
                return false;
            }

            // check HTTP response code for errors
            if ( webResponse.StatusCode != HttpStatusCode.OK )
            {
                Log.ErrorFormat ( "CloseUpload: unexpected http response {0}", webResponse.StatusCode.ToString ( ) );
                return false;
            }

            double seconds = ( endTime - startTime ).TotalSeconds;
            Log.InfoFormat ( "CloseUpload: file={0} size={1:0.00}MB time={2:0.00}sec", ue.Name, ue.ChunkFileInfo.FileSizeMB, seconds );

            return true;
        }

        #endregion
    }
}
