using Library.Networking;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DatabaseV2.Database
{
    /// <summary>
    /// A database lookup table.
    /// </summary>
    public class DatabaseLookup : IDisposable
    {
        /// <summary>
        /// The chunks in the database.
        /// </summary>
        private readonly List<ChunkDefinition> _chunks = new List<ChunkDefinition>();

        /// <summary>
        /// The lock to use when handling the lookup data.
        /// </summary>
        private readonly ReaderWriterLockSlim _databaseLock = new ReaderWriterLockSlim();

        /// <summary>
        /// A value indicating whether the object has already been _disposed.
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="DatabaseNode"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Joins two chunks into one.
        /// </summary>
        /// <param name="start1">The start of the first chunk.</param>
        /// <param name="end1">The end of the first chunk.</param>
        /// <param name="start2">The start of the second chunk.</param>
        /// <param name="end2">The end of the second chunk.</param>
        /// <param name="owner">The owner of the joined chunk.</param>
        /// <returns>A value indicating whether the join was successful.</returns>
        public bool Join(ChunkMarker start1, ChunkMarker end1, ChunkMarker start2, ChunkMarker end2, NodeDefinition owner)
        {
            bool chunk1Found = false;
            bool chunk2Found = false;
            ChunkDefinition chunk1 = null;
            ChunkDefinition chunk2 = null;
            _databaseLock.EnterWriteLock();

            foreach (var c in _chunks)
            {
                if (Equals(c.Start, start1) && Equals(c.End, end1))
                {
                    chunk1Found = true;
                    chunk1 = c;
                }
                else if (Equals(c.Start, start2) && Equals(c.End, end2))
                {
                    chunk2Found = true;
                    chunk2 = c;
                }

                if (chunk1Found && chunk2Found)
                {
                    break;
                }
            }

            if (chunk1Found && chunk2Found)
            {
                _chunks.Remove(chunk1);
                _chunks.Remove(chunk2);
                _chunks.Add(new ChunkDefinition(start1, end2, owner));
            }

            _databaseLock.ExitWriteLock();

            return chunk1Found && chunk2Found;
        }

        /// <summary>
        /// Splits a chunk into two new ones.
        /// </summary>
        /// <param name="start">The start of the chunk.</param>
        /// <param name="end">The end of the chunk.</param>
        /// <param name="middleMarker">The marker to split at.</param>
        /// <param name="owner">The owner of the new chunks.</param>
        /// <returns>A value indicating whether the split was successful.</returns>
        public bool Split(ChunkMarker start, ChunkMarker end, ChunkMarker middleMarker, NodeDefinition owner)
        {
            bool found = false;
            _databaseLock.EnterWriteLock();

            ChunkDefinition chunkToRemove = null;
            foreach (var c in _chunks)
            {
                if (Equals(c.Start, start) && Equals(c.End, end))
                {
                    chunkToRemove = c;
                    _chunks.Add(new ChunkDefinition(start, middleMarker, owner));
                    _chunks.Add(new ChunkDefinition(middleMarker, end, owner));
                    found = true;
                    break;
                }
            }

            if (found)
            {
                _chunks.Remove(chunkToRemove);
            }

            _databaseLock.ExitWriteLock();

            return found;
        }

        /// <summary>
        /// Updates the owner of a chunk.
        /// </summary>
        /// <param name="start">The start of the chunk.</param>
        /// <param name="end">The end of the chunk.</param>
        /// <param name="owner">The new owner of the chunk.</param>
        /// <returns>A value indicating whether the update was successful.</returns>
        public bool UpdateOwner(ChunkMarker start, ChunkMarker end, NodeDefinition owner)
        {
            bool found = false;
            _databaseLock.EnterWriteLock();

            ChunkDefinition chunkToRemove = null;
            foreach (var c in _chunks)
            {
                if (Equals(c.Start, start) && Equals(c.End, end))
                {
                    chunkToRemove = c;
                    found = true;
                    break;
                }
            }

            if (found)
            {
                _chunks.Remove(chunkToRemove);
                _chunks.Add(new ChunkDefinition(start, end, owner));
            }

            _databaseLock.ExitWriteLock();

            return found;
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="DatabaseNode"/> class.
        /// </summary>
        /// <param name="disposing">Whether to dispose of managed resources or not.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _databaseLock.Dispose();
                    _disposed = true;
                }
            }
        }
    }
}