namespace QDMSApp
{
    public interface IRealTimeDataServer
    {
        /// <summary>
        ///     Whether the server is running or not.
        /// </summary>
        bool ServerRunning { get; }

        void Dispose();

        /// <summary>
        ///     Starts the server.
        /// </summary>
        void StartServer();

        /// <summary>
        ///     Stops the server.
        /// </summary>
        void StopServer();
    }
}