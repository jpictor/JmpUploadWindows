#region using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

#endregion

namespace JmpUploadEngine
{
    public class JobQueue
    {
        private volatile bool IsShutdown = false;
        private ManualResetEvent JobNotifyEvent = new ManualResetEvent ( false );
        private List<Job> Jobs = new List<Job> ( );

        public void Shutdown ( )
        {
            lock ( Jobs )
            {
                IsShutdown = true;
                JobNotifyEvent.Set ( );
            }
        }

        public void ClearAllJobs ( )
        {
            lock ( Jobs )
            {
                Jobs.Clear ( );
            }
        }

        public void AddJob ( Job job )
        {
            lock ( Jobs )
            {
                if ( IsShutdown )
                {
                    return;
                }
                Jobs.Add ( job );
                JobNotifyEvent.Set ( );
            }
        }

        public Job GetNextJob ( )
        {
            while ( true )
            {
                lock ( Jobs )
                {
                    if ( IsShutdown )
                    {
                        return null;
                    }
                    if ( Jobs.Count > 0 )
                    {
                        Job job = Jobs [ 0 ];
                        Jobs.RemoveAt ( 0 );
                        if ( Jobs.Count == 0 )
                        {
                            JobNotifyEvent.Reset ( );
                        }
                        return job;
                    }
                }
                JobNotifyEvent.WaitOne ( );
            }
        }
    }
}
