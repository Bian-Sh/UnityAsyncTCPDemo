using System;

public static class BytesHelper
{
    public static void CopyBytes(short value, byte[] buffer, int index)
    {
        CopyBytesImpl(value, 2, buffer, index);
    }

    ///     Copies the specified 32-bit signed integer value into the specified byte array,
    ///     beginning at the specified index.
    public static void CopyBytes(int value, byte[] buffer, int index)
    {
        CopyBytesImpl(value, 4, buffer, index);
    }

    public static void CopyBytesImpl(long value, int bytes, byte[] buffer, int index)
    {
        for (var i = 0; i < bytes; i++)
        {
            buffer[i + index] = unchecked((byte)(value & 0xff));
            value = value >> 8;
        }
    }

    ///     Returns a 16-bit signed integer converted from two bytes at a specified position in a byte array.
    public static short ToInt16(byte[] value, int startIndex)
    {
        return unchecked((short)CheckedFromBytes(value, startIndex, 2));
    }

    ///     Returns a 32-bit signed integer converted from four bytes at a specified position in a byte array.
    public static int ToInt32(byte[] value, int startIndex)
    {
        return unchecked((int)CheckedFromBytes(value, startIndex, 4));
    }

    private static long CheckedFromBytes(byte[] buffer, int startIndex, int bytesToConvert)
    {
        if (buffer == null)
            throw new ArgumentNullException("buffer");
        if ((startIndex < 0) || (startIndex > buffer.Length - bytesToConvert))
            throw new ArgumentOutOfRangeException("startIndex");
        long ret = 0;
        for (var i = 0; i < bytesToConvert; i++)
            ret = unchecked((ret << 8) | buffer[startIndex + bytesToConvert - 1 - i]);
        return ret;
    }

    public static byte[] GetBytes(ushort val)
    {
        if (BitConverter.IsLittleEndian)
        {
            return BitConverter.GetBytes(val);
        }
        else
        {
            return BitConverter.GetBytes(val).Swap();
        }
    }

    public static byte[] GetBytes(int val)
    {
        if (BitConverter.IsLittleEndian)
        {
            return BitConverter.GetBytes(val);
        }
        else
        {
            return BitConverter.GetBytes(val).Swap();
        }
    }

    public static ushort ToUInt16(byte[] value, int startIndex)
    {
        if (BitConverter.IsLittleEndian)
        {
            return BitConverter.ToUInt16(value, startIndex);
        }
        else
        {
            return BitConverter.ToUInt16(value.Swap(startIndex, sizeof(ushort)), startIndex);
        }
    }

    private static byte[] Swap(this byte[] vals, int startIdx, int len)
    {
        var dst = new byte[len];
        Buffer.BlockCopy(vals, startIdx, dst, 0, len);
        return Swap(dst);
    }

    private static byte[] Swap(this byte[] vals)
    {
        var count = vals.Length;
        for (int i = 0, j = count - 1; i < j; ++i, --j)
        {
            var temp = vals[i];
            vals[i] = vals[j];
            vals[j] = temp;
        }

        return vals;
    }
}
