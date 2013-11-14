using System.Collections.Generic;
using System.Net.Sockets;
using System;
using System.IO;
using System.Text;

namespace JSONProtocol
{
    public class SendJSONRequest
    {
        private string _ipAdress;
        private int _port;
        Stream _stm;
        TcpClient _tcpclnt;

        public SendJSONRequest(string ip, int port)
        {
            _ipAdress = ip;
            _port = port;
        }

        public object SendRequest(String[] CommandToSend)
        {
            TcpClient _tcpclnt = new TcpClient();
            _tcpclnt.Connect(_ipAdress, _port);
            Stream _stm = _tcpclnt.GetStream();
            String HeadOfRequest = "[";
            String EndOfRequest = "]";
            String command = String.Empty;
            String ResultOfCommand = String.Empty;

            int sizeSendCommand = CommandToSend.Length;
            command += HeadOfRequest;
            for (int i = 0; i < sizeSendCommand; i++)
            {
                if (i == sizeSendCommand - 1)
                {
                    command += CommandToSend[i];
                }
                else
                {
                    command += CommandToSend[i] + ',';
                }
            }
            command += EndOfRequest;

            ASCIIEncoding asen = new ASCIIEncoding();
            byte[] ba = asen.GetBytes(command);

            _stm.Write(ba, 0, ba.Length);

            byte[] bb = new byte[100];
            int k = _stm.Read(bb, 0, 100);

            for (int i = 0; i < k; i++)
                ResultOfCommand += Convert.ToChar(bb[i]);
            _stm.Close();
            _tcpclnt.Close();
            return ResultOfCommand;
        }
    }
}
