using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

namespace ServerNamespace
{
    [Serializable]
    public class NetworkPacket
    {
        #region members
        /// <summary>
        /// 
        /// </summary>
        private byte[] buffer = new byte[0];
        #endregion

        #region public methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_data"></param>
        public void Write(byte[] _data)
        {
            byte[] newBuffer = new byte[buffer.Length + _data.Length];
            Array.Copy(buffer, newBuffer, buffer.Length);
            Array.Copy(_data, 0, newBuffer, buffer.Length, _data.Length);
            buffer = newBuffer;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_buffer"></param>
        public NetworkPacket(byte[] _buffer)
        {
            buffer = _buffer;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] GetBuffer()
        {
            return buffer;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_buffer"></param>
        public void SetBuffer(byte[] _buffer)
        {
            buffer = _buffer;
        }
        #endregion
    }
}
