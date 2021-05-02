
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Diagnostics;
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
        private static int minPlayersToStart;
        private int spawnPosCount;
        private bool serverWorks = false;
        private bool startTimerCheck = false;
        private int minutesAfterStart;
        private Stopwatch startTimer;
        private Thread threadStartTimer;
        private List<int> takenPositions = new List<int>();
        public void Run(int maxPlayerCount, int minPlayerCount, int spawnPosCount, int minutesAfterStart, double resetTimeDisconecctedList, int port)
        {
            var externalip = new WebClient().DownloadString("https://ipv4.icanhazip.com/").TrimEnd();
            Console.WriteLine("ip address: " + externalip + ":" + port.ToString());
            this.minutesAfterStart = minutesAfterStart;
            this.spawnPosCount = spawnPosCount;
            serverWorks = true;
            MaxPlayers = maxPlayerCount;
            minPlayersToStart = minPlayerCount;
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
            startTimer = new Stopwatch();
            UserSetup();
            attackHandler = new Attack(this, playerArray);
            sendLock = new object();

            threadStartTimer = new Thread(() => StartTimer());
            threadStartTimer.Start();

            MessageHandler(); // this always should be last

            

            //listener.Close();

        }

        public void StopServer()
        {

            serverWorks = false;
            listener.Close();

        }

        private void MessageHandler()
        {
            string received_data;
            Byte[] receive_byte_array;
            try
            {
                while (serverWorks)
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
                                    packetLenBits = BitFunctions.BitsReverse(BitFunctions.Range((int)(packetTypeCount + commandLen), playerCount, receivedMessage));
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
                                    packetLenBits = BitFunctions.BitsReverse(BitFunctions.Range((int)(packetTypeCount + commandLen), playerCount, receivedMessage));
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
                    else
                    {
                        Packet packet = new Packet(receive_byte_array);
                        if (packet.GetReceivedSenderId() > 0 && packet.GetReceivedSenderId() < playerArray.Length)
                        {
                            if (packetType == (int)ServerCommands.PacketType.UserServer)
                            {

                                attackHandler.AttackMessage(packet);
                            }
                            else
                            {
                                if (packet.GetReceivedSenderId() != 0)
                                {
                                   
                                    if (connectedIP.Contains(user.ToString()) && !playerArray[packet.GetReceivedSenderId() - 1].PlayerIsDead())
                                    {
                                        BroadcastMessageAll(receive_byte_array);
                                        //Console.WriteLine(receive_byte_array);
                                    }
                                }

                            }
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
            
            byte[] messageCopy = new byte[message.Length];
            Array.Copy(message, messageCopy, message.Length);
            DateTime time = DateTime.Now;
            double sec = 0;
            while (serverWorks && otherConnectionCount.Count <= connectedIP.Count)
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

            byte[] messageCopy = new byte[message.Length];
            Array.Copy(message, messageCopy, message.Length);
            DateTime time = DateTime.Now;
            double sec = 0;
            while (serverWorks && disconnectedFromOthersCount.Count <= connectedIP.Count)
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


        private void StartTimer()
        {
            ServerCommands commandsForThread = new ServerCommands(this);
            long elapsedMilliseconds = 0;
            while (serverWorks)
            {
                if (CheckMinConnected() && !startTimerCheck)
                {
                    startTimerCheck = true;
                    elapsedMilliseconds = 0;
                }
                else if (startTimerCheck)
                {
                    int time = (minutesAfterStart * 60) - (int)(elapsedMilliseconds / 1000);
                    if (time > -1)
                    {
                        
                        Console.WriteLine(time);
                        commandsForThread.AddData(time);
                        byte[] send_buffer = commandsForThread.CommandPacket(ServerCommands.ServerCommand.Timer);
                        BroadcastMutipleMessageAll(send_buffer, 1);
                    }
                    elapsedMilliseconds += 50;
                    Thread.Sleep(50);

                }
                if (!CheckMinConnected() && startTimerCheck)
                {
                    elapsedMilliseconds = 0;
                    startTimerCheck = false;
                }
            }
        }


        public void UserSetup()
        {
            for (int i = 0; i < MaxPlayers; i ++)
            {
                
                playerArray[i] = new Player(i+1, this);
            }
        }

        public int GenerateRandomPos()
        {
            Random rand = new Random();
            int position = rand.Next(0, spawnPosCount);
            while (takenPositions.Contains(position))
            {
                position = rand.Next(0, spawnPosCount);
            }
            return position;
        }

        public void RemoveRandomPosFromTaken(int pos)
        {
            takenPositions.Remove(pos);
        }

        public bool PlayerWin()
        {
            int countAlive = 0;
            foreach (Player player in playerArray)
            {
                if (!player.PlayerIsDead() && player.isConnected())
                {
                    countAlive++;
                }
            }
            return countAlive == 1;
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
                    if (playerArray[i].CheckUser(user))
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

        public static bool CheckMinConnected()
        {
            int countConnected = 0;
            foreach (Player player in playerArray)
            {
                if (player.isConnected())
                {
                    countConnected++;
                }
            }
            return countConnected >= minPlayersToStart;

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

        public static void ActionAfterThread(Action action, Thread thread)
        {
            while (thread.IsAlive)
            { }
            action();
        }

        public static void BroadcastMutipleMessageAll(byte[] message, int length, Thread thread)
        {
            while (thread.IsAlive)
            { }
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

        public static void BroadcastMultipleMessageToReceiver(byte[] message, int count, int index)
        {
            for (int i = 0; i < count; i++)
            {
                BroadcastMessageToSpecificReceiver(message, index);
            }

        }

        private static void BroadcastMessageToSpecificReceiver(byte[] message, int index)
        {
            if (playerArray[index].isConnected())
            {
                listener.Send(message, message.Length, playerArray[index].endPoint);
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
