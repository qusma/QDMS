// -----------------------------------------------------------------------
// <copyright file="StreamSocket.cs" company="">
// Copyright 2026 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace QDMS.Server.DataSources.Binance
{
    /// <summary>
    /// Minimal read-only websocket: connects to a stream URL and hands each text message to a callback.
    /// </summary>
    internal sealed class StreamSocket : IDisposable
    {
        private readonly Uri _uri;
        private readonly Action<string> _onMessage;
        private readonly ClientWebSocket _socket = new ClientWebSocket();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public StreamSocket(string url, Action<string> onMessage)
        {
            _uri = new Uri(url);
            _onMessage = onMessage;
        }

        public void Open()
        {
            _ = Task.Run(ReceiveLoop);
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[16 * 1024];
            try
            {
                await _socket.ConnectAsync(_uri, _cts.Token).ConfigureAwait(false);

                while (_socket.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                {
                    using var message = new MemoryStream();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token).ConfigureAwait(false);
                        if (result.MessageType == WebSocketMessageType.Close) return;
                        message.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        _onMessage(Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length));
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Binance stream {0} failed", _uri);
            }
        }

        public void Close()
        {
            _cts.Cancel();
            if (_socket.State == WebSocketState.Open)
            {
                try
                {
                    _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).Wait(TimeSpan.FromSeconds(2));
                }
                catch (Exception)
                {
                    // closing a dead socket, nothing to do
                }
            }
        }

        public void Dispose()
        {
            _socket.Dispose();
            _cts.Dispose();
        }
    }
}
