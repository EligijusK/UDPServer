using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace UnityGameServerUDP
{
    class Player
    {

        int connected = 0;
        int id = 0;

        int health = 0;

        bool playerIsdead = false;

        int spawnPosition = -1;

        Server server;

        Packet attacker;

        Random random;

        public IPEndPoint endPoint { get; private set; }

        public Player(int id, Server server)
        {
            this.id = id;
            this.server = server;
            this.playerIsdead = false;
        }

        public void ConectToServer(IPEndPoint endPoint, UdpClient listener, ServerCommands commands)
        {
            health = 100;
            if (this.connected != 1)
            {
                this.endPoint = new IPEndPoint(endPoint.Address, endPoint.Port);
                Server.UserConnects();

                spawnPosition = server.GenerateRandomPos();

                Console.WriteLine("connected to server: " + this.endPoint.ToString());

                commands.AddData((uint)this.id);
                commands.AddSecData((uint)spawnPosition);
                byte[] send_buffer = commands.CommandPacket(ServerCommands.ServerCommand.Connected);

                this.connected = 1;
                Server.AddIPToList(this.endPoint.ToString());

                listener.Send(send_buffer, send_buffer.Length, endPoint);

                server.ResetReceivedConnectionOtherList();
                server.AddOtherReceivedAddress(endPoint.ToString());

                Player[] players = Server.GetConnected();
                commands.AddData(players);
                commands.AddSecData((uint)this.id);
                commands.AddThirdData(false);
                send_buffer = commands.CommandPacket(ServerCommands.ServerCommand.ConnectOther);
                listener.Send(send_buffer, send_buffer.Length, endPoint);



                players = new Player[] { this };
                int index = 0;
                commands.AddData(players);
                commands.AddSecData((uint)this.id);
                commands.AddThirdData(true);
                send_buffer = commands.CommandPacket(ServerCommands.ServerCommand.ConnectOther);
                Thread threadOther = new Thread(() => server.MessagesForOtherReacivingConnection(send_buffer));
                threadOther.Start();

                send_buffer = commands.CommandPacket(ServerCommands.ServerCommand.Start);

                Thread serverStatus = new Thread(() => Server.BroadcastMultipleMessageToReceiver(send_buffer, 200, id-1));
                serverStatus.Start();

                Thread serverStatusForOthers = new Thread(() => Server.BroadcastMutipleMessageAll(send_buffer, 20));
                serverStatusForOthers.Start();

            }
            else
            {
                if (endPoint.ToString() == this.endPoint.ToString())
                {


                    commands.AddData((uint)this.id);
                    commands.AddSecData((uint)spawnPosition);
                    byte[] send_buffer = commands.CommandPacket(ServerCommands.ServerCommand.Connected);
                    listener.Send(send_buffer, send_buffer.Length, endPoint);

                    Player[] players = Server.GetConnected();
                    commands.AddData(players);
                    commands.AddSecData((uint)this.id);
                    commands.AddThirdData(false);
                    send_buffer = commands.CommandPacket(ServerCommands.ServerCommand.ConnectOther);
                    listener.Send(send_buffer, send_buffer.Length, endPoint);

                    send_buffer = commands.CommandPacket(ServerCommands.ServerCommand.Start);
                    Thread serverStatus = new Thread(() => Server.BroadcastMultipleMessageToReceiver(send_buffer, 200, id - 1));
                    serverStatus.Start();

                }
            }
        }

        public bool CheckUser(IPEndPoint endPoint)
        {
            if (connected == 1 && this.endPoint.ToString() == endPoint.ToString())
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public bool isConnected()
        {
            if (connected == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Disconnect()
        {
            if (connected == 1)
            {
                connected = 0;
                endPoint = null;
                server.RemoveRandomPosFromTaken(spawnPosition);
            }
        }

        public int GetPlayerId()
        {
            return id;
        }

        public void Attack(int damage, Packet attacked, int healthPrefix, int deathPrefix, int attackerId, int winPrefix)
        {

            health -= damage;
            if (health <= 0)
            {
                health = 0;
                attacked.SetupHeader();
                attacked.ChangeReceivePrefix(healthPrefix);
                attacked.AddInt(health);
                Console.WriteLine(health);
                byte[] message = attacked.CreatePacket();

                attacked.SetupHeader();
                attacked.ChangeReceivePrefix(deathPrefix);
                attacked.AddBool(false);

                Thread thread = new Thread(() => Server.BroadcastMutipleMessageAll(message, 200));
                thread.Start();

                byte[] wealthMessage = attacked.CreatePacket();
                Thread secThread = new Thread(() => Server.BroadcastMutipleMessageAll(wealthMessage, 200, thread));
                secThread.Start();

                attacker = attacked;
                attacker.SetSenderId(attackerId);
                attacker.SetupHeader();
                attacker.ChangeReceivePrefix(winPrefix);
                Action playerDeath = new Action(delegate
                {
                    this.PlayersDeath();
                });

                Thread actionThread = new Thread(() => Server.ActionAfterThread(playerDeath, secThread));
                actionThread.Start();

            }
            else
            {
                attacked.SetupHeader();
                attacked.ChangeReceivePrefix(healthPrefix);
                attacked.AddInt(health);
                Console.WriteLine(health);
                byte[] message = attacked.CreatePacket();

                attacked.SetupHeader();
                attacked.ChangeReceivePrefix(deathPrefix);
                attacked.AddBool(true);

                Thread thread = new Thread(() => Server.BroadcastMutipleMessageAll(message, 200));
                thread.Start();

                byte[] wealthMessage = attacked.CreatePacket();
                Thread secThread = new Thread(() => Server.BroadcastMutipleMessageAll(wealthMessage, 200, thread));
                secThread.Start();
            }

        }

        public void PlayersDeath()
        {
            playerIsdead = true;

            if (server.PlayerWin())
            {
                attacker.AddBool(true);
                byte[] wealthMessage = attacker.CreatePacket();
                int sendTo = (attacker.GetSenderId() - 1);
                Thread thread = new Thread(() => Server.BroadcastMultipleMessageToReceiver(wealthMessage, 200, sendTo));
                thread.Start();
            }
        }

        public bool PlayerIsDead()
        {
            return playerIsdead;
        }
        

    }
}
