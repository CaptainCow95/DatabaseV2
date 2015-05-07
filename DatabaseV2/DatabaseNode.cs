using DatabaseV2.Networking;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading;

namespace DatabaseV2
{
    /// <summary>
    /// Represents a node in the database.
    /// </summary>
    public class DatabaseNode
    {
        /// <summary>
        /// The chord network to use as a backend.
        /// </summary>
        private readonly ChordNetwork _network;

        /// <summary>
        /// The settings of the database.
        /// </summary>
        private readonly Settings _settings;

        /// <summary>
        /// The thread to run the web interface on.
        /// </summary>
        private readonly Thread _webInterfaceThread;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseNode"/> class.
        /// </summary>
        /// <param name="settings">The settings to use.</param>
        public DatabaseNode(Settings settings)
        {
            _settings = settings;

            Logger.Init(_settings.LogLocation, _settings.LogLevel);
            _network = new ChordNetwork(_settings.Port, _settings.Nodes);

            if (_settings.EnableWebInterface)
            {
                _webInterfaceThread = new Thread(RunWebInterface);
                _webInterfaceThread.Start();
            }
        }

        /// <summary>
        /// Shuts down the node.
        /// </summary>
        public void Shutdown()
        {
            _network.Shutdown();

            if (_webInterfaceThread != null)
            {
                _webInterfaceThread.Join();
            }

            Logger.Shutdown();
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
            builder.Append("<b>Predecessor:</b> ");
            builder.Append(_network.Predecessor == null ? "null" : _network.Predecessor.ConnectionName);

            builder.Append("<br/><br/><b>Finger Table:</b>");
            foreach (var finger in _network.FingerTable)
            {
                builder.Append("<br/>");
                builder.Append(finger == null ? "null" : finger.ConnectionName);
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
            var listener = (HttpListener)result.AsyncState;
            HttpListenerContext context;
            lock (_webInterfaceThread)
            {
                if (listener.IsListening)
                {
                    context = listener.EndGetContext(result);
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

            if (page.EndsWith("/"))
            {
                page = page.Substring(0, page.Length - 1);
            }

            if (page.StartsWith("/"))
            {
                page = page.Substring(1);
            }

            NameValueCollection queryString = context.Request.QueryString;

            lock (_webInterfaceThread)
            {
                if (listener.IsListening)
                {
                    listener.BeginGetContext(ProcessWebRequest, listener);
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

        /// <summary>
        /// The run method for the web interface.
        /// </summary>
        private void RunWebInterface()
        {
            HttpListener listener;
            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add("http://*:" + (_settings.Port + 1) + "/");
                listener.Start();
            }
            catch (HttpListenerException)
            {
                listener = new HttpListener();
                listener.Prefixes.Add("http://localhost:" + (_settings.Port + 1) + "/");
                listener.Start();
            }

            listener.BeginGetContext(ProcessWebRequest, listener);
            while (_network.Running)
            {
                Thread.Sleep(100);
            }

            lock (_webInterfaceThread)
            {
                listener.Stop();
            }
        }
    }
}