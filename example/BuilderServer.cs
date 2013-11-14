using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.IO;
using System.Collections;
using ServerNamespace;
using System.Web;
using MI2200TestBench.Printer;

namespace BuilderServerNamespace
{
    public partial class BuilderServer
    {
        private Server serv;
        private ArrayList clients = new ArrayList();

        public BuilderServer(List<object> objectList)
        {
            serv = new Server(8000, objectList);
            serv.ClientConnected += new ClientConnectedEventHandler(serv_ClientConnected);
            serv.DataReceived += new ServerDataReceivedEventHandler(serv_DataReceived);
            serv.ClientExited += new ClientConnectedEventHandler(serv_ClientExited);
            serv.ServerRunning += new EventHandler(serv_ServerRunning);
            serv.Start(); 
        }

        #region Server
        void serv_ServerRunning(object sender, EventArgs e)
        {
            object[] param = { "Host> Server running..." };
        }

        void serv_ClientExited(object sender, ClientConnectedEventArgs e)
        {
            ClientData data = getClient(e.ClientId);
            clients.Remove(data);
        }

        void serv_DataReceived(object sender, ServerDataReceivedEventArgs e)
        {
            byte[] cmd = e.Packet.GetBuffer();
        }

        void serv_ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            ClientData cd = new ClientData();
            cd.client_id = e.ClientId;
            clients.Add(cd);
        }

        // petite fonction permettant de récupèrer l'id du client dan notre list à partir de son identifiant serveur
        ClientData getClient(int ClientId)
        {
            foreach (ClientData cd in clients)
            {
                if (cd.client_id == ClientId)
                    return cd;
            }
            return null;
        }
        #endregion

    }
}