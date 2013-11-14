using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNamespace
{
    #region Directives Using
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    #endregion

    #region Delegate
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ServerDataReceivedEventHandler(object sender, ServerDataReceivedEventArgs e);
    #endregion

    #region classe ServerDataReceivedEventArgs
    /// <summary>
    /// 
    /// </summary>
    public class ServerDataReceivedEventArgs : EventArgs
    {
        #region private members
        /// <summary>
        /// 
        /// </summary>
        private NetworkPacket packet;

        /// <summary>
        /// 
        /// </summary>
        private int clientId;
        #endregion

        #region Constructeur
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerDataReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="clientId"></param>
        public ServerDataReceivedEventArgs(NetworkPacket data, int clientId)
        {
            this.packet = data;
            this.clientId = clientId;
        }
        #endregion

        #region Obtains
        /// <summary>
        /// 
        /// </summary>
        public NetworkPacket Packet
        {
            get { return this.packet; }
        }

        /// <summary>
        /// 
        /// </summary>
        public int ClientId
        {
            get { return this.clientId; }
        }
        #endregion
    }
    #endregion

    #region delegate
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ClientConnectedEventHandler(object sender, ClientConnectedEventArgs e);
    #endregion

    #region classe ClientConnectedEventArgs
    /// <summary>
    /// 
    /// </summary>
    public class ClientConnectedEventArgs : EventArgs
    {

        #region Obtains
        /// <summary>
        /// 
        /// </summary>
        public int ClientId
        {
            get { return this._clientId; }
        }

        /// <summary>
        /// 
        /// </summary>
        public IPAddress ClientIp
        {
            get { return this._clientIp; }
        }
        /// <summary>
        /// 
        /// </summary>
        public int ClientPort
        {
            get { return this._clientPort; }
        }
        #endregion

        #region constructeur
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientConnectedEventArgs"/> class.
        /// </summary>
        /// <param name="clientId_P"></param>
        /// <param name="socket_P"></param>
        public ClientConnectedEventArgs(int clientId_P, Socket socket_P)
        {
            this._clientId = clientId_P;
            this._clientIp = null;
            this._clientPort = 0;
            if (socket_P != null)
            {
                this._clientIp = IPAddress.Parse(((IPEndPoint)socket_P.RemoteEndPoint).Address.ToString());
                this._clientPort = ((IPEndPoint)socket_P.RemoteEndPoint).Port;
            }
        }
        #endregion

        #region private members
        /// <summary>
        /// 
        /// </summary>
        private int _clientId = -1;

        /// <summary>
        /// 
        /// </summary>
        private IPAddress _clientIp;

        /// <summary>
        /// 
        /// </summary>
        private int _clientPort;
        #endregion
    }
    #endregion
}