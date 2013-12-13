// -----------------------------------------------------------------------
// <copyright file="ConcurrentNotifierDictionary.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Windows.Threading;

namespace QDMSServer
{
    public class ConcurrentNotifierBlockingList<T> : IEnumerable, INotifyCollectionChanged
    {
        public List<T> Collection;
        private readonly Dictionary<Delegate, Thread> _handlerThreads;
        private object lockObj = new object();

        public ConcurrentNotifierBlockingList()
        {
            Collection = new List<T>();
            _handlerThreads = new Dictionary<Delegate, Thread>();
        }

        public bool TryAdd(T value, int timeout = -1)
        {
            bool res = false;
            if(timeout == -1)
            {
                lock(lockObj)
                {
                    Collection.Add(value);
                    res = true;
                }
            }
            else
            {
                if(Monitor.TryEnter(lockObj, timeout))
                {
                    try
                    {
                        Collection.Add(value);
                        res = true;
                    }
                    finally
                    {
                        Monitor.Exit(lockObj);
                    }
                }
            }
            
            if (res)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value));
            return res;
        }

        public bool TryRemove(T value, int timeout = -1)
        {
            bool res = false;
            int index = 0;
            if (timeout == -1)
            {
                lock (lockObj)
                {
                    index = Collection.IndexOf(value);
                    if (index >= 0)
                    {
                        Collection.RemoveAt(index);
                        res = true;
                    }
                }
            }
            else
            {
                if (Monitor.TryEnter(lockObj, timeout))
                {
                    try
                    {
                        index = Collection.IndexOf(value);
                        if (index >= 0)
                        {
                            Collection.RemoveAt(index);
                            res = true;
                        }
                    }
                    finally
                    {
                        Monitor.Exit(lockObj);
                    }
                }
            }

            if(res)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, value, index));
            return res;
        }

        public T this [int index]
        {
            get
            {
                lock (lockObj)
                {
                    return Collection[index];
                }
            }

            set
            {
                lock (lockObj)
                {
                    Collection[index] = value;
                }
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator GetEnumerator()
        {
            return Collection.GetEnumerator();
        }


        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                _handlerThreads.Add(value, Thread.CurrentThread);
            }
            remove
            {
                _handlerThreads.Remove(value);
            }
        }

        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            foreach (Delegate handler in _handlerThreads.Keys)
            {
                Dispatcher dispatcher = Dispatcher.FromThread(_handlerThreads[handler]);
                if (dispatcher != null) dispatcher.Invoke(DispatcherPriority.Send, handler, this, e);
            }
        }
    }
    
}
