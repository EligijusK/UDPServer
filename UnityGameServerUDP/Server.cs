
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace UnityGameServerUDP
{
    class Server
    {
        public static int MaxPlayers { get; private set; }
        public static int Port { get; private set; }
        private static UdpClient listener;
        private static List<string> connectedIP;
        private static List<string> disconnectedIP;
        private static Player[] playerArray;
        private static int connected = 0;
        private static BitArray receivedMessage;
        private static ServerCommands commands;
        private static DateTime time;
        private static double timer = 0;
        private static double disconnectedListResetTime = 0;
        public List<string> otherConnectionCount { get; private set; }
        public List<string> disconnectedFromOthersCount { get; private set; }
        private static object sendLock = new object();
        private Attack attackHandler;

        public void Run(int maxPlayerCount, double resetTimeDisconecctedList, int port)
        {
          
            MaxPlayers = maxPlayerCount;
            Port = port;
            listener = new UdpClient(port);
            playerArray = new Player[maxPlayerCount];
            connectedIP = new List<string>();
            disconnectedIP = new List<string>();
            commands = new ServerCommands(this);
            time = DateTime.Now;
            disconnectedListResetTime = resetTimeDisconecctedList;
            otherConnectionCount = new List<string>();
            disconnectedFromOthersCount = new List<string>();
            UserSetup();
            attackHandler = new Attack(this, playerArray);
            sendLock = new object();
            MessageHandler(); // this always should be last

            //listener.Close();

        }

        private void MessageHandler()
        {
            string received_data;
            Byte[] receive_byte_array;
            try
            {
                while (true)
                {
                    
                    //Console.WriteLine("Waiting for broadcast");
                    
                    IPEndPoint user = new IPEndPoint(IPAddress.Any, Port);
                    receive_byte_array = listener.Receive(ref user);
                    //Console.WriteLine("Received a broadcast from {0}", user.ToString());

                    receivedMessage = new BitArray(receive_byte_array);

                    //listener.Send(receive_byte_array, receive_byte_array.Length, user);
                    

                    received_data = Encoding.ASCII.GetString(receive_byte_array, 0, receive_byte_array.Length);
                    //Console.WriteLine(receive_byte_array);

                    int packetTypeCount = (int)BitFunctions.CountBits((int)ServerCommands.PacketType.NumberOfValues);

                    BitArray packetTypeBits = BitFunctions.BitsReverse(BitFunctions.Range(0, packetTypeCount, receivedMessage));
                    int packetType = (int)BitFunctions.BitArrayToUInt(packetTypeBits);

                    if (packetType == (int)ServerCommands.PacketType.ServerCommand)
                    {

                        uint commandLen = BitFunctions.CountBits((int)ServerCommands.ServerCommand.ConnectedOther);
                        int index = 0;
                        BitArray packetLenBits = BitFunctions.BitsReverse(BitFunctions.Range(packetTypeCount, (int)commandLen, receivedMessage));
                        int commandIndex = (int)BitFunctions.BitArrayToUInt(packetLenBits);

                        if (Enum.IsDefined(typeof(ServerCommands.ServerCommand), commandIndex))
                        {
                            ServerCommands.ServerCommand command = (ServerCommands.ServerCommand)commandIndex;

                            if (command == ServerCommands.ServerCommand.Connect)
                            {
                                commands.AddUserForConnection(user);
                                commands.Command(command);
                            }
                            else if (command == ServerCommands.ServerCommand.ConnectedOther)
                            {
                                commands.AddData(user.ToString());
                                commands.Command(command);
                            }

                            if (connectedIP.Contains(user.ToString()))
                            {
                                if (command == ServerCommands.ServerCommand.Disconnect)
                                {

                                    int playerCount = (int)BitFunctions.CountBits(Server.MaxPlayers);
                                    packetLenBits = BitFunctions.BitsReverse(BitFunctions.Range((int)(1 + commandLen), playerCount, receivedMessage));
                                    int res = (int)BitFunctions.BitArrayToUInt(packetLenBits);
                                    commands.AddData(res);
                                    commands.AddUserForConnection(user);
                                    byte[] message = commands.CommandPacket(ServerCommands.ServerCommand.Disconnected);
                                    listener.Send(message, message.Length, user);
                                    Console.WriteLine("Disconnected from server: " + user.ToString() + " " + commands.GetMessage());
                                }
                                else if (command == ServerCommands.ServerCommand.DisconnectedFromOther)
                                {
                                    commands.AddData(user.ToString());
                                    commands.Command(command);
                                    Console.WriteLine("trying to disconnect");
                                }
                                // next console commands
                                commands.Command((ServerCommands.ServerCommand)commandIndex);
                            }
                            if (!connectedIP.Contains(user.ToString()) && disconnectedIP.Contains(user.ToString()))
                            {
                                if (command == ServerCommands.ServerCommand.Disconnect)
                                {

                                    int playerCount = (int)BitFunctions.CountBits(Server.MaxPlayers);
                                    packetLenBits = BitFunctions.BitsReverse(BitFunctions.Range((int)(1 + commandLen), playerCount, receivedMessage));
                                    int res = (int)BitFunctions.BitArrayToUInt(packetLenBits);
                                    commands.AddData(res);
                                    commands.AddUserForConnection(user);
                                    commands.Command((ServerCommands.ServerCommand)commandIndex);
                                    byte[] message = commands.CommandPacket(ServerCommands.ServerCommand.Disconnected);
                                    listener.Send(message, message.Length, user);
                                    Console.WriteLine("Disconnected from server: " + user.ToString() + " " + commands.GetMessage());

                                }

                            }


                        }

                        //Console.WriteLine("message To Server");

                    }
                    else if (packetType == (int)ServerCommands.PacketType.UserServer)
                    { 
                        
                    }
                    else
                    {
                        if (connectedIP.Contains(user.ToString()))
                        {
                            BroadcastMessageAll(receive_byte_array);
                            //Console.WriteLine(receive_byte_array);
                        }




                    }

                    if (timer > disconnectedListResetTime)
                    {
                        disconnectedIP = new List<string>();
                        time = DateTime.Now;
                    }

                    timer = (DateTime.Now - time).TotalSeconds;
                    //Console.WriteLine("data follows \n{0}\n\n", received_data);

                }



            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void MessagesForOtherReacivingConnection(byte[] message)
        {
            byte[] messageCopy = message;
            DateTime time = DateTime.Now;
            double sec = 0;
            while (otherConnectionCount.Count <= connectedIP.Count)
            {
                sec = (DateTime.Now - time).TotalMilliseconds;
                if (sec > 20)
                {
                    BroadcastMessageAllButReceived(messageCopy, otherConnectionCount);
                    time = DateTime.Now;
                }
                //Console.WriteLine("test other players for first user");
            }


        }

        public void MessagesForOtherReacivingDisconnection(byte[] message)
        {
            byte[] messageCopy = message;
            DateTime time = DateTime.Now;
            double sec = 0;
            while (disconnectedFromOthersCount.Count <= connectedIP.Count)
            {
                sec = (DateTime.Now - time).TotalMilliseconds;
                if (sec > 250)
                {
                    BroadcastMessageAllButReceived(messageCopy, disconnectedFromOthersCount);
                    time = DateTime.Now;
                }
                //Console.WriteLine("test other players for first user");
            }


        }


        public void UserSetup()
        {
            for (int i = 0; i < MaxPlayers; i ++)
            {
                playerArray[i] = new Player(i+1, this);
            }
        }

        public static void UserConnects()
        {
            connected++;
        }

        public static void Connect(IPEndPoint user)
        {
            if (connected < MaxPlayers)
            {
                for (int i = 0; i < MaxPlayers; i++)
                {
                    if (playerArray[i].isConnected() && playerArray[i].CheckUser(user))
                    {
                        playerArray[i].ConectToServer(user, listener, commands);
                        break;
                    }
                    else if (!playerArray[i].isConnected()) 
                    {
                        playerArray[i].ConectToServer(user, listener, commands);
                        break;
                    }
                }
            }
            else 
            {
                //string text_to_send = "full";
                byte[] send_buffer = commands.CommandPacket(ServerCommands.ServerCommand.Full);
                listener.Send(send_buffer, send_buffer.Length, user);
            }
        }

        public static Player[] GetConnected()
        {
            return playerArray;
        }

        public static void UserDisconnect()
        {
            connected--;
        }

        public static void BroadcastMessageAll(byte[] message)
        {
            lock (sendLock)
            {
                for (int i = 0; i < MaxPlayers; i++)
                {
                    if (playerArray[i].isConnected())
                    {
                        listener.Send(message, message.Length, playerArray[i].endPoint);
                    }
                }
            }

        }

        public static void BroadcastMutipleMessageAll(byte[] message, int length)
        {
            for (int messageCount = 0; messageCount < length; messageCount++)
            {
                BroadcastMessageAll(message);

            }
        }

        private static void BroadcastMessageAllButSender(byte[] message, int index)
        {
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (playerArray[i].isConnected() && index != i)
                {
                    listener.Send(message, message.Length, playerArray[i].endPoint);
                }
            }

        }

        private static void BroadcastMessageAllButReceived(byte[] message, List<string> otherConnectedCount)
        {
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (playerArray[i].isConnected() && !otherConnectedCount.Contains(playerArray[i].endPoint.ToString()))
                {
                    listener.Send(message, message.Length, playerArray[i].endPoint);
                }
            }

        }

        public static void AddIPToList(string IP)
        {
            connectedIP.Add(IP);
        }

        public static void AddIPToDisconnectList(string IP)
        {
            disconnectedIP.Add(IP);
        }

        public static void RemoveIPFromList(string IP)
        {
            connectedIP.Remove(IP);
            for (int i = 0; i < connectedIP.Count; i++)
            {
                Console.WriteLine(connectedIP[i]);
            }
        }

        public void AddOtherReceivedAddress(string address)
        {
            if(!otherConnectionCount.Contains(address))
            {
                otherConnectionCount.Add(address);
            }
        }

        public void ResetReceivedConnectionOtherList()
        {
            otherConnectionCount = new List<string>();
        }

        public void AddOtherReceivedDisconnectedAddress(string address)
        {
            if(!disconnectedFromOthersCount.Contains(address))
            {
                disconnectedFromOthersCount.Add(address);
            }
        }
        public void ResetReceivedDisconnectionOtherList()
        {
            disconnectedFromOthersCount = new List<string>();
        }


    }
}
