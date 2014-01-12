using System.Collections.Generic;
using System.Collections.Specialized;
using NUnit.Framework;
using QDMSServer;
using System.Windows.Threading;

namespace QDMSTest
{
    [TestFixture]
    public class ConcurrentNotifierBlockingListTest
    {
        private ConcurrentNotifierBlockingList<int> _list;
            
        [SetUp]
        public void Setup()
        {
            _list = new ConcurrentNotifierBlockingList<int>();
            for (int i = 0; i < 5; i++)
            {
                _list.TryAdd(i);
            }
        }

        [Test]
        public void AdddingItemsWithoutTimeoutWorks()
        {
            bool succeeded = _list.TryAdd(6);

            Assert.IsTrue(succeeded);
            Assert.IsTrue(_list.Collection.Contains(6));
        }

        [Test]
        public void RemovingItemsWithoutTimeoutWorks()
        {
            bool succeeded = _list.TryRemove(4);

            Assert.IsTrue(succeeded);
            Assert.IsTrue(!_list.Collection.Contains(4));
        }

        [Test]
        public void AdddingItemsWithTimeoutWorks()
        {
            bool succeeded = _list.TryAdd(6, 1000);

            Assert.IsTrue(succeeded);
            Assert.IsTrue(_list.Collection.Contains(6));
        }

        [Test]
        public void ReturnsFalseWhenRemovingItemNotInCollection()
        {
            bool succeeded = _list.TryRemove(100);

            Assert.IsFalse(succeeded);
        }

        [Test]
        public void RemovingItemsWithTimeoutWorks()
        {
            bool succeeded = _list.TryRemove(4, 1000);

            Assert.IsTrue(succeeded);
            Assert.IsTrue(!_list.Collection.Contains(4));
        }

        [Test]
        public void AdditionNotificationWorks()
        {
            //we have to use the dispatcher once so that the event can be raised on this thread
            Dispatcher disp = Dispatcher.CurrentDispatcher; 
            
            var actions = new List<NotifyCollectionChangedAction>();

            _list.CollectionChanged += (sender, e) => actions.Add(e.Action);

            _list.TryAdd(5);

            Assert.IsTrue(actions.Contains(NotifyCollectionChangedAction.Add));
        }

        [Test]
        public void RemovalNotificationWorks()
        {
            //we have to use the dispatcher once so that the event can be raised on this thread
            Dispatcher disp = Dispatcher.CurrentDispatcher; 
            var actions = new List<NotifyCollectionChangedAction>();

            _list.CollectionChanged += (sender, e) => actions.Add(e.Action);

            _list.TryRemove(4);

            Assert.IsTrue(actions.Contains(NotifyCollectionChangedAction.Remove));
        }
    }
}
