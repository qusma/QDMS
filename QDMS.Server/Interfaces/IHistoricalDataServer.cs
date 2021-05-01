namespace QDMSApp
{
    public interface IHistoricalDataServer
    {
        /// <summary>
        ///     Whether the server is running or not.
        /// </summary>
        bool ServerRunning { get; }

        void Dispose();

        /// <summary>
        ///     Start the server.
        /// </summary>
        void StartServer();

        /// <summary>
        ///     Stop the server.
        /// </summary>
        void StopServer();
    }
}