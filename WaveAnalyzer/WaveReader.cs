using System;
using System.IO;

namespace WaveAnalyer
{
    public class WaveReader
    {
        public byte[] byteArray;
        public int chunkID;
        public int fileSize;
        public int riffType;
        public int fmtID;
        public int fmtSize;
        public int fmtCode;
        public int channels;
        public int sampleRate;
        public int fmtAvgBPS;
        public int fmtBlockAlign;
        public int bitDepth;
        public int fmtExtraSize;
        public int dataID;
        public int dataSize;

      /*
       * Bytes to Double convertion method
       *
       *    Conversts an array of 2 bytes (16 bit audio sample) into a double
       * 
       * Parameter:
       *          firstByte     First byte in the array
       *          secondByte    Second byte in the array
       *
       * Return double 16 bit sample in double format
       */
        static double bytesToDouble(byte firstByte, byte secondByte) {
            short s = BitConverter.ToInt16(new byte[2] {firstByte, secondByte},0);
            return s;
        }

      /*
       * Double to Bytes convertion method
       *
       *    Conversts an a double to and array of 2 bytes (16 bit audio sample)
       * 
       * Parameter:
       *          d     Double to be converted in to bytes
       *
       * Return byte[] 16 bit sample
       */
        static byte[] doubleToBytes(double d)
        {
            short tmp = (short)(d);
            return BitConverter.GetBytes(tmp);
        }

        /*
         * Open wave file method
         *
         *    Returns audio samples in wave file to left and right double arrays. 'right' will be null if sound is mono.
         * 
         * Parameter:
         *          fileName     file to be opened and read from
         *          out left     output parameter for the left half of the samples
         *          out right    output paramter for the right half of the samples
         *
         * Return void
         */
        public void openWav(string filename, out double[] left, out double[] right)
        {
            int samples, pos = 0;

            FileStream fs = new FileStream(filename, FileMode.Open);
            BinaryReader reader = new BinaryReader(fs);

            chunkID = reader.ReadInt32();
            fileSize = reader.ReadInt32();
            riffType = reader.ReadInt32();
            fmtID = reader.ReadInt32();
            fmtSize = reader.ReadInt32();
            fmtCode = reader.ReadInt16();
            channels = reader.ReadInt16();
            sampleRate = reader.ReadInt32();
            fmtAvgBPS = reader.ReadInt32();
            fmtBlockAlign = reader.ReadInt16();
            bitDepth = reader.ReadInt16();

            if (fmtSize == 18)
            {
                // Read any extra values
                fmtExtraSize = reader.ReadInt16();
                reader.ReadBytes(fmtExtraSize);
            }

            dataID = reader.ReadInt32();
            dataSize = reader.ReadInt32();

            byteArray = reader.ReadBytes(dataSize);

            // Check bitDepth
            if (bitDepth == 16)
            {
                if (channels == 2) samples = dataSize / 4;
                else samples = dataSize / 2;
            }
            else
            {
                if (channels == 2) samples = dataSize / 2;
                else samples = dataSize;
            }

            // Allocate memory (right will be null if only mono sound)
            left = new double[samples];
            if (channels == 2) right = new double[samples];
            else right = null;

            if (bitDepth == 16)
            {
                // Write to double array/s:
                int i = 0;
                while (pos < dataSize)
                {
                    left[i] = bytesToDouble(byteArray[pos], byteArray[pos + 1]);
                    pos += 2;
                    if (channels == 2)
                    {
                        right[i] = bytesToDouble(byteArray[pos], byteArray[pos + 1]);
                        pos += 2;
                    }
                    i++;
                }
            }
            else if (bitDepth == 8)
            {
                // Write to double array/s:
                int i = 0;
                while (pos < dataSize)
                {
                    left[i] = Convert.ToDouble(byteArray[pos]);
                    pos++;
                    if (channels == 2)
                    {
                        right[i] = Convert.ToDouble(byteArray[pos]);
                        pos++;
                    }
                    i++;
                }
            }
        }

      /*
       * Rebuild Wave method
       *
       *    Method rebuilds the wave file with the samples passed back from the program
       * 
       * Parameter:
       *          left     left half of the audio file
       *          right    right half of the audio file
       *
       * Return void
       */
        public void rebuildWav(double[] left, double[] right)
        {
            byte[] newWav;

            if (bitDepth == 16)
            {
                if (channels == 1)
                {
                    newWav = new byte[left.Length * 2];

                    for (int i = 0; i < left.Length; i++)
                    {
                        Buffer.BlockCopy(doubleToBytes(left[i]), 0, newWav, i * 2, 2);
                    }
                }
                else
                {
                    newWav = new byte[left.Length * 4];
                    int i = 0;
                    int pos = 0;
                    while (pos < newWav.Length)
                    {
                        Buffer.BlockCopy(doubleToBytes(left[i]), 0, newWav, pos, 2);
                        pos += 2;
                        if (channels == 2)
                        {
                            Buffer.BlockCopy(doubleToBytes(right[i]), 0, newWav, pos, 2);
                            pos += 2;
                        }
                        i++;
                    }
                }
            }
            else if (bitDepth == 8)
            {
                if (channels == 1)
                {
                    newWav = new byte[left.Length];

                    for (int i = 0; i < left.Length; i++)
                    {
                        Buffer.BlockCopy(doubleToBytes(left[i]), 0, newWav, i, 1);
                    }
                }
                else
                {
                    newWav = new byte[left.Length * 4];
                    int i = 0;
                    int pos = 0;
                    while (pos < newWav.Length)
                    {
                        Buffer.BlockCopy(doubleToBytes(left[i]), 0, newWav, pos, 1);
                        pos ++;
                        if (channels == 2)
                        {
                            Buffer.BlockCopy(doubleToBytes(right[i]), 0, newWav, pos, 1);
                            pos ++;
                        }
                        i++;
                    }
                }
            } else
            {
                return;
            }

            byteArray = newWav;
            dataSize = newWav.Length;
            fileSize = newWav.Length + 44 - 8;
        }

        /*
         * write Wave method
         *
         *    Method write the wave file data to the specified file
         * 
         * Parameter:
         *          fileName file written to
         *
         * Return void
         */
        public void writeWav(string fileName)
        {
            if (byteArray != null)
            {   byte[] tmp = new byte[fileSize + 8];

                Buffer.BlockCopy(intToByteArr(chunkID), 0, tmp, 0, 4);
                Buffer.BlockCopy(intToByteArr(fileSize), 0, tmp, 4, 4);
                Buffer.BlockCopy(intToByteArr(riffType), 0, tmp, 8, 4);
                Buffer.BlockCopy(intToByteArr(fmtID), 0, tmp, 12, 4);
                Buffer.BlockCopy(intToByteArr(fmtSize), 0, tmp, 16, 4);
                Buffer.BlockCopy(intToByteArr(fmtCode), 0, tmp, 20, 2);
                Buffer.BlockCopy(intToByteArr(channels), 0, tmp, 22, 2);
                Buffer.BlockCopy(intToByteArr(sampleRate), 0, tmp, 24, 4);
                Buffer.BlockCopy(intToByteArr(fmtAvgBPS), 0, tmp, 28, 4);
                Buffer.BlockCopy(intToByteArr(fmtBlockAlign), 0, tmp, 32, 2);
                Buffer.BlockCopy(intToByteArr(bitDepth), 0, tmp, 34, 2);
                Buffer.BlockCopy(intToByteArr(dataID), 0, tmp, 36, 4);
                Buffer.BlockCopy(intToByteArr(dataSize), 0, tmp, 40, 4);

                Buffer.BlockCopy(byteArray, 0, tmp, 44, dataSize);

                File.WriteAllBytes(fileName, tmp);
            }
        }

        /*
         * play Wave method
         *
         *    Method builds the wave file as a byte array and returns it to caller, used only for C#
         *    playback method
         * 
         * Return byte[] returns the full wave file as a byte array for playback
         */
        /*public byte[] playWav()
        {
            if (byteArray != null)
            {
                byte[] tmp = new byte[fileSize + 8];

                Buffer.BlockCopy(intToByteArr(chunkID), 0, tmp, 0, 4);
                Buffer.BlockCopy(intToByteArr(fileSize), 0, tmp, 4, 4);
                Buffer.BlockCopy(intToByteArr(riffType), 0, tmp, 8, 4);
                Buffer.BlockCopy(intToByteArr(fmtID), 0, tmp, 12, 4);
                Buffer.BlockCopy(intToByteArr(fmtSize), 0, tmp, 16, 4);
                Buffer.BlockCopy(intToByteArr(fmtCode), 0, tmp, 20, 2);
                Buffer.BlockCopy(intToByteArr(channels), 0, tmp, 22, 2);
                Buffer.BlockCopy(intToByteArr(sampleRate), 0, tmp, 24, 4);
                Buffer.BlockCopy(intToByteArr(fmtAvgBPS), 0, tmp, 28, 4);
                Buffer.BlockCopy(intToByteArr(fmtBlockAlign), 0, tmp, 32, 2);
                Buffer.BlockCopy(intToByteArr(bitDepth), 0, tmp, 34, 2);
                Buffer.BlockCopy(intToByteArr(dataID), 0, tmp, 36, 4);
                Buffer.BlockCopy(intToByteArr(dataSize), 0, tmp, 40, 4);

                Buffer.BlockCopy(byteArray, 0, tmp, 44, dataSize);

                return tmp;
            }
            return null;
        }*/

        /*
         * int to byte Array method
         *
         *    converts an int to an array of bytes, used for writting header data.
         * 
         * Parameter:
         *          int     int to be converted into a byte array
         *
         * Return byte[]
         */
        public byte[] intToByteArr(int i)
        {
            byte[] tmp = BitConverter.GetBytes(i);
            return tmp;
        }

        /*
         * Record wave file method
         *
         *    method used in tandom with Record object, takes data recorded and converts it into use able data
         * 
         * Parameter:
         *          wave         recorded wave
         *          out left     output parameter for the left half of the samples
         *          out right    output paramter for the right half of the samples
         *
         * Return void
         */
        public void recWav(byte[] wave, out double[] left, out double[] right)
        {
            int samples, pos = 0;

            System.IO.MemoryStream ms = new System.IO.MemoryStream(wave);
            BinaryReader reader = new BinaryReader(ms);

            chunkID = reader.ReadInt32();
            fileSize = reader.ReadInt32();
            riffType = reader.ReadInt32();
            fmtID = reader.ReadInt32();
            fmtSize = reader.ReadInt32();
            fmtCode = reader.ReadInt16();
            channels = reader.ReadInt16();
            sampleRate = reader.ReadInt32();
            fmtAvgBPS = reader.ReadInt32();
            fmtBlockAlign = reader.ReadInt16();
            bitDepth = reader.ReadInt16();

            if (fmtSize == 18)
            {
                // Read any extra values
                fmtExtraSize = reader.ReadInt16();
                reader.ReadBytes(fmtExtraSize);
            }

            dataID = reader.ReadInt32();
            dataSize = reader.ReadInt32();

            byteArray = reader.ReadBytes(dataSize);

            if (bitDepth == 16)
            {
                if (channels == 2) samples = dataSize / 4;
                else samples = dataSize / 2;
            } else
            {
                if (channels == 2) samples = dataSize / 2;
                else samples = dataSize;
            }

            // Allocate memory (right will be null if only mono sound)
            left = new double[samples];
            if (channels == 2) right = new double[samples];
            else right = null;

            if (bitDepth == 16)
            {
                // Write to double array/s:
                int i = 0;
                while (pos < dataSize)
                {
                    left[i] = bytesToDouble(byteArray[pos], byteArray[pos + 1]);
                    pos += 2;
                    if (channels == 2)
                    {
                        right[i] = bytesToDouble(byteArray[pos], byteArray[pos + 1]);
                        pos += 2;
                    }
                    i++;
                }
            } else if(bitDepth == 8)
            {
                // Write to double array/s:
                int i = 0;
                while (pos < dataSize)
                {
                    left[i] = Convert.ToDouble(byteArray[pos]);
                    pos++;
                    if (channels == 2)
                    {
                        right[i] = Convert.ToDouble(byteArray[pos]);
                        pos++;
                    }
                    i++;
                }
            } 
        }
    }
}
