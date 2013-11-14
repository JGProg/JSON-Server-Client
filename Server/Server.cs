using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using MI2200TestBench.Printer;
using System.Web;
using System.Reflection;
using MI2200TestBench;
using MI2200TestBench.Applicator;

namespace ServerNamespace
{
    public class Server
    {
        #region Properties

        // Events
        public event ClientConnectedEventHandler ClientConnected;
        public event ClientConnectedEventHandler ClientExited;
        public event ServerDataReceivedEventHandler DataReceived;
        public event EventHandler ServerRunning;
        public event EventHandler ServerHalted;

        List<object> _listObject;

        public bool Started
        {
            get { return started; }
        }
        public int NbClient
        {
            get { return clients.Count; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port_P"></param>
        /// <param name="log_P"></param>
        public Server(int port_P, List<object> listObject)
        {
            af = AddressFamily.InterNetwork;
            _listObject = listObject;

            _port = port_P;
            this.sock = new Socket(af, SocketType.Stream, ProtocolType.Tcp);
        }
        #endregion

        #region public method

        #region Listening
        /// <summary>
        /// Start server
        /// </summary>
        /// <exception cref="Exception">if server already started</exception>
        public void Start()
        {
            if (started)
                throw (new Exception("Server is already started."));

            // Clear and re-init the socket list
            clients = new ArrayList();
            try
            {
                // Bind the socket to the local endpoint and listen for incoming connections.        
                this.sock.Bind(new IPEndPoint(IPAddress.Any, _port));
                this.sock.Listen(100);
                started = true;
                ThreadPool.QueueUserWorkItem(new WaitCallback(Listen));
            }
            catch (Exception e)
            {
                started = false;
                throw e;
            }
        }
        #endregion

        #region Halt
        /// <summary>
        /// Halt server
        /// </summary>
        public void Halt()
        {
            // Stop the server
            started = false;
            allDone.Set();
            foreach (ClientData cd in clients)
            {
                cd.sock.Close();
                clients.Remove(cd);
            }
        }
        #endregion

        #region Send
        /// <summary>
        /// Send data to one client
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="np"></param>
        public void SendTo(int clientId, NetworkPacket np)
        {
            ClientData data = getClient(clientId);
            data.sendbuf = np.GetBuffer();
            data.sock.BeginSend(data.sendbuf, 0, data.sendbuf.Length, SocketFlags.None, new AsyncCallback(SendCallBack), data.sock);
        }

        /// <summary>
        /// Send data to all connected clients
        /// </summary>
        /// <param name="np"></param>
        public void SendToAll(NetworkPacket np)
        {
            byte[] _sendbuf;
            _sendbuf = np.GetBuffer();

            foreach (ClientData cd in clients)
            {
                // ici on travail en asynchrone afin de limité le décalage entre les clients
                cd.sock.BeginSend(_sendbuf, 0, _sendbuf.Length, SocketFlags.None, new AsyncCallback(SendCallBack), cd.sock);
            }
        }
        #endregion

        #endregion

        #region private method

        #region Listening
        /// <summary>
        /// Listen callback
        /// </summary>
        /// <param name="state"></param>
        private void Listen(object state)
        {
            // We notice that the server is running
            this.OnServerRunning(new EventArgs());
            while (started)
            {
                // Set the event to nonsignaled state.
                allDone.Reset();
                // Start an asynchronous socket to listen for connections.
                this.sock.BeginAccept(new AsyncCallback(AcceptCallback), this.sock);
                // Wait until a connection is made before continuing.
                allDone.WaitOne();
            }
        }

        /// <summary>
        /// Accept connection
        /// </summary>
        /// <param name="ar"></param>
        private void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            allDone.Set();

            Socket listener = ar.AsyncState as Socket;

            if (listener.Handle.ToInt32() != -1) // Si à -1 : socket fermé
            {
                // Get the socket that handles the client request.
                Socket handler = listener.EndAccept(ar);

                // On rajoute le client à la liste
                this.incr++;
                ClientData c = new ClientData();
                c.id = this.incr;
                c.sock = handler;
                c.readbuf = new byte[BUFFER_BYTESIZE];
                clients.Add(c);

                // On envoi un event pour indiquer la connection d'un client.
                this.OnClientConnected(new ClientConnectedEventArgs(c.id, handler));
                // On lance la boucle de reception du client

                handler.BeginReceive(c.readbuf, 0, c.readbuf.Length, SocketFlags.None, new AsyncCallback(this.ReceiveCallback), handler);
            }
        }
        #endregion

        #region Receive
        /// <summary>
        /// Receive data
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveCallback(IAsyncResult ar)
        {
            object result =null;
            Socket handler = ar.AsyncState as Socket;
            if (handler.Connected)
            {
                try
                {
                    int read = handler.EndReceive(ar);
                    if (read > 0)
                    {
                        ClientData data = getClient(handler);
                        
                        byte[] sizedBuffer = new byte[read];
                        Array.Copy(data.readbuf, sizedBuffer, read);
                        NetworkPacket np = new NetworkPacket(sizedBuffer);
                        this.OnDataReceived(new ServerDataReceivedEventArgs(np, data.id));
                        String Data = Encoding.UTF8.GetString(data.readbuf, 0, read);
                        Data += "\n\r";

                        ArrayList resultArray = (ArrayList) JSON.Deserialize(Data);
                        object objectForInvoke = null;

                        foreach (object objectSelected in _listObject)
                        {
                            if (resultArray[0].ToString() == objectSelected.GetType().ToString())
                            {
                                objectForInvoke = objectSelected;
                                break;
                            }
                        }
                        
                        if (objectForInvoke != null)
                        {
                            MethodInfo methodInfo = objectForInvoke.GetType().GetMethod(resultArray[1].ToString());
                            if (methodInfo != null)
                            {
                                int indice = 2;
                                ParameterInfo[] parameters = methodInfo.GetParameters();
                                int NumberOfParamaters = parameters.Length;
                                List<object> parametersArray = new List<object>();
                                foreach (ParameterInfo TypeOfParameters in parameters)
                                {
                                    if (TypeOfParameters.ParameterType == typeof(int))
                                    {
                                        parametersArray.Add(Convert.ToInt32(resultArray[indice]));
                                    }
                                    else if (TypeOfParameters.ParameterType == typeof(uint))
                                    {
                                        parametersArray.Add(Convert.ToUInt32(resultArray[indice]));
                                    }
                                    else if (TypeOfParameters.ParameterType == typeof(bool))
                                    {
                                        parametersArray.Add(Convert.ToBoolean(resultArray[indice]));
                                    }
                                    else if (TypeOfParameters.ParameterType == typeof(String) || TypeOfParameters.ParameterType == typeof(string))
                                    {
                                        parametersArray.Add(Convert.ToString(resultArray[indice]));
                                    }
                                    else if (TypeOfParameters.ParameterType == typeof(ushort))
                                    {
                                        parametersArray.Add(Convert.ToUInt16(resultArray[indice]));
                                    }
                                    else if ((TypeOfParameters.ParameterType.BaseType == typeof(Enum)))
                                    {
                                        parametersArray.Add(Convert.ToInt32(resultArray[indice]));
                                    }
                                    else
                                    {
                                        parametersArray.Add(resultArray[indice]);
                                    }
                                    indice++;
                                }
                                object[] Arrayparameters = parametersArray.ToArray();
                                result = methodInfo.Invoke(objectForInvoke, Arrayparameters);



                                if (result == null)
                                {
                                    result = "void";
                                }
                                ASCIIEncoding asen = new ASCIIEncoding();
                                np = new NetworkPacket(asen.GetBytes(result.ToString()));
                            }
                            else
                            {
                                PropertyInfo propertyInfo = objectForInvoke.GetType().GetProperty(resultArray[1].ToString());
                                if (propertyInfo != null)
                                {
                                    if (resultArray.Count == 2)
                                    {
                                        result = propertyInfo.GetGetMethod().Invoke(objectForInvoke, null);

                                        if (result.GetType().BaseType == typeof(Enum))
                                        {
                                            result = Convert.ToInt32(result);
                                        }
                                    }
                                    else
                                    {
                                        object[] Param = { Convert.ToUInt32(resultArray[2])};
                                        if (propertyInfo.PropertyType.BaseType == typeof(Enum))
                                        {
                                            Param[0] = Convert.ToInt32(Param[0]);
                                        }
                                        result = propertyInfo.GetSetMethod().Invoke(objectForInvoke, Param);

                                        if (result == null)
                                        {
                                            result = "void";
                                        }
                                        else if (result.GetType().BaseType == typeof(Enum))
                                        {
                                            result = Convert.ToInt32(result);
                                        }
                                        
                                    }
                                    ASCIIEncoding asen = new ASCIIEncoding();
                                    np = new NetworkPacket(asen.GetBytes(JSON.Serialize(result)));
                                }
                            }
                        }
                        
                        
                        SendTo(data.id, np);

                        data.readbuf = new byte[BUFFER_BYTESIZE];
                        handler.BeginReceive(data.readbuf, 0, data.readbuf.Length, SocketFlags.None, new AsyncCallback(this.ReceiveCallback), handler);
                    }
                    else
                    {
                        this.ClientDisconnect(handler);
                    }
                }
                catch (SocketException)
                {
                    this.ClientDisconnect(handler);
                }
                catch (Exception _e)
                {
                    LogException(_e);
                    this.ClientDisconnect(handler);
                }
            }
            else
            {
                this.ClientDisconnect(handler);
            }
        }
        #endregion

        #region Send
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ar"></param>
        private void SendCallBack(IAsyncResult ar)
        {
            Socket handler = ar.AsyncState as Socket;
            try
            {
                handler.EndSend(ar);
            }
            catch (SocketException)
            {
                ClientDisconnect(handler);
            }
            catch (Exception _e)
            {
                LogException(_e);
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        private void OnClientConnected(ClientConnectedEventArgs info)
        {
            if (ClientConnected != null)
                ClientConnected(this, info);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        private void OnClientExited(ClientConnectedEventArgs info)
        {
            if (ClientExited != null)
                ClientExited(this, info);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        private void OnDataReceived(ServerDataReceivedEventArgs info)
        {
            if (DataReceived != null)
                DataReceived(this, info);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        private void OnServerRunning(EventArgs info)
        {
            if (ServerRunning != null)
                ServerRunning(this, info);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        private void OnServerHalted(EventArgs info)
        {
            if (ServerHalted != null)
                ServerHalted(this, info);
        }
        #endregion

        #region Misc
        /// <summary>
        /// 
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        private ClientData getClient(Socket handler)
        {
            foreach (ClientData cd in clients)
            {
                if (cd.sock == handler)
                    return cd;
            }
            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        private ClientData getClient(int clientId)
        {
            foreach (ClientData cd in clients)
            {
                if (cd.id == clientId)
                    return cd;
            }
            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="handler"></param>
        private void ClientDisconnect(Socket handler)
        {
            ClientData data = getClient(handler);
            if (data != null)
            {
                clients.Remove(data);
                handler.Close();
                this.OnClientExited(new ClientConnectedEventArgs(data.id, null));
            }
        }
        #endregion

        #region Trace
        /// <summary>
        /// Log exception
        /// </summary>
        /// <param name="Exception_P">Exception à tracer</param>
        private void LogException(Exception Exception_P)
        {
            //Message
            StringBuilder strbMessage_L = new StringBuilder();
            strbMessage_L.Append("Exception: ");
            strbMessage_L.Append("- Type: ");
            strbMessage_L.Append(Exception_P.GetType());
            strbMessage_L.Append(" - Message : ");
            strbMessage_L.Append(Exception_P.Message);
            strbMessage_L.Append(" - Pile : ");
            strbMessage_L.Append(Exception_P.StackTrace);
        }
        #endregion

        #endregion

        #region private nested class
        /// <summary>
        /// Client data : data stored for each client
        /// </summary>
        private class ClientData
        {
            public int id;
            public Socket sock;
            public byte[] readbuf;
            public byte[] sendbuf;
        };
        #endregion

        #region private members
        /// <summary>
        /// 
        /// </summary>
        private int _port;
        /// <summary>
        /// 
        /// </summary>
        private ArrayList clients;
        /// <summary>
        /// 
        /// </summary>
        private Socket sock;
        /// <summary>
        /// 
        /// </summary>
        private bool started = false;
        /// <summary>
        /// 
        /// </summary>
        private int incr = -1;
        /// <summary>
        /// 
        /// </summary>
        private AddressFamily af;
        /// <summary>
        /// threading
        /// </summary>
        private ManualResetEvent allDone = new ManualResetEvent(false);

        #endregion

        #region private constante
        /// <summary>
        /// Max byte size for both emission and reception
        /// </summary>
        private const int BUFFER_BYTESIZE = 100 * 1024;
        #endregion
    }
}
