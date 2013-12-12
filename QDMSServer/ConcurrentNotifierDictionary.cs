// -----------------------------------------------------------------------
// <copyright file="ConcurrentNotifierDictionary.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Windows.Threading;

namespace QDMSServer
{
    public class ConcurrentNotifierDictionary<TKey, TValue> : IEnumerable, INotifyCollectionChanged
    {
        public ConcurrentDictionary<TKey, TValue> Dictionary;
        private readonly Dictionary<Delegate, Thread> _handlerThreads;

        public ConcurrentNotifierDictionary()
        {
            Dictionary = new ConcurrentDictionary<TKey, TValue>();
            _handlerThreads = new Dictionary<Delegate, Thread>();
        }

        public bool TryAdd(TKey key, TValue value)
        {
            bool res = Dictionary.TryAdd(key, value);
            if (res)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value)));
            return res;
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            bool res = Dictionary.TryRemove(key, out value);
            if(res)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new KeyValuePair<TKey, TValue>(key, value)));
            return res;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return Dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator GetEnumerator()
        {
            return Dictionary.GetEnumerator();
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
