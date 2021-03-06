﻿// Copyright (c) 2013, Chad Zawistowski
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Quickbeam.Low.ByteArray
{
    /// <summary>
    /// Provides a default implementation for most of IByteArray.
    /// </summary>
    /// <remarks>
    /// Override ReadBytesCore and WriteBytesCore to complete your class.
    /// </remarks>
    public abstract class BaseByteArray : IByteArray
    {
        protected static ASCIIEncoding Encoding = new ASCIIEncoding();

        public int Offset { get; protected set; }
        public int Size { get; protected set; }

        protected BaseByteArray(int offset, int size)
        {
            Offset = offset;
            Size = size;
        }


        #region bytearrays

        /// <summary>
        /// Performs bounds-checking, then reads bytes from the mapfile
        /// </summary>
        public byte[] ReadBytes(int offset, int length)
        {
            if ((offset + length) > (Offset + Size))
                throw new ArgumentException("offset + length too large: not allowed to read outside of MapAccess range");
            return ReadBytesCore(offset, length);
        }

        protected abstract byte[] ReadBytesCore(int offset, int length);

        /// <summary>
        /// Performs bounds-checking, then writes bytes to the mapfile
        /// </summary>
        public void WriteBytes(int offset, byte[] data)
        {
            if (data == null)
                throw new ArgumentException("Cannot write bytes: given no data to write.");
            if ((offset + data.Length) > (Offset + Size))
                throw new ArgumentException("offset + data.Length too large: not allowed to write outside of MapAccess range");

            WriteBytesCore(offset, data);
        }

        protected abstract void WriteBytesCore(int offset, byte[] data);

        #endregion

        #region string types

        public string ReadAscii(int offset, int length)
        {
            var strBytes = ReadBytes(offset, length);
            var pinnedStrBytes = GCHandle.Alloc(strBytes, GCHandleType.Pinned);
            var addrOfName = pinnedStrBytes.AddrOfPinnedObject();
            var result = Marshal.PtrToStringAnsi(addrOfName);
            pinnedStrBytes.Free();
            return result;
        }

        public string ReadAsciiz(int offset)
        {
            // I'm not aware of any good way to read a null-terminated string
            // from another process' memory, so we have to guess at the length
            // and try again if we didn't find the null.

            byte[] strBytes;
            int maxToRead = Size - offset;
            int bytesToRead = Math.Min(/* A decent starting value? */ 64, maxToRead); //TODO profiling for a good starting value

            do
            {
                strBytes = ReadBytes(offset, bytesToRead);
                bytesToRead *= 2;
            } while ((bytesToRead < maxToRead) && !strBytes.Any(b => b == (byte)0x00));

            var pinnedStrBytes = GCHandle.Alloc(strBytes, GCHandleType.Pinned);
            var addrOfName = pinnedStrBytes.AddrOfPinnedObject();
            var result = Marshal.PtrToStringAnsi(addrOfName);
            pinnedStrBytes.Free();
            return result;
        }

        /// <summary>
        /// Writes a string, as ascii, to the specified location in the map.
        /// </summary>
        /// <param name="toWrite">The string to write</param>
        /// <param name="offset">The offset in bytes to write to</param>
        // TODO - Does this include null-termination? It should not.
        public void WriteAscii(int offset, string toWrite)
        {
            WriteBytes(offset, Encoding.GetBytes(toWrite));
        }

        /// <summary>
        /// Writes a string, as ascii, to the specified location in the map.
        /// </summary>
        /// <param name="toWrite">The string to write</param>
        /// <param name="offset">The offset in bytes to write to</param>
        // TODO - Does this include null-termination? It should.
        public void WriteAsciiz(int offset, string toWrite)
        {
            WriteBytes(offset, Encoding.GetBytes(toWrite));
        }

        #endregion

        #region integer types

        public sbyte ReadInt8(int offset) { return (sbyte)ReadBytes(offset, 1).First(); }
        public short ReadInt16(int offset) { return BitConverter.ToInt16(ReadBytes(offset, 2), 0); }
        public int ReadInt32(int offset) { return BitConverter.ToInt32(ReadBytes(offset, 4), 0); }
        public long ReadInt64(int offset) { return BitConverter.ToInt64(ReadBytes(offset, 8), 0); }
        public byte ReadUInt8(int offset) { return ReadBytes(offset, 1).First(); }
        public ushort ReadUInt16(int offset) { return BitConverter.ToUInt16(ReadBytes(offset, 2), 0); }
        public uint ReadUInt32(int offset) { return BitConverter.ToUInt32(ReadBytes(offset, 4), 0); }
        public ulong ReadUInt64(int offset) { return BitConverter.ToUInt64(ReadBytes(offset, 8), 0); }
        public void WriteInt8(int offset, sbyte toWrite) { WriteBytes(offset, new[] { (byte)toWrite }); }
        public void WriteInt16(int offset, short toWrite) { WriteBytes(offset, BitConverter.GetBytes(toWrite)); }
        public void WriteInt32(int offset, int toWrite) { WriteBytes(offset, BitConverter.GetBytes(toWrite)); }
        public void WriteInt64(int offset, long toWrite) { WriteBytes(offset, BitConverter.GetBytes(toWrite)); }

        public void WriteUInt8(int offset, byte toWrite) { WriteBytes(offset, new[] { toWrite }); }
        public void WriteUInt16(int offset, ushort toWrite) { WriteBytes(offset, BitConverter.GetBytes(toWrite)); }
        public void WriteUInt32(int offset, uint toWrite) { WriteBytes(offset, BitConverter.GetBytes(toWrite)); }
        public void WriteUInt64(int offset, ulong toWrite) { WriteBytes(offset, BitConverter.GetBytes(toWrite)); }

        #endregion

        #region float types

        public float ReadFloat32(int offset) { return BitConverter.ToSingle(ReadBytes(offset, 4), 0); }
        public double ReadFloat64(int offset) { return BitConverter.ToDouble(ReadBytes(offset, 8), 0); }
        public void WriteFloat32(int offset, float toWrite) { WriteBytes(offset, BitConverter.GetBytes(toWrite)); }
        public void WriteFloat64(int offset, double toWrite) { WriteBytes(offset, BitConverter.GetBytes(toWrite)); }

        #endregion
    }
}
