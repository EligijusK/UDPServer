using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace UnityGameServerUDP
{
    class BitFunctions
    {

        public static BitArray BitsReverse(BitArray bits)
        {
            int len = bits.Count;
            BitArray a = new BitArray(bits);
            BitArray b = new BitArray(bits);

            for (int i = 0, j = len - 1; i < len; ++i, --j)
            {
                a[i] = a[i] ^ b[j];
                b[j] = a[i] ^ b[j];
                a[i] = a[i] ^ b[j];
            }

            return a;
        }

        public static BitArray Range(int index, int len, BitArray bitArray)
        {

            BitArray returnArray = new BitArray(len);

            for (int i = index, bitArrayIndex = 0; i < index + len; i++, bitArrayIndex++)
            {
                returnArray.Set(bitArrayIndex, bitArray.Get(i));
            }
            index += len;
            return returnArray;
        }

        public static uint BitArrayToUInt(BitArray array)
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

        public static uint CountBits(int number)
        {
            return (uint)Math.Log(number, 2.0) + 1;
        }

        public static byte[] BitArrayToByteArray(BitArray bits)
        {
            float bitLen = bits.Length;
            bitLen = bitLen / 8;

            if (bitLen - Math.Truncate(bitLen) > 0)
            {
                bitLen++;
            }

            byte[] ret = new byte[Math.Max(1, (int)Math.Truncate(bitLen))];
            bits.CopyTo(ret, 0); // every byte is reverse
            return ret;
        }

    }
}
