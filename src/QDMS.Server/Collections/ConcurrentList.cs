// -----------------------------------------------------------------------
// <copyright file="ConcurrentList.cs" company="">
// Copyright 2026 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;

namespace QDMS.Server
{
    /// <summary>
    /// A lock-protected list. <see cref="Collection"/> returns a snapshot, so it's safe to enumerate
    /// while other threads add or remove items.
    /// </summary>
    public class ConcurrentList<T> : IEnumerable<T>
    {
        private readonly List<T> _items = new List<T>();

        public List<T> Collection
        {
            get
            {
                lock (_items)
                {
                    return new List<T>(_items);
                }
            }
        }

        public bool TryAdd(T value)
        {
            lock (_items)
            {
                _items.Add(value);
                return true;
            }
        }

        public bool TryRemove(T value)
        {
            lock (_items)
            {
                return _items.Remove(value);
            }
        }

        public IEnumerator<T> GetEnumerator() => Collection.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
