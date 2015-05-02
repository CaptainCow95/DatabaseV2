using System;
using System.Linq;
using System.Text;

namespace DatabaseV2
{
    /// <summary>
    /// A class to help with byte array operations.
    /// </summary>
    public static class ByteArrayHelper
    {
        /// <summary>
        /// Combines multiple byte arrays into one.
        /// </summary>
        /// <param name="toCombine">The byte arrays to combine.</param>
        /// <returns>The combined byte array.</returns>
        public static byte[] Combine(params byte[][] toCombine)
        {
            byte[] result = new byte[toCombine.Sum(e => e.Length)];
            int index = 0;
            foreach (var array in toCombine)
            {
                Buffer.BlockCopy(array, 0, result, index, array.Length);
                index += array.Length;
            }

            return result;
        }

        /// <summary>
        /// Reads a boolean from a byte array.
        /// </summary>
        /// <param name="data">The byte array to read from.</param>
        /// <param name="index">The index in the byte array to read from.</param>
        /// <returns>The boolean that was read.</returns>
        public static bool ToBoolean(byte[] data, ref int index)
        {
            bool returnValue = BitConverter.ToBoolean(data, index);
            index += sizeof(bool);
            return returnValue;
        }

        /// <summary>
        /// Gets the byte array value of an integer.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The byte array that represents the integer.</returns>
        public static byte[] ToBytes(int value)
        {
            return BitConverter.GetBytes(value);
        }

        /// <summary>
        /// Gets the byte array value of an unsigned integer.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The byte array that represents the unsigned integer.</returns>
        public static byte[] ToBytes(uint value)
        {
            return BitConverter.GetBytes(value);
        }

        /// <summary>
        /// Gets the byte array value of a boolean.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The byte array that represents the boolean.</returns>
        public static byte[] ToBytes(bool value)
        {
            return BitConverter.GetBytes(value);
        }

        /// <summary>
        /// Gets the byte array value of a string.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The byte array that represents the string.</returns>
        public static byte[] ToBytes(string value)
        {
            byte[] stringData = Encoding.UTF8.GetBytes(value);
            return Combine(BitConverter.GetBytes(stringData.Length), stringData);
        }

        /// <summary>
        /// Reads an integer from a byte array.
        /// </summary>
        /// <param name="data">The byte array to read from.</param>
        /// <param name="index">The index in the byte array to read from.</param>
        /// <returns>The integer that was read.</returns>
        public static int ToInt32(byte[] data, ref int index)
        {
            int returnValue = BitConverter.ToInt32(data, index);
            index += sizeof(int);
            return returnValue;
        }

        /// <summary>
        /// Reads a string from a byte array.
        /// </summary>
        /// <param name="data">The byte array to read from.</param>
        /// <param name="index">The index in the byte array to read from.</param>
        /// <returns>The string that was read.</returns>
        public static string ToString(byte[] data, ref int index)
        {
            int length = ToInt32(data, ref index);
            string returnValue = Encoding.UTF8.GetString(data, index, length);
            index += length;
            return returnValue;
        }

        /// <summary>
        /// Reads an unsigned integer from a byte array.
        /// </summary>
        /// <param name="data">The byte array to read from.</param>
        /// <param name="index">The index in the byte array to read from.</param>
        /// <returns>The unsigned integer that was read.</returns>
        public static uint ToUInt32(byte[] data, ref int index)
        {
            uint returnValue = BitConverter.ToUInt32(data, index);
            index += sizeof(uint);
            return returnValue;
        }
    }
}