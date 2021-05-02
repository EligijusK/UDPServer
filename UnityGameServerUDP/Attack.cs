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

        private int firstPacketId = 0;
        private int secondPacketId = 0;

        private int requestTimeout = 40;

        private string filePath = "";

        private Player[] players;

        public Attack(Server serverRef, Player[] players)
        {
            server = serverRef;
            this.players = players;
            attackPackets = new Dictionary<int, (Packet, Packet)>();
            wepons = new Dictionary<string, int>();

            filePath = Path.Combine(Environment.CurrentDirectory, "Weapons.json");
            Console.WriteLine("lol");
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
            if (!attackPackets.ContainsKey(attackId) && firstPacketId != packet.GetPackageIndex() && attackedId != packet.GetSenderId())
            {
                attackPackets.Add(attackId, (packet, null));
                firstPacketId = packet.GetPackageIndex();
            }
            else if (firstPacketId != packet.GetPackageIndex() && attackPackets[attackId].Item2 == null && secondPacketId != packet.GetPackageIndex())
            { 
                attackPackets[attackId] = (attackPackets[attackId].Item1, packet);
                Thread thread = new Thread(() => HandleAttack(attackId));
                thread.Name = attackedId.ToString();
                secondPacketId = packet.GetPackageIndex();
            }
        }

        public void ThradTimer(int attackId)
        {
            int timer = 0;
            while (timer < requestTimeout)
            {
                if (attackPackets[attackId].Item1 != null && attackPackets[attackId].Item2 != null)
                {
                    HandleAttack(attackId);
                    break;
                }
                timer++;
                Thread.Sleep(1);
            }
            if (attackPackets[attackId].Item1 != null && attackPackets[attackId].Item2 != null)
            {
                HandleAttack(attackId);

            }
            else 
            {
                attackPackets.Remove(attackId);
            }

        }

        public void HandleAttack(int attackId)
        {
            Packet playerOne = attackPackets[attackId].Item1;
            Packet playerTwo = attackPackets[attackId].Item2;
            int playerOneAttack = playerOne.GetInt();
            int playerTwoAttack = playerTwo.GetInt();
            if (GetDamageAt(playerOneAttack) == GetDamageAt(playerTwoAttack))
            {
                players[playerTwo.GetSenderId()].Attack(GetDamageAt(playerOneAttack));
                playerTwo.SetPacketType(Packet.PacketType.User);
                playerTwo.AddFloat(GetDamageAt(playerTwoAttack));
                byte[] message = playerTwo.CreatePacket();
                Thread thread = new Thread(() => Server.BroadcastMutipleMessageAll(message, 200));

            }
            else
            {
                players[playerTwo.GetSenderId()].Attack(GetDamageAt(playerTwoAttack));
                playerTwo.SetPacketType(Packet.PacketType.User);
                playerTwo.AddFloat(GetDamageAt(playerTwoAttack));
                byte[] message = playerTwo.CreatePacket();
                Thread thread = new Thread(() => Server.BroadcastMutipleMessageAll(message, 200));

            }
            


        }


        public int GetDamageAt(int index)
        {
            return wepons.ElementAt(index).Value;
        }

    }
}
