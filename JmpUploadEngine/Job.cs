#region using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion

namespace JmpUploadEngine
{
    public class Job
    {
        public UploadEntry UploadEntry;

        public Job ( UploadEntry uploadEntry )
        {
            UploadEntry = uploadEntry;
        }
    }
}
