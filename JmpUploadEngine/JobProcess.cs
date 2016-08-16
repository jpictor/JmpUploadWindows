#region using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

#endregion

namespace JmpUploadEngine
{
    public class JobProcess
    {
        private string Name;
        private ThreadStart ThreadMain;
        private volatile bool IsStarted;
        private int NumThreads;
        private Thread [ ] Threads;
        private JobQueue JobQueue;

        public JobProcess ( string name, ThreadStart threadStart, int numThreads )
        {
            Name = name;
            ThreadMain = threadStart;
            NumThreads = numThreads;
        }

        public JobProcess ( string name, ThreadStart threadStart )
            : this ( name, threadStart, 1 )
        {
        }

        public void Start ( )
        {
            lock ( this )
            {
                if ( !IsStarted )
                {
                    Log.InfoFormat ( "JobProcess.Start: {0}", Name );
                    JobQueue = new JobQueue ( );
                    Threads = new Thread [ NumThreads ];
                    for ( int i = 0; i < Threads.Length; ++i )
                    {
                        Threads [ i ] = new Thread ( ThreadMain );
                        Threads [ i ].Name = string.Format ( "JobProcess:{0}.{1}", Name, i );
                        Threads [ i ].Start ( );
                    }
                    IsStarted = true;
                }
            }
        }

        public void SignalShutdown ( )
        {
            lock ( this )
            {
                if ( IsStarted )
                {
                    Log.InfoFormat ( "JobProcess.SignalShutdown: {0} called", Name );
                    JobQueue.Shutdown ( );
                }
            }
        }

        public void WaitForShutdown ( )
        {
            lock ( this )
            {
                if ( IsStarted )
                {
                    Log.InfoFormat ( "JobProcess.WaitForShutdown: {0} called", Name );
                    for ( int i = 0; i < Threads.Length; ++i )
                    {
                        if ( Threads [ i ] == Thread.CurrentThread )
                        {
                            throw new Exception ( "Programming Error: JobProcess.Shutdown() cannot be called from worker thread" );
                        }
                        Threads [ i ].Join ( );
                        Threads [ i ] = null;
                    }
                    IsStarted = false;
                    Log.InfoFormat ( "JobProcess.WaitForShutdown: {0} complete", Name );
                }
            }
        }

        public void AddJob ( Job job )
        {
            if ( !IsStarted )
            {
                Start ( );
            }
            JobQueue.AddJob ( job );
        }

        public Job GetNextJob ( )
        {
            return JobQueue.GetNextJob ( );
        }

        public void ClearAllJobs ( )
        {
            JobQueue.ClearAllJobs ( );
        }
    }
}
