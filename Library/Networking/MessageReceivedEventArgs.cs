using System;

namespace Library.Networking
{
    /// <summary>
    /// The event arguments for a message received event.
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// The message that was received.
        /// </summary>
        private readonly Message _message;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        public MessageReceivedEventArgs(Message message)
        {
            _message = message;
        }

        /// <summary>
        /// Gets the message that was received.
        /// </summary>
        public Message Message
        {
            get { return _message; }
        }
    }
}