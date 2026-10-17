// -----------------------------------------------------------------------
// <copyright file="IEmailService.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

namespace QDMS.Server
{
    public interface IEmailService
    {
        void Send(string from, string to, string subject, string body);
    }
}
