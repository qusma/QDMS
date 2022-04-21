// -----------------------------------------------------------------------
// <copyright file="FeedTimer.cs" company="">
// Copyright 2016 Leonhard Schick (leonhard.schick@gmail.com)
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QDMS.Server.DataSources.ForexFeed
{
    class FeedTimer
    {
        public FeedTimer(BarSize barSize)
        {
            BarSize = barSize;
            loopHandler = TimerLoop;
        }

        public BarSize BarSize { get; }

        public bool Active { get; private set; }

        public event EventHandler<BarSize> Elapsed;

        private TimerLoopHandler loopHandler;
        private IAsyncResult asyncResult;

        private void TimerLoop()
        {
            while (Active)
            {
                int interval = GetTimerInterval(BarSize);
                System.Threading.Thread.Sleep(interval);

                if (!Active)
                    return;

                if (Elapsed != null)
                    Elapsed(this, BarSize);
            }
        }

        private delegate void TimerLoopHandler();

        public void Start()
        {
            Active = true;
            asyncResult = loopHandler.BeginInvoke(null, null);
        }

        public void Stop()
        {
            Active = false;
            if (!asyncResult.IsCompleted)
                asyncResult.AsyncWaitHandle.WaitOne(0);
        }

        public void Stop(TimeSpan timeout)
        {
            Active = false;
            if (!asyncResult.IsCompleted)
                asyncResult.AsyncWaitHandle.WaitOne(timeout);
        }

        static int GetTimerInterval(BarSize barSize)
        {
            DateTime now = DateTime.Now;

            switch (barSize)
            {
                case BarSize.OneSecond:
                    return 1000
                        - now.Millisecond;
                case BarSize.OneMinute:
                    return (60 - now.Second) * 1000
                        - now.Millisecond;
                case BarSize.OneHour:
                    return (60 - now.Minute) * 60 * 1000
                        - (60 - now.Second) * 1000
                        - now.Millisecond;
                case BarSize.OneDay:
                    return (24 - now.Hour) * 24 * 60 * 1000
                        - (60 - now.Minute) * 60 * 1000
                        - (60 - now.Second) * 1000
                        - now.Millisecond;
                default:
                    throw new NotImplementedException($"BarSize {barSize} is currently not supported by the ForexFeed data source");
            }
        }
    }
}
