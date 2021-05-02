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
            Start = 7,
            Timer = 8,
            ConnectOther = 9,
            ConnectedOther = 10

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
        object thirdData;
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
            uint spawnId = (uint)secData;
            int secdataCount = (int)BitFunctions.CountBits((int)spawnId);
            message = message + Convert.ToString((uint)Server.MaxPlayers, toBase: 2).PadLeft(7, '0');
            message = message + Convert.ToString((uint)data, toBase: 2).PadLeft(playerCount, '0');
            message = message + Convert.ToString((uint)secdataCount, toBase: 2).PadLeft(7, '0');
            message = message + Convert.ToString(spawnId, toBase: 2).PadLeft(secdataCount, '0');
        }

        public void ConnectOtherPacket()
        {
            Player[] allPlayers = (Player[])data;
            int playerCount = (int)BitFunctions.CountBits(Server.MaxPlayers);
            int playerLength = 0;
            uint currentIndex = (uint)secData;
            string tempMessage = "";
            bool check = (bool)thirdData;
            for(int i = 0; i < allPlayers.Length; i++ )
            {
                if (currentIndex != allPlayers[i].GetPlayerId() && allPlayers[i].isConnected() && check == false)
                {
                    playerLength++;
                    tempMessage = tempMessage + Convert.ToString(allPlayers[i].GetPlayerId(), toBase: 2).PadLeft(playerCount, '0');
                    Console.WriteLine("other id: " + allPlayers[i].GetPlayerId());

                }
                else if (currentIndex == allPlayers[i].GetPlayerId() && allPlayers[i].isConnected() && check == true)
                {
                    playerLength++;
                    tempMessage = tempMessage + Convert.ToString(allPlayers[i].GetPlayerId(), toBase: 2).PadLeft(playerCount, '0');
                }
            }

            //(uint)playerLength
            Console.WriteLine(playerLength);
            message = message + Convert.ToString(playerLength, toBase: 2).PadLeft(playerCount, '0');
            message = message + tempMessage;

        }

        public void StartPacket()
        {
            bool check = Server.CheckMinConnected();
            int checkMess = check ? 1 : 0;
            message = message + checkMess.ToString();
        }

        public void TimerPacket()
        {
            int seconds = (int)data;
            message = message + Convert.ToString((uint)seconds, toBase: 2).PadLeft(12, '0');
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
            if (index < (allPlayers.Length + 1) && allPlayers[index-1].isConnected() && allPlayers[index-1].GetPlayerId() == index)
            {
                string ip = allPlayers[index-1].endPoint.ToString();
                Server.RemoveIPFromList(ip);
                allPlayers[index-1].Disconnect();
                Server.UserDisconnect();
                Server.AddIPToDisconnectList(ip);
                server.ResetReceivedDisconnectionOtherList();
                server.AddOtherReceivedDisconnectedAddress(ip);

                AddData((uint)allPlayers[index-1].GetPlayerId());
                byte[] send_buffer = CommandPacket(ServerCommand.DisconnectedFromOther);
                Thread threadOther = new Thread(() => server.MessagesForOtherReacivingDisconnection(send_buffer));
                threadOther.Start();
                
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

        public void AddThirdData(object data)
        {
            this.thirdData = data;
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
