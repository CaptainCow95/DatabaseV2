using Library.Logging;
using Library.Networking;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace DatabaseV2
{
    /// <summary>
    /// Encapsulates the web interface logic for a node.
    /// </summary>
    public class WebInterface : IDisposable
    {
        /// <summary>
        /// The network to display information about.
        /// </summary>
        private readonly Network _network;

        /// <summary>
        /// A value indicating whether the object has already been _disposed.
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// The listener for incoming requests.
        /// </summary>
        private HttpListener _listener;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebInterface"/> class.
        /// </summary>
        /// <param name="network">The network to display information about.</param>
        public WebInterface(Network network)
        {
            _network = network;
        }

        /// <summary>
        /// Disables the web interface.
        /// </summary>
        public void Disable()
        {
            lock (_listener)
            {
                if (_listener != null)
                {
                    _listener.Stop();
                }
            }
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="DatabaseNode"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Enables the web interface.
        /// </summary>
        /// <param name="port">The port to enable the web interface on.</param>
        public void Enable(int port)
        {
            Logger.Log("Enabling the web interface on port " + port, LogLevel.Info);
            lock (_listener)
            {
                try
                {
                    _listener = new HttpListener();
                    _listener.Prefixes.Add("http://*:" + port + "/");
                    _listener.Start();
                }
                catch (HttpListenerException)
                {
                    Logger.Log("Failed to enable the web interface on anything other than localhost. Please check the permissions the program is running with.", LogLevel.Warning);
                    _listener = new HttpListener();
                    _listener.Prefixes.Add("http://localhost:" + port + "/");
                    _listener.Start();
                }

                _listener.BeginGetContext(ProcessWebRequest, _listener);
            }
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="DatabaseNode"/> class.
        /// </summary>
        /// <param name="disposing">Whether to dispose of managed resources or not.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    lock (_listener)
                    {
                        ((IDisposable)_listener).Dispose();
                    }

                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// Generates the connections web page.
        /// </summary>
        /// <param name="queryString">The value of the query string.</param>
        /// <returns>The html or json of the connections web page.</returns>
        private string GenerateConnectionsPage(NameValueCollection queryString)
        {
            StringBuilder builder = new StringBuilder();
            if (queryString["json"] == "true")
            {
                builder.Append("{\"connections\":[");
                bool first = true;
                foreach (var node in _network.GetConnectedNodes())
                {
                    if (!first)
                    {
                        builder.Append(",");
                    }

                    builder.Append("\"");
                    builder.Append(node.ConnectionName);
                    builder.Append("\"");
                    first = false;
                }

                builder.Append("]}");
            }
            else
            {
                builder.Append("<html><body>");
                builder.Append("<b>Connected Nodes:</b>");

                foreach (var node in _network.GetConnectedNodes())
                {
                    builder.Append("<br/>");
                    builder.Append(node.ConnectionName);
                }

                builder.Append("</body></html>");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Generates the main web page.
        /// </summary>
        /// <returns>The html for the main web page.</returns>
        private string GenerateMainPage()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("<html><body>");
            builder.Append("<b>Connected Nodes:</b>");
            foreach (var item in _network.GetConnectedNodes())
            {
                builder.Append("<br/>");
                builder.Append(item.ConnectionName);
            }

            builder.Append("</body></html>");
            return builder.ToString();
        }

        /// <summary>
        /// Generates a response for a web interface request.
        /// </summary>
        /// <param name="page">The page that was requested.</param>
        /// <param name="queryString">The value of the query string.</param>
        /// <returns>The html response to the web interface request.</returns>
        private string GenerateWebResponse(string page, NameValueCollection queryString)
        {
            switch (page)
            {
                case "":
                    return GenerateMainPage();

                case "connections":
                    return GenerateConnectionsPage(queryString);
            }

            return "<html><body>Page not found.</body></html>";
        }

        /// <summary>
        /// Processes a web interface request.
        /// </summary>
        /// <param name="result">The listener the request came from.</param>
        private void ProcessWebRequest(IAsyncResult result)
        {
            HttpListenerContext context;
            lock (_listener)
            {
                if (_listener.IsListening)
                {
                    context = _listener.EndGetContext(result);
                }
                else
                {
                    return;
                }
            }

            string page = context.Request.RawUrl;
            if (page.Contains("?"))
            {
                page = page.Substring(0, page.IndexOf('?'));
            }

            if (page.EndsWith("/", StringComparison.Ordinal))
            {
                page = page.Substring(0, page.Length - 1);
            }

            if (page.StartsWith("/", StringComparison.Ordinal))
            {
                page = page.Substring(1);
            }

            NameValueCollection queryString = context.Request.QueryString;

            lock (_listener)
            {
                if (_listener.IsListening)
                {
                    _listener.BeginGetContext(ProcessWebRequest, _listener);
                }
            }

            string response = GenerateWebResponse(page.ToLowerInvariant(), queryString);

            byte[] buffer = Encoding.Default.GetBytes(response);
            try
            {
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();
            }
            catch
            {
                // The connection no longer exists, do nothing as no response is needed.
            }
        }
    }
}