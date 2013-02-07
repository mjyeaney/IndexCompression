using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Performse Base-128 Varint encoding on a singe or list or uint's.
/// </summary>
public class Base128Encoder
{
    /// <summary>
    /// Encodes a list of uint data.
    /// </summary>
    /// <param name="stream">Output stream used to capture encoded bytes.</param>
    /// <param name="data">The data to encode.</param>
    /// <returns>Length of the encoded data stream.</returns>
    public byte[] EncodeList(List<uint> data)
    {
        if (data.Count == 0) return new byte[0];

        using (var encodingStream = new MemoryStream())
        {
            data = createDGapList(data);
            for (int i = 0; i < data.Count; i++)
            {
                Encode(encodingStream, data[i]);
            }
            return encodingStream.ToArray();
        }
    }

    /// <summary>
    /// Decodes a list of uint data from the provided stream.
    /// </summary>
    /// <param name="stream">Stream containing encoded data bytes.</param>
    /// <returns>Decoded data set.</returns>
    public List<uint> DecodeList(byte[] compressedData)
    {
        using (var decodeStream = new MemoryStream(compressedData))
        {
            var list = new List<uint>();
            uint value, last = 0;
            while (TryDecode(decodeStream, out value))
            {
                // we encoded a d-gap list, so keep adding to get orig data
                last += value;
                list.Add(last);
            }
            return list;
        }
    }

    /// <summary>
    /// Encodes a single uint data point.
    /// </summary>
    /// <param name="stream">Stream used to capture encoded bytes.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>Number of bytes used for encoding.</returns>
    public int Encode(Stream stream, uint value)
    {
        int count = 0, index = 0;
        byte[] buffer = new byte[8];

        do
        {
            buffer[index++] = (byte)((value & 0x7F) | 0x80);    // read first 7-bits, and check for MSB set
            value >>= 7;                                        // shift right 7 bits
            count++;                                            // increment bytes written counter
        } while (value != 0);

        buffer[index - 1] &= 0x7F;  // mark last byte MSB low since we're done

        // write data and return length
        stream.Write(buffer, 0, count);
        return count;
    }

    /// <summary>
    /// Tries to decode a uint data point from the encoded stream.
    /// </summary>
    /// <param name="source">Encoded stream data.</param>
    /// <param name="value">Captures the decoded data point.</param>
    /// <returns>True if the decoding was successful; otherwise false.</returns>
    public bool TryDecode(Stream source, out uint value)
    {
        // read a single byte
        int b = source.ReadByte();

        // make sure data was read
        if (b < 0)
        {
            value = 0;
            return false;
        }

        // check for single-byte encoded value
        if ((b & 0x80) == 0)
        {
            value = (uint)b;
            return true;
        }

        // we have a multi-byte vaule...read first byte
        int shift = 7;
        value = (uint)(b & 0x7F);
        bool keepGoing;
        int i = 0;

        do
        {
            // read byte and make sure data was found
            b = source.ReadByte();
            if (b < 0) throw new EndOfStreamException();

            i++;                                    // incr bytes-read counter
            keepGoing = (b & 0x80) != 0;            // continue reading until MSB is set (0x80)
            value |= ((uint)(b & 0x7F)) << shift;   // read the next encoded byte, shift left and or to get remaining data
            shift += 7;                             // bump shift amount for next read
        } while (keepGoing && i < 4);               // remember we only read up to 4 bytes (uint)

        // bounds check - uint is only 4 bytes EVAR
        if (keepGoing && i == 4)
        {
            throw new OverflowException();
        }

        // indicate success for this multi-byte value
        return true;
    }

    /// <summary>
    /// Creates a d-gap list from source data.
    /// </summary>
    /// <param name="data">Source data list.</param>
    /// <returns>D-gap list based on original data.</returns>
    private List<uint> createDGapList(List<uint> data)
    {
        // sort input data
        var dg = new List<uint>();
        data.Sort();

        // create d-gap list
        var counter = 0;
        for (var i=0; i < data.Count; i++)
        {
            var v = data[i];
            if (counter > 0)
            {
                dg.Add(v - data[counter - 1]);
            }
            else
            {
                dg.Add(v);
            }
            counter++;
        }
        return dg;
    }
}
