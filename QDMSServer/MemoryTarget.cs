// -----------------------------------------------------------------------
// <copyright file="MemoryTarget.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

// A target for NLog, used to display the log in the UI.

using System;
using System.Reactive.Subjects;
using NLog;
using NLog.Targets;

namespace QDMSServer
{
    [Target("MemoryTarget")]
    class MemoryTarget : TargetWithLayout
    {
        private readonly Subject<LogEventInfo> _messages = new Subject<LogEventInfo>();
        public IObservable<LogEventInfo> Messages { get; private set; }

        public MemoryTarget()
        {
            Messages = _messages;
        }

        protected override void Write(LogEventInfo logEvent)
        {
            _messages.OnNext(logEvent);
        }        
    }
}
