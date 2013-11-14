using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace ServerNamespace
{
    internal class ClientData
    {
        public int id = 0;
        public Socket sock = null ;
        public byte[] readbuf = null;
        public byte[] sendbuf = null;
    }
}
