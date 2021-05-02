using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using Newtonsoft.Json;

namespace UnityGameServerUDP
{
    class Attack
    {
        private Server server;
        private Dictionary<int, (Packet, Packet)> attackPackets;

        private Dictionary<string, int> wepons = new Dictionary<string, int>();

        private int firstPacketId = -1;
        private int secondPacketId = -1;

        private int requestTimeout = 2000;

        private string filePath = "";

        private Player[] players;

        public Attack(Server serverRef, Player[] players)
        {
            server = serverRef;
            this.players = players;
            attackPackets = new Dictionary<int, (Packet, Packet)>();
            wepons = new Dictionary<string, int>();

            filePath = Path.Combine(Environment.CurrentDirectory, "Weapons.json");
            using (StreamReader file = File.OpenText(@filePath))
            {
                string fileText = file.ReadToEnd();
                wepons = JsonConvert.DeserializeObject<Dictionary<string, int>>(fileText);
            }

            
        }

        public void AttackMessage(Packet packet)
        {
            int attackedId = packet.GetInt();
            int attackId = packet.GetInt();

            //Console.WriteLine("attackers id: " + packet.GetReceivedSenderId() + " attacked id: " + attackedId + " attackId: " + attackId);
            //Console.WriteLine();

            //Console.WriteLine("attack package");
            if (!attackPackets.ContainsKey(attackId) && firstPacketId != packet.GetPackageIndex() && attackedId != packet.GetReceivedSenderId())
            {
                attackPackets.Add(attackId, (packet, null));
                firstPacketId = packet.GetPackageIndex();
                Thread thred = new Thread(() => ThreadTimer(attackId));
                thred.Start();
            }
            else if (!attackPackets.ContainsKey(attackId) && secondPacketId != packet.GetPackageIndex() && attackedId == packet.GetReceivedSenderId())
            {
                attackPackets.Add(attackId, (null, packet));
                secondPacketId = packet.GetPackageIndex();
                Thread thred = new Thread(() => ThreadTimer(attackId));
                thred.Start();
            }
            else if (attackPackets.ContainsKey(attackId) && attackPackets[attackId].Item2 == null && secondPacketId != packet.GetPackageIndex() && attackedId == packet.GetReceivedSenderId())
            { 
                attackPackets[attackId] = (attackPackets[attackId].Item1, packet);
                secondPacketId = packet.GetPackageIndex();
            }
            else if (attackPackets.ContainsKey(attackId) && firstPacketId != packet.GetPackageIndex() && attackedId != packet.GetReceivedSenderId() && attackPackets[attackId].Item1 == null)
            {
                attackPackets[attackId] = (packet, attackPackets[attackId].Item2);
                secondPacketId = packet.GetPackageIndex();
            }
            
        }

        public void ThreadTimer(int attackId)
        {

            int timer = 0;
            while (timer < requestTimeout)
            {
                if (attackPackets.ContainsKey(attackId) && attackPackets[attackId].Item1 != null && attackPackets[attackId].Item2 != null)
                {
                    HandleAttack(attackId);
                    break;
                }
                timer++;
                Thread.Sleep(1);
            }
            if (attackPackets.ContainsKey(attackId) && attackPackets[attackId].Item1 != null && attackPackets[attackId].Item2 != null)
            {
                HandleAttack(attackId);

            }
            else 
            {
                attackPackets.Remove(attackId);
                firstPacketId = -1;
                secondPacketId = -1;
            }

        }

        public void HandleAttack(int attackId)
        {
            Packet playerOne = attackPackets[attackId].Item1;
            Packet playerTwo = attackPackets[attackId].Item2;
            int playerOneAttack = playerOne.GetInt();
            int playerTwoAttack = playerTwo.GetInt();
            int healthPrefix = playerTwo.GetInt();
            int deathPrefix = playerTwo.GetInt();
            int winPrefix = playerTwo.GetInt();
            //Console.WriteLine("Working attack");

            if (GetDamageAt(playerOneAttack) == GetDamageAt(playerTwoAttack))
            {
                
                playerTwo.SetupHeader();
                playerTwo.SetPacketType(Packet.PacketType.User);
                playerTwo.AddInt(GetDamageAt(playerTwoAttack));
                byte[] message = playerTwo.CreatePacket();
                players[playerTwo.GetReceivedSenderId() - 1].Attack(GetDamageAt(playerOneAttack), playerTwo, healthPrefix, deathPrefix, playerOne.GetReceivedSenderId(), winPrefix);
                Thread thread = new Thread(() => Server.BroadcastMutipleMessageAll(message, 200));
                thread.Start();
                attackPackets.Remove(attackId);
                firstPacketId = -1;
                secondPacketId = -1;
            }
            else
            {
                
                playerTwo.SetupHeader();
                playerTwo.SetPacketType(Packet.PacketType.User);
                playerTwo.AddInt(GetDamageAt(playerTwoAttack));
                byte[] message = playerTwo.CreatePacket();
                players[playerTwo.GetReceivedSenderId() - 1].Attack(GetDamageAt(playerTwoAttack), playerTwo, healthPrefix, deathPrefix, playerOne.GetReceivedSenderId(), winPrefix);
                Thread thread = new Thread(() => Server.BroadcastMutipleMessageAll(message, 200));
                thread.Start();
                attackPackets.Remove(attackId);
                firstPacketId = -1;
                secondPacketId = -1;
            }
            

        }


        public int GetDamageAt(int index)
        {
            return wepons.ElementAt(index).Value;
        }

    }
}
