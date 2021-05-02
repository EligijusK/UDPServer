using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using System.Reflection;
using System.Net;
using System.Net.Sockets;

namespace UnityGameServerUDP
{
    class ServerCommands
    {
        public enum ServerCommand : int // Server must have same enum for communication
        {
            Connect = 1,
            Disconnect = 2,
            Disconnected = 3,
            DisconnectedFromOther = 4,
            Connected = 5,
            Full = 6,
            ConnectOther = 7,
            ConnectedOther = 8

        }

        public enum PacketType
        {
            User = 0,
            ServerCommand = 1,
            UserServer = 2,
            NumberOfValues,
        }

        string message = "";
        int commandLen = 0;

        object data;
        object secData;
        IPEndPoint user;

        Server server;
        public ServerCommands(Server server)
        {
            this.server = server;
            this.commandLen = (int)BitFunctions.CountBits((int)ServerCommand.ConnectedOther); // count bit size of last index in command enum

        }

        public byte[] CommandPacket(ServerCommand commandIndex)
        {

            message = "";
            message = message + Convert.ToString((int)commandIndex, toBase: 2).PadLeft(commandLen, '0');
            
            Type thisType = this.GetType();
            MethodInfo theMethod = thisType.GetMethod(commandIndex.ToString()+"Packet");
            theMethod.Invoke(this, null);

            PacketLength();

            BitArray bits = new BitArray(this.message.Select(c => c == '1').ToArray());

            return BitFunctions.BitArrayToByteArray(bits);
        }

        public void ConnectedPacket() // function name by enum
        {
            int playerCount = (int)BitFunctions.CountBits(Server.MaxPlayers);
            message = message + Convert.ToString((uint)Server.MaxPlayers, toBase: 2).PadLeft(7, '0');
            message = message + Convert.ToString((uint)data, toBase: 2).PadLeft(playerCount, '0');
        }

        public void ConnectOtherPacket()
        {
            Player[] allPlayers = (Player[])data;
            int playerCount = (int)BitFunctions.CountBits(Server.MaxPlayers);
            int playerLength = 0;
            uint currentIndex = (uint)secData;
            string tempMessage = "";
            for(int i = 0; i < allPlayers.Length; i++ )
            {
                if(currentIndex != (i+1) && allPlayers[i].isConnected())
                {
                    playerLength++;
                    tempMessage = tempMessage + Convert.ToString(allPlayers[i].GetPlayerId(), toBase: 2).PadLeft(playerCount, '0');
                    Console.WriteLine("other id: " + allPlayers[i].GetPlayerId());
                }
            }
            //(uint)playerLength
            message = message + Convert.ToString(playerLength, toBase: 2).PadLeft(playerCount, '0');
            message = message + tempMessage;

        }

        public void FullPacket()
        { 
        
        }

        public void Disconnected()
        { 
        
        }

        public void Connected()
        { 
        
        }

        public void Full()
        {

        }

        public void ConnectOther()
        {
        
        }

        public void DisconnectedPacket()
        { 

        }

        public void DisconnectedFromOtherPacket()
        {
            int playerCount = (int)BitFunctions.CountBits(Server.MaxPlayers);
            message = message + Convert.ToString((uint)data, toBase: 2).PadLeft(playerCount, '0');
        }

        public void Command(ServerCommand commandIndex)
        {
            Type thisType = this.GetType();
            MethodInfo theMethod = thisType.GetMethod(commandIndex.ToString());
            if (theMethod != null)
            {
                theMethod.Invoke(this, null);
            }
        }

        public void Connect() // function name by enum
        {
            Server.Connect(user);
        }

        public void ConnectedOther()
        {
            string ip = (string)(data);
            server.AddOtherReceivedAddress(ip);
        }

        public void Disconnect() // function name by enum
        {
            Player[] allPlayers = Server.GetConnected();
            int index = (int)data;
            for (int i = 0; i < allPlayers.Length; i++)
            {
                if (index < (allPlayers.Length + 1) && allPlayers[i].isConnected() && allPlayers[i].GetPlayerId() == index)
                {
                    string ip = allPlayers[i].endPoint.ToString();
                    Server.RemoveIPFromList(ip);
                    allPlayers[i].Disconnect();
                    Server.UserDisconnect();
                    Server.AddIPToDisconnectList(ip);
                    server.ResetReceivedDisconnectionOtherList();
                    server.AddOtherReceivedDisconnectedAddress(ip);

                    AddData((uint)allPlayers[i].GetPlayerId());
                    byte[] send_buffer = CommandPacket(ServerCommand.DisconnectedFromOther);
                    Thread threadOther = new Thread(() => server.MessagesForOtherReacivingDisconnection(send_buffer));
                    threadOther.Start();

                    break;
                }
            }
            
            
            
        }



        public void DisconnectedFromOther()
        {
            string ip = (string)(data);
            server.AddOtherReceivedDisconnectedAddress(ip);
        }

        private void PacketLength()
        {
            int typeLen = (int)BitFunctions.CountBits((int)PacketType.NumberOfValues);
            float messageLen = this.message.Length + 7 + typeLen; // + 7 for packet length, + for consolle check
            messageLen = messageLen / 8;

            if (messageLen - Math.Truncate(messageLen) > 0)
            {
                messageLen++;
            }

            message = Convert.ToString(1, toBase: 2).PadLeft(typeLen, '0') + Convert.ToString(Math.Max(1, (int)Math.Truncate(messageLen)), toBase: 2).PadLeft(7, '0') + message;
        }

        public void AddData(object data)
        {
            this.data = data;
        }

        public void AddSecData(object data)
        {
            this.secData = data;
        }

        public void AddUserForConnection(IPEndPoint user)
        {
            this.user = user;
        }

        public string GetMessage()
        {
            return message;
        }
    }
}
