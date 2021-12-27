using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageProcessing
{
   static public class Bytes
    {
        static public void Add(ref byte[] source, byte[] dataAdd)
        {
            if (source == null)
                source = new byte[0];
            Array.Resize(ref source, source.Length + dataAdd.Length);
            Array.Copy(dataAdd, 0, source, source.Length - dataAdd.Length, dataAdd.Length);
        }

        static public byte[] Combine(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }

        static public byte[] Copy(byte[] bytes)
        {
            byte[] bytesCopy = new byte[bytes.Length];
            bytes.CopyTo(bytesCopy, 0);
            return bytesCopy;
        }
    }
}
