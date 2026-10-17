// -----------------------------------------------------------------------
// <copyright file="EmailSender.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Mail;

namespace QDMS.Server
{
    public class EmailSender : IEmailService, IDisposable
    {
        private SmtpClient _client;

        public EmailSender(string host, string username, string password, int port)
        {
            _client = new SmtpClient
            {
                Port = port,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = true,
                UseDefaultCredentials = false,
                Host = host,
                Credentials = new NetworkCredential(username, password)
            };
        }

        public void Send(string @from, string to, string subject, string body)
        {
            using var mail = new MailMessage(from, to) { Subject = subject, Body = body };
            _client.Send(mail);
        }

        public void Dispose()
        {
            _client?.Dispose();
            _client = null;
        }
    }
}
