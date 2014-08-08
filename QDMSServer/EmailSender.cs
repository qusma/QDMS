// -----------------------------------------------------------------------
// <copyright file="EmailSender.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Net.Mail;

namespace QDMSServer
{
    public class EmailSender : IEmailService, IDisposable
    {
        private SmtpClient _client;

        public void Dispose()
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }

        public EmailSender(string host, string username, string password)
        {
            _client = new SmtpClient
            {
                Port = 25,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Host = host,
                Credentials = new System.Net.NetworkCredential(username, password)
            };
        }

        public void Send(string @from, string to, string subject, string body)
        {
            var mail = new MailMessage(from, to);
            mail.Subject = subject;
            mail.Body = body;

            _client.Send(mail);
        }
    }
}
