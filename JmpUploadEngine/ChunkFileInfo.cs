#region using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion

namespace JmpUploadEngine
{
    public class ChunkFileInfo
    {
        public const int OneMB = 1048576;
        public const int TwoMB = 2 * OneMB;
        public const int ThreeMB = 3 * OneMB;

        public int ChunkSize;
        public long FileSize;
        public double FileSizeMB;
        public int NumChunks;
        public int LastChunkSize;
        public string UploadId;

        public ChunkFileInfo ( long fileSize )
        {
            FileSize = fileSize;
            FileSizeMB = ( double ) FileSize / ( double ) OneMB;
        }

        public void SetChunkSize ( int chunkSize )
        {
            ChunkSize = chunkSize;
            NumChunks = ( int ) Math.Floor ( ( double ) FileSize / ( double ) ChunkSize );
            LastChunkSize = ( int ) ( FileSize % ChunkSize );
            if ( LastChunkSize > 0 )
            {
                NumChunks++;
            }
        }

        public bool IsFirstChunk ( int chunkNum )
        {
            return ( chunkNum == 0 );
        }

        public bool IsLastChunk ( int chunkNum )
        {
            return ( chunkNum == ( NumChunks - 1 ) );
        }

        public long ChunkSeekOffset ( int chunkNum )
        {
            return chunkNum * ChunkSize;
        }

        public int GetChunkSize ( int chunkNum )
        {
            if ( IsLastChunk ( chunkNum ) )
            {
                return LastChunkSize;
            }
            return ChunkSize;
        }
    }
}
