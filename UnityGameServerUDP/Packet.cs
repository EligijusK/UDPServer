using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;

namespace UnityGameServerUDP
{
    public class Packet
    {

        public enum PacketType
        {
            User = 0,
            ServerCommand = 1,
            UserServer = 2,
            NumberOfValues,
        }

        //protected static int prefixBitLen = 0;
        protected int packetPrefixBitLen = 0;
        protected int preFixBitLenFloat = 4; // 13 in bits len
        protected int preFixBitLenInt = 5; // 20 in bits len

        protected bool[] resultsInBits = new bool[768];
        protected bool[] headerInBits;
        protected int indexRefBits = 0;

        // bit count initialization;
        protected static int count = 0;

        // indexes for types
        protected uint boolIndex = 3;
        protected uint floatIndex = 1;
        protected uint intIndex = 2;

        // packet creation
        protected bool[] header = new bool[768];
        protected int headerIndex = 0;
        protected int prefixStartIndex = 0;
        protected float messageLen = 0;
        protected int packetIndex = 0;
        protected bool increaseIndexManualy = false;
        protected const byte BZ = 0, B0 = 1 << 0, B1 = 1 << 1, B2 = 1 << 2, B3 = 1 << 3, B4 = 1 << 4, B5 = 1 << 5, B6 = 1 << 6, B7 = 1 << 7;


        // receive
        protected bool[] receivedMessage;
        protected byte[] received;
        protected int currentIndex = 0;
        protected List<int> prevIndexArray;
        protected int prefixIndex = 0;
        protected int playerId = 0;
        protected uint res;
        protected int packageLen = 0;
        protected int packetReceiveIndex = 0;
        protected uint lenPrefix = 0;

        //bool to byte array

        protected int lenghtlen = 0;
        protected int bytes = 0;
        protected byte[] resultInBytes;

        //Conversion
        protected byte[] bytesForint = new byte[4];
        protected bool[] bytes2Bits = new bool[64];

        protected string packetPrefix;
        protected int senderPlayerId = 0;

        protected PacketType packetType;


        public Packet(byte[] receive)
        {
            this.currentIndex = 0;
            packetReceiveIndex = -1;

            received = receive;
            this.receivedMessage = Byte2Bool(receive);

            int typeLen = (int)CountBits((int)PacketType.NumberOfValues);

            bool[] packetConsoleBit = BitsReverseLen(receivedMessage, ref this.currentIndex, typeLen);
            res = BitArrayToUInt(packetConsoleBit);

            bool[] packetLenBits = BitsReverseLen(receivedMessage, ref this.currentIndex, 7);

            res = BitArrayToUInt(packetLenBits); // package lenght for data check
            packageLen = (int)res;

            bool[] packetIndexBits = BitsReverseLen(receivedMessage, ref this.currentIndex, 32);
            res = BitArrayToUInt(packetIndexBits); // package lenght for data check
            packetReceiveIndex = (int)res;


            int byteCountMaxPlayer = Server.MaxPlayers;
            byteCountMaxPlayer = (int)CountBits(byteCountMaxPlayer);


            packetLenBits = BitsReverseLen(receivedMessage, ref currentIndex, byteCountMaxPlayer);
            playerId = (int)BitArrayToUInt(packetLenBits);

            senderPlayerId = playerId;

            packetLenBits = BitsReverseLen(receivedMessage, ref currentIndex, 5);
            lenPrefix = BitArrayToUInt(packetLenBits);

            packetLenBits = BitsReverseLen(receivedMessage, ref currentIndex, (int)lenPrefix);
            prefixIndex = (int)BitArrayToUInt(packetLenBits);

            prevIndexArray = new List<int>();

            //PrintValues(packetLenBits, 0);
        }

        public void Next()
        {
            // calculate next index by checking typr and adding indexes
            prevIndexArray.Add(this.currentIndex);

            bool[] packetLenBits = BitsReverseLen(receivedMessage, ref this.currentIndex, 2);
            uint tempRes = BitArrayToUInt(packetLenBits); // package lenght for data check

            if (tempRes == floatIndex)
            {
                packetLenBits = BitsReverseLen(receivedMessage, ref this.currentIndex, preFixBitLenFloat);
                tempRes = BitArrayToUInt(packetLenBits); // package lenght for data check
                this.currentIndex += (int)tempRes;
                this.currentIndex++;
                this.currentIndex += 10;

            }
            else if (tempRes == intIndex)
            {
                packetLenBits = BitsReverseLen(receivedMessage, ref this.currentIndex, preFixBitLenInt);
                tempRes = BitArrayToUInt(packetLenBits); // package lenght for data check
                this.currentIndex += (int)tempRes;
                this.currentIndex++;
                //this.currentIndex += preFixBitLenInt;
            }
            else if (tempRes == boolIndex)
            {
                this.currentIndex++;
            }
        }

        public void Previous()
        {
            // list with prev current index
            this.currentIndex = prevIndexArray[prevIndexArray.Count - 1];
            prevIndexArray.Remove(this.currentIndex);

        }

        public bool TryReadFloat()
        {
            int index = this.currentIndex;
            bool[] packetLenBits = BitsReverseLen(receivedMessage, ref index, 2);
            uint tempRes = BitArrayToUInt(packetLenBits); // package lenght for data check
                                                          //Debug.Log("index of start in float: " +  this.currentIndex);

            if (tempRes == floatIndex && this.currentIndex < (packageLen * 8))
            {

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TryReadVector3()
        {
            int index = this.currentIndex;
            bool[] packetLenBits = BitsReverseLen(receivedMessage, ref index, 2);
            uint tempRes = BitArrayToUInt(packetLenBits); // package lenght for data check
                                                          //Debug.Log("index of start in float: " +  this.currentIndex);

            if (tempRes == floatIndex && index < (packageLen * 8) && (index + 11 + 1 + preFixBitLenFloat) < (packageLen * 8))
            {

                packetLenBits = BitsReverseLen(receivedMessage, ref index, preFixBitLenFloat);
                tempRes = BitArrayToUInt(packetLenBits); // package lenght for data check
                index += (int)tempRes;
                index += 11;

                if ((index + 2) < (packageLen * 8) && (index + 2 + 11 + 1 + preFixBitLenFloat) < (packageLen * 8))
                {

                    packetLenBits = BitsReverseLen(receivedMessage, ref index, preFixBitLenFloat);
                    tempRes = BitArrayToUInt(packetLenBits); // package lenght for data check
                    index += (int)tempRes;
                    index += 11;

                    if ((index + 2) < (packageLen * 8) && (index + 2 + 11 + 1 + preFixBitLenFloat) < (packageLen * 8))
                    {

                        return true;
                    }

                    else
                    {
                        return false;
                    }


                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public float GetFloat()
        {
            // read float or throw message that it can't be read
            prevIndexArray.Add(this.currentIndex);
            bool[] packetLenBits = BitsReverseLen(receivedMessage, ref this.currentIndex, 2);
            uint tempRes = BitArrayToUInt(packetLenBits); // package lenght for data check
                                                          //Debug.Log("index of start in float: " +  this.currentIndex);
            if (tempRes == floatIndex)
            {
                packetLenBits = BitsReverseLen(receivedMessage, ref this.currentIndex, preFixBitLenFloat);
                tempRes = BitArrayToUInt(packetLenBits); // package lenght for data check

                bool sign = receivedMessage[this.currentIndex];
                this.currentIndex++;

                packetLenBits = BitsReverseLen(receivedMessage, ref this.currentIndex, (int)tempRes);
                tempRes = BitArrayToUInt(packetLenBits); // package lenght for data check

                float res = (float)tempRes;

                packetLenBits = BitsReverseLen(receivedMessage, ref this.currentIndex, 10);
                tempRes = BitArrayToUInt(packetLenBits); // package lenght for data check

                float fraction = ((float)tempRes) / 1000;
                res = res + fraction;

                //Debug.Log(gg);



                if (sign)
                {
                    res = res * -1;
                }

                return res;

            }
            else
            {
                Previous();
                //PrintValues(receivedMessage, 0);
                throw new ArgumentException("Float cannot be read");

            }
        }

        public bool TryReadInt()
        {
            int index = this.currentIndex;
            bool[] packetLenBits = BitsReverseLen(receivedMessage, ref index, 2);
            uint tempRes = BitArrayToUInt(packetLenBits); // package lenght for data check
                                                          //Debug.Log("index of start in float: " +  this.currentIndex);

            if (tempRes == intIndex && this.currentIndex < (packageLen * 8))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public int GetInt()
        {
            // read int or throw message that it can't be read
            prevIndexArray.Add(this.currentIndex);
            bool[] packetLenBits = BitsReverseLen(receivedMessage, ref this.currentIndex, 2);
            uint tempRes = BitArrayToUInt(packetLenBits); // package lenght for data check
            if (tempRes == intIndex)
            {
                packetLenBits = BitsReverseLen(receivedMessage, ref this.currentIndex, preFixBitLenInt);
                tempRes = BitArrayToUInt(packetLenBits); // package lenght for data check

                bool sign = receivedMessage[this.currentIndex];
                this.currentIndex++;

                packetLenBits = BitsReverseLen(receivedMessage, ref this.currentIndex, (int)tempRes);
                tempRes = BitArrayToUInt(packetLenBits); // package lenght for data check

                int result = (int)tempRes;

                if (sign)
                {
                    result = result * -1;
                }

                return result;

            }
            else
            {
                Previous();
                //PrintValues(receivedMessage, 0);
                throw new ArgumentException("Int cannot be read");

            }
        }

        public bool TryReadBool()
        {
            int index = this.currentIndex;
            bool[] packetLenBits = BitsReverseLen(receivedMessage, ref index, 2);
            uint tempRes = BitArrayToUInt(packetLenBits); // package lenght for data check
                                                          //Debug.Log("index of start in float: " +  this.currentIndex);

            if (tempRes == intIndex && this.currentIndex < (packageLen * 8))
            {
                return true;
            }
            else
            {

                return false;
            }
        }

        public bool GetBool()
        {
            // read bool or throw message that it can't be read
            prevIndexArray.Add(this.currentIndex);
            bool[] packetLenBits = BitsReverseLen(receivedMessage, ref this.currentIndex, 2);

            uint res = BitArrayToUInt(packetLenBits); // package lenght for data check
            if (res == boolIndex)
            {
                bool value = receivedMessage[this.currentIndex];
                this.currentIndex++;
                return value;
            }
            else
            {
                Previous();
                //throw new ArgumentException("Bool cannot be read");
                return false;
            }
        }

        public int GetReceivedSenderId()
        {
            return this.playerId;
        }

        public int GetPackageIndex()
        {
            return packetReceiveIndex;
        }


        public static bool[] BitsReverseLen(bool[] bits, ref int start, int lenght)
        {
            int startTemp = start;
            bool[] a = new bool[lenght];
            bool[] b = new bool[lenght];

            for (int i = 0, j = lenght - 1; i < lenght; ++i, --j)
            {
                a[i] = bits[start + i];
                b[j] = bits[start + j];

                a[i] = a[i] ^ b[j];
                b[j] = a[i] ^ b[j];
                a[i] = a[i] ^ b[j];
            }
            start += lenght;
            return a;
        }

        public static bool[] Byte2Bool(byte[] bytes)
        {
            bool[] res = new bool[bytes.Length * 8];
            int index = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                index = i * 8;
                res[index + 0] = (bytes[i] & (1 << 0)) == 0 ? false : true;
                res[index + 1] = (bytes[i] & (1 << 1)) == 0 ? false : true;
                res[index + 2] = (bytes[i] & (1 << 2)) == 0 ? false : true;
                res[index + 3] = (bytes[i] & (1 << 3)) == 0 ? false : true;
                res[index + 4] = (bytes[i] & (1 << 4)) == 0 ? false : true;
                res[index + 5] = (bytes[i] & (1 << 5)) == 0 ? false : true;
                res[index + 6] = (bytes[i] & (1 << 6)) == 0 ? false : true;
                res[index + 7] = (bytes[i] & (1 << 7)) == 0 ? false : true;

            }
            return res;
        }


        public static uint BitArrayToUInt(bool[] array)
        {

            uint res = 0;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i])
                {
                    res |= (uint)(1 << i);
                }
            }
            return res;

        }


        //public static void SetPRefixBitLen(int prefixListLen)
        //{

        //    if (prefixListLen <= 7)
        //    {
        //        prefixBitLen = 3;
        //    }
        //    else if (prefixListLen <= 15)
        //    {
        //        prefixBitLen = 4;
        //    }
        //    else if (prefixListLen <= 31)
        //    {
        //        prefixBitLen = 5;
        //    }
        //    else if (prefixListLen <= 63)
        //    {
        //        prefixBitLen = 6;
        //    }
        //    else if (prefixListLen <= 127)
        //    {
        //        prefixBitLen = 7;
        //    }



        //}


        public static uint CountBits(int n)
        {

            int count = 0;
            while (n != 0)
            {
                count++;
                n >>= 1;
            }

            return (uint)count;

        }


        public void AddFloat(float value)
        {

            int start = (int)Math.Truncate(value);
            int sign = 0;

            if (value >= 0)
            {
                sign = 0;
            }
            else
            {
                sign = 1;
            }

            start = Math.Abs(start);


            if (start > 8191) // max 13 bits
            {
                start = 8191;
            }

            int result = (int)((Math.Abs(value) % 1) * 1000); // max 10 bits

            if (result > 999)
            {
                result = 999;
            }

            bool[] lenBitsTest = ByteArray2BitArray(IntToBytes((int)floatIndex));

            AppendBitsFromBoolArrayToBoolComplete(ref indexRefBits, 2, lenBitsTest, ref resultsInBits);

            lenBitsTest = ByteArray2BitArray(IntToBytes((int)CountBits(start)));
            AppendBitsFromBoolArrayToBoolComplete(ref indexRefBits, preFixBitLenFloat, lenBitsTest, ref resultsInBits);

            lenBitsTest = ByteArray2BitArray(IntToBytes((int)sign));

            AppendBitsFromBoolArrayToBoolComplete(ref indexRefBits, 1, lenBitsTest, ref resultsInBits);

            lenBitsTest = ByteArray2BitArray(IntToBytes(start));

            AppendBitsFromBoolArrayToBoolComplete(ref indexRefBits, (int)CountBits(start), lenBitsTest, ref resultsInBits);

            lenBitsTest = ByteArray2BitArray(IntToBytes(result));
            AppendBitsFromBoolArrayToBoolComplete(ref indexRefBits, 10, lenBitsTest, ref resultsInBits);


        }

        public void AddInt(int value)
        {

            int result = value;
            int sign = 0;

            if (value >= 0)
            {
                sign = 0;
            }
            else
            {
                sign = 1;
            }

            result = Math.Abs(result);
            if (result > 1048575) // max 20 bits
            {
                result = 1048575;
            }

            bool[] lenBitsTest = ByteArray2BitArray(IntToBytes((int)intIndex));

            AppendBitsFromBoolArrayToBoolComplete(ref indexRefBits, 2, lenBitsTest, ref resultsInBits);

            lenBitsTest = ByteArray2BitArray(IntToBytes((int)CountBits(result)));

            AppendBitsFromBoolArrayToBoolComplete(ref indexRefBits, preFixBitLenInt, lenBitsTest, ref resultsInBits);

            lenBitsTest = ByteArray2BitArray(IntToBytes((int)sign));

            AppendBitsFromBoolArrayToBoolComplete(ref indexRefBits, 1, lenBitsTest, ref resultsInBits);

            lenBitsTest = ByteArray2BitArray(IntToBytes(result));

            AppendBitsFromBoolArrayToBoolComplete(ref indexRefBits, (int)CountBits(result), lenBitsTest, ref resultsInBits);

        }

        public void AddBool(bool value)
        {

            int result = value ? 1 : 0;

            bool[] lenBitsTest = ByteArray2BitArray(IntToBytes((int)boolIndex));

            AppendBitsFromBoolArrayToBoolComplete(ref indexRefBits, 2, lenBitsTest, ref resultsInBits);

            lenBitsTest = ByteArray2BitArray(IntToBytes((int)result));

            AppendBitsFromBoolArrayToBoolComplete(ref indexRefBits, 1, lenBitsTest, ref resultsInBits);


        }

        public void SetupHeader()
        {

            int byteCountMaxPlayer = Server.MaxPlayers;
            byteCountMaxPlayer = (int)CountBits(byteCountMaxPlayer); // bit count for player index

            int bitLen = byteCountMaxPlayer + 5 + (int)lenPrefix;
            headerInBits = new bool[bitLen];

            int headerIndex = 0;

            bool[] bytes = ByteArray2BitArray(IntToBytes((int)senderPlayerId));

            AppendBitsFromBoolArrayToBoolComplete(ref headerIndex, byteCountMaxPlayer, bytes, ref headerInBits);

            bytes = ByteArray2BitArray(IntToBytes((int)lenPrefix));

            AppendBitsFromBoolArrayToBoolComplete(ref headerIndex, 5, bytes, ref headerInBits);

            int prefixIndex = this.prefixIndex;

            bytes = ByteArray2BitArray(IntToBytes(prefixIndex));
            prefixStartIndex = headerIndex;
            AppendBitsFromBoolArrayToBoolComplete(ref headerIndex, (int)lenPrefix, bytes, ref headerInBits);

            Array.Clear(resultsInBits, 0, resultsInBits.Length);
            indexRefBits = 0;
            AppendBitsFromBoolArrayToBool(ref indexRefBits, headerInBits.Length, headerInBits, ref resultsInBits);

        }


        public void ChangeReceivePrefix(int prefix)
        {

            headerIndex = prefixStartIndex;
            bool[] bytes = ByteArray2BitArray(IntToBytes(prefix));
            AppendBitsFromBoolArrayToBoolComplete(ref headerIndex, (int)lenPrefix, bytes, ref headerInBits);

            Array.Clear(resultsInBits, 0, resultsInBits.Length);
            indexRefBits = 0;
            AppendBitsFromBoolArrayToBool(ref indexRefBits, headerInBits.Length, headerInBits, ref resultsInBits);
        }

        public byte[] CreatePacket()
        {

            Array.Clear(header, 0, header.Length);

            headerIndex = 0;
            int bitCount = (int)CountBits((int)PacketType.NumberOfValues);
            messageLen = indexRefBits + bitCount + 7 + 32; // + 7 for packet length, 1 + for console check, 1 + for lenght, 32 + for packet index
            messageLen = messageLen / 8;



            if (messageLen - Math.Truncate(messageLen) > 0)
            {
                messageLen++;
            }


            if (!increaseIndexManualy)
            {
                packetIndex = packetIndex + 7;
            }

            //Debug.Log("index milisecs: " + packetIndex + " index ticks: " + lastTwoTics);

            messageLen = (int)Math.Truncate(messageLen);

            int tempLen = (int)messageLen;

            bool[] lenBitsTest = ByteArray2BitArray(IntToBytes((int)packetType));

            AppendBitsFromBoolArrayToBoolComplete(ref headerIndex, bitCount, lenBitsTest, ref header);


            lenBitsTest = ByteArray2BitArray(IntToBytes(Math.Max(1, (int)messageLen)));

            AppendBitsFromBoolArrayToBoolComplete(ref headerIndex, 7, lenBitsTest, ref header);


            lenBitsTest = ByteArray2BitArray(IntToBytes(packetIndex));

            AppendBitsFromBoolArrayToBoolComplete(ref headerIndex, 32, lenBitsTest, ref header);


            AppendBitsFromBoolArrayToBool(ref headerIndex, indexRefBits, resultsInBits, ref header);

            //StringBuilder sb = new StringBuilder();
            //foreach (var bit in header)
            //{
            //    sb.Append(bit ? "1" : "0");
            //}

            //Debug.Log("------------------------------------------------");
            //Debug.Log(sb.ToString());
            //Debug.Log(res);
            //Debug.Log("------------------------------------------------");

            //Debug.Log("packet length: " + (tempLen) + " actual package len: " + (headerIndex/8));

            return PackBoolsInByteArray(header, headerIndex);
        }

        public void SetIndex(int index)
        {
            packetIndex = index;
        }

        public int ReturnCurrentSendPacketIndex()
        {
            return packetIndex;
        }

        public void SetManualyIncresedIndex(bool value)
        {
            increaseIndexManualy = value;
        }

        public bool IncresaseManulayIndexCheck()
        {
            return increaseIndexManualy;
        }

        public void SetPacketType(PacketType packetType)
        {
            this.packetType = packetType;
        }

        public void AppendBitsFromBoolArrayToBoolComplete(ref int index, int len, bool[] bitArray, ref bool[] bitBuilder)
        {
            for (int i = len - 1; i > -1; i--)
            {
                bitBuilder[index] = bitArray[i];
                index++;
            }
        }

        public void AppendBitsFromBoolArrayToBool(ref int index, int len, bool[] bitArray, ref bool[] bitBuilder)
        {
            for (int i = 0; i < len; i++, index++)
            {
                bitBuilder[index] = bitArray[i];
            }
        }


        public bool[] ByteArray2BitArray(byte[] bytes)
        {


            for (var x = 0; x < bytes2Bits.Length; x++)
            {
                bytes2Bits[x] = false;
            }

            for (int i = 0; i < bytes.Length * 8; i++)
            {
                if ((bytes[i / 8] & (1 << ((i % 8)))) > 0) // (7 - (i % 8)) non reverse
                    bytes2Bits[i] = true;
            }
            return bytes2Bits;
        }




        public static byte[] PackBoolsInByteArray(bool[] bools, int len)
        {

            int rem = len & 0x07; // hint: rem = len % 8.


            byte[] byteArr = rem == 0 // length is a multiple of 8? (no remainder?)
                ? new byte[len >> 3] // -yes-
                : new byte[(len >> 3) + 1]; // -no-


            byte b;
            int i = 0;
            for (int mul = len & ~0x07; i < mul; i += 8) // hint: len = mul + rem.
            {
                b = bools[i] ? B0 : BZ;
                if (bools[i + 1]) b |= B1;
                if (bools[i + 2]) b |= B2;
                if (bools[i + 3]) b |= B3;
                if (bools[i + 4]) b |= B4;
                if (bools[i + 5]) b |= B5;
                if (bools[i + 6]) b |= B6;
                if (bools[i + 7]) b |= B7;

                byteArr[i >> 3] = b;
                //yield return b;
            }

            if (rem != 0) // take care of the remainder...
            {
                b = bools[i] ? B0 : BZ; // (there is at least one more bool.)

                switch (rem) // rem is [1:7] (fall-through switch!)
                {
                    case 7:
                        if (bools[i + 6]) b |= B6;
                        goto case 6;
                    case 6:
                        if (bools[i + 5]) b |= B5;
                        goto case 5;
                    case 5:
                        if (bools[i + 4]) b |= B4;
                        goto case 4;
                    case 4:
                        if (bools[i + 3]) b |= B3;
                        goto case 3;
                    case 3:
                        if (bools[i + 2]) b |= B2;
                        goto case 2;
                    case 2:
                        if (bools[i + 1]) b |= B1;
                        break;
                        // case 1 is the statement above the switch!
                }

                byteArr[i >> 3] = b; // write the last byte to the array.
                                     //yield return b; // yield the last byte.
            }

            return byteArr;
        }


        public byte[] IntToBytes(int value)
        {

            unchecked
            {
                bytesForint[3] = (byte)(value >> 24);
                bytesForint[2] = (byte)(value >> 16);
                bytesForint[1] = (byte)(value >> 8);
                bytesForint[0] = (byte)value;
            }

            return bytesForint;
        }

        public static void PrintValues(BitArray b, int realRes)
        {

            IEnumerator enumerator = b.GetEnumerator();
            string res = "";
            while (enumerator.MoveNext())
            {
                //Debug.Log(enumerator.Current);
                if (bool.Parse(enumerator.Current.ToString()))
                {
                    res = res + "1";
                }
                else
                {
                    res = res + "0";
                }
            }



        }

        public void SetSenderId(int senderId)
        {
            senderPlayerId = senderId;
        }

        public int GetSenderId()
        {
            return senderPlayerId;
        }



    }
}
