//
//
// Licensed under the MIT license.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace GeneticAlgorithm_Waves
{
    public class WaveFile
    {

        //-----WaveHeader-----
        struct WaveHeader
        {
            public char[] sGroupID; // RIFF
            public uint dwFileLength; // total file length minus 8, which is taken up by RIFF
            public char[] sRiffType;// always WAVE
        }

        //-----WaveFormatChunk-----
        struct WaveFormatChunk
        {
            public char[] sFChunkID;         // Four bytes: "fmt "
            public uint dwFChunkSize;        // Length of header in bytes
            public ushort wFormatTag;       // 1 (MS PCM)
            public ushort wChannels;        // Number of channels
            public uint dwSamplesPerSec;    // Frequency of the audio in Hz... 44100
            public uint dwAvgBytesPerSec;   // for estimating RAM allocation
            public ushort wBlockAlign;      // sample frame size, in bytes
            public ushort wBitsPerSample;    // bits per sample
        }

        //-----WaveDataChunk-----
        struct WaveDataChunk
        {
            public char[] sDChunkID;     // "data"
            public uint dwDChunkSize;    // Length of header in bytes
        }

        byte dataStartPos = 44;  // audio data start position

        WaveHeader waveHeader;
        WaveFormatChunk waveFormatChunk;
        WaveDataChunk waveDataChunk;

        public byte[] ReadPartOfFile(string path, int length)
        {

            // Open a file
            FileStream fileStream = new FileStream(path, FileMode.Open);

            // Use BinaryReader to read the bytes from the file
            BinaryReader reader = new BinaryReader(fileStream);

            byte[] data;
            int audioDataLength = (int)fileStream.Length - 44;
            data = new byte[length];

            // Read the header
            waveHeader.sGroupID = reader.ReadChars(4);
            waveHeader.dwFileLength = reader.ReadUInt32();
            waveHeader.sRiffType = reader.ReadChars(4);

            // Read the format chunk
            waveFormatChunk.sFChunkID = reader.ReadChars(4);
            waveFormatChunk.dwFChunkSize = reader.ReadUInt32();
            waveFormatChunk.wFormatTag = reader.ReadUInt16();
            waveFormatChunk.wChannels = reader.ReadUInt16();
            waveFormatChunk.dwSamplesPerSec = reader.ReadUInt32();
            waveFormatChunk.dwAvgBytesPerSec = reader.ReadUInt32();
            waveFormatChunk.wBlockAlign = reader.ReadUInt16();
            waveFormatChunk.wBitsPerSample = reader.ReadUInt16();

            reader.ReadUInt16();

            // Read the data chunk
            waveDataChunk.sDChunkID = reader.ReadChars(4);
            waveDataChunk.dwDChunkSize = reader.ReadUInt32();

            // Read part of audio data
            data = reader.ReadBytes(length);
            
            // Clean up
            reader.Close();
            fileStream.Close();

            return data;
        }

        public void Write (string path, byte[] databuffer)
        {

            //-----WaveHeader-----
            waveHeader.dwFileLength = 0;
            waveHeader.sGroupID = "RIFF".ToCharArray();
            waveHeader.sRiffType = "WAVE".ToCharArray();

            //-----WaveFormatChunk-----
            waveFormatChunk.sFChunkID = "fmt ".ToCharArray();
            waveFormatChunk.dwFChunkSize = 16;
            waveFormatChunk.wFormatTag = 1;
            waveFormatChunk.wChannels = 1;
            waveFormatChunk.dwSamplesPerSec = 44100;
            waveFormatChunk.wBitsPerSample = 8;
            waveFormatChunk.wBlockAlign = (ushort)(waveFormatChunk.wChannels * (waveFormatChunk.wBitsPerSample / 8));
            waveFormatChunk.dwAvgBytesPerSec = waveFormatChunk.dwSamplesPerSec * waveFormatChunk.wBlockAlign;

            //-----WaveDataChunk-----
            waveDataChunk.dwDChunkSize = (uint)(databuffer.Length * (waveFormatChunk.wBitsPerSample / 8));
            waveDataChunk.sDChunkID = "data".ToCharArray();

            // Create a file (it always overwrites)
            FileStream fileStream = new FileStream(path, FileMode.Create);

            // Use BinaryWriter to write the bytes to the file
            BinaryWriter writer = new BinaryWriter(fileStream);

            // Write the header
            writer.Write(waveHeader.sGroupID);
            writer.Write(waveHeader.dwFileLength);
            writer.Write(waveHeader.sRiffType);

            // Write the format chunk
            writer.Write(waveFormatChunk.sFChunkID);
            writer.Write(waveFormatChunk.dwFChunkSize);
            writer.Write(waveFormatChunk.wFormatTag);
            writer.Write(waveFormatChunk.wChannels);
            writer.Write(waveFormatChunk.dwSamplesPerSec);
            writer.Write(waveFormatChunk.dwAvgBytesPerSec);
            writer.Write(waveFormatChunk.wBlockAlign);
            writer.Write(waveFormatChunk.wBitsPerSample);

            // Write the data chunk
            writer.Write(waveDataChunk.sDChunkID);
            writer.Write(waveDataChunk.dwDChunkSize);

            foreach (byte dataPoint in databuffer)
            {
                writer.Write(dataPoint);
            }

            writer.Seek(4, SeekOrigin.Begin);
            uint filesize = (uint)writer.BaseStream.Length;
            writer.Write(filesize - 8);

            // Clean up
            writer.Close();
            fileStream.Close();

        }
    }
}
