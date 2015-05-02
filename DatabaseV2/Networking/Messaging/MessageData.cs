using System;

namespace DatabaseV2.Networking.Messaging
{
    /// <summary>
    /// Represents the data contained in a message.
    /// </summary>
    public abstract class MessageData
    {
        /// <summary>
        /// Represents the message type.
        /// </summary>
        protected enum MessageType
        {
            /// <summary>
            /// Represents a <see cref="JoinRequest"/>.
            /// </summary>
            JoinRequest,

            /// <summary>
            /// Represents a <see cref="JoinResult"/>.
            /// </summary>
            JoinResult,

            /// <summary>
            /// Represents a <see cref="ChordSuccessorRequest"/>.
            /// </summary>
            ChordSuccessorRequest,

            /// <summary>
            /// Represents a <see cref="ChordSuccessorResponse"/>.
            /// </summary>
            ChordSuccessorResponse,

            /// <summary>
            /// Represents a <see cref="ChordPredecessorRequest"/>.
            /// </summary>
            ChordPredecessorRequest,

            /// <summary>
            /// Represents a <see cref="ChordPredecessorResponse"/>.
            /// </summary>
            ChordPredecessorResponse,

            /// <summary>
            /// Represents a <see cref="ChordNotify"/>.
            /// </summary>
            ChordNotify
        }

        /// <summary>
        /// Decodes a message.
        /// </summary>
        /// <param name="data">The message data.</param>
        /// <param name="index">The current index in the data.</param>
        /// <returns>The message this data represents.</returns>
        public static MessageData Decode(byte[] data, int index)
        {
            int messageTypeId = ByteArrayHelper.ToInt32(data, ref index);
            switch ((MessageType)Enum.ToObject(typeof(MessageType), messageTypeId))
            {
                case MessageType.JoinRequest:
                    return new JoinRequest(data, index);

                case MessageType.JoinResult:
                    return new JoinResult(data, index);

                case MessageType.ChordSuccessorRequest:
                    return new ChordSuccessorRequest();

                case MessageType.ChordSuccessorResponse:
                    return new ChordSuccessorResponse(data, index);

                case MessageType.ChordPredecessorRequest:
                    return new ChordPredecessorRequest();

                case MessageType.ChordPredecessorResponse:
                    return new ChordPredecessorResponse(data, index);

                case MessageType.ChordNotify:
                    return new ChordNotify(data, index);

                default:
                    throw new Exception("Unidentified message type!");
            }
        }

        /// <summary>
        /// Encodes the data in the message.
        /// </summary>
        /// <returns>The encoded data.</returns>
        public byte[] Encode()
        {
            return ByteArrayHelper.Combine(ByteArrayHelper.ToBytes((int)GetMessageTypeId()), EncodeData());
        }

        /// <summary>
        /// Encodes the data in the message.
        /// </summary>
        /// <returns>The encoded data.</returns>
        protected abstract byte[] EncodeData();

        /// <summary>
        /// Gets the ID of the message type.
        /// </summary>
        /// <returns>The ID of the message type.</returns>
        protected abstract MessageType GetMessageTypeId();
    }
}