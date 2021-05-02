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

        Server server;

        public IPEndPoint endPoint { get; private set; }

        public Player(int id, Server server)
        {
            this.id = id;
            this.server = server;
        }

        public void ConectToServer(IPEndPoint endPoint, UdpClient listener, ServerCommands commands)
        {
            health = 100;
            if (this.connected != 1)
            {
                this.endPoint = new IPEndPoint(endPoint.Address, endPoint.Port);
                Server.UserConnects();

                Console.WriteLine("connected to server: " + this.endPoint.ToString());

                commands.AddData((uint)this.id);
                byte[] send_buffer = commands.CommandPacket(ServerCommands.ServerCommand.Connected);

                this.connected = 1;
                Server.AddIPToList(this.endPoint.ToString());

                listener.Send(send_buffer, send_buffer.Length, endPoint);

                server.ResetReceivedConnectionOtherList();
                server.AddOtherReceivedAddress(endPoint.ToString());

                Player[] players = Server.GetConnected();
                commands.AddData(players);
                commands.AddSecData((uint)this.id);
                send_buffer = commands.CommandPacket(ServerCommands.ServerCommand.ConnectOther);
                listener.Send(send_buffer, send_buffer.Length, endPoint);



                players = new Player[] { this };
                int index = 0;
                commands.AddData(players);
                commands.AddSecData((uint)index);
                send_buffer = commands.CommandPacket(ServerCommands.ServerCommand.ConnectOther);
                Thread threadOther = new Thread(() => server.MessagesForOtherReacivingConnection(send_buffer));
                threadOther.Start();



            }
            else
            {
                if (endPoint.ToString() == this.endPoint.ToString())
                {

                   
                    commands.AddData((uint)this.id);
                    byte[] send_buffer = commands.CommandPacket(ServerCommands.ServerCommand.Connected);
                    listener.Send(send_buffer, send_buffer.Length, endPoint);

                    Player[] players = Server.GetConnected();
                    commands.AddData(players);
                    commands.AddSecData((uint)this.id);
                    send_buffer = commands.CommandPacket(ServerCommands.ServerCommand.ConnectOther);
                    listener.Send(send_buffer, send_buffer.Length, endPoint);
                  
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
            }
        }

        public int GetPlayerId()
        {
            return id;
        }

        public void Attack(int damage)
        {
            health -= damage;
        }
        

    }
}
