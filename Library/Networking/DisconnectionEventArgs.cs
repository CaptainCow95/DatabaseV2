using System;

namespace Library.Networking
{
    /// <summary>
    /// The event arguments for a disconnection event.
    /// </summary>
    public class DisconnectionEventArgs : EventArgs
    {
        /// <summary>
        /// The node that was disconnected.
        /// </summary>
        private readonly NodeDefinition _node;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisconnectionEventArgs"/> class.
        /// </summary>
        /// <param name="node">The node that was disconnected.</param>
        public DisconnectionEventArgs(NodeDefinition node)
        {
            _node = node;
        }

        /// <summary>
        /// Gets the node that was disconnected.
        /// </summary>
        public NodeDefinition Node
        {
            get { return _node; }
        }
    }
}