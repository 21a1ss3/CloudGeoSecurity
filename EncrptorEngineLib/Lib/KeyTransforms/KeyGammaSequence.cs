using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    public class KeyGammaSequence
    {
        private ushort _startPosition;
        private byte[] _rawData;

        public KeyGammaSequence(byte[] rawData, ushort start)
        {
            if ((rawData == null) || (rawData.Length == 0))
                throw new ArgumentNullException($"{nameof(rawData)} cannot be empty or equal to null");


            RawData = rawData;
            StartPosition = start;
        }

        public byte GetByteAtIndex(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            return RawData[(_rawDataOffset + index) % RawData.Length];
        }

        public void SetByteAtIndex(int index, byte value)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            RawData[(_rawDataOffset + index) % RawData.Length] = value;
        }

        public byte[] ExtractBytes(int offset, int count)
        {
            if ((offset >= RawData.Length) || (offset < 0))
                throw new ArgumentOutOfRangeException(nameof(offset));

            if ((offset + count) > RawData.Length || (count < 0))
                throw new ArgumentOutOfRangeException(nameof(count));

            byte[] result = new byte[count];

            int startIndex = (StartPosition % RawData.Length) + offset;
            int overallLength = startIndex + count;

            int firstTierStart = Math.Min(startIndex, RawData.Length);
            int firstTierEnd = Math.Min(overallLength, RawData.Length);
            int firstTierLen = firstTierEnd - firstTierStart;
            startIndex -= RawData.Length;
            overallLength -= RawData.Length;
            int secondTierStart = Math.Max(startIndex, 0);
            int secondTierEnd = Math.Max(overallLength, 0);

            Array.Copy(RawData, firstTierStart, result, 0, firstTierLen);
            Array.Copy(RawData, secondTierStart, result, firstTierLen, secondTierEnd - secondTierStart);

            return result;
        }


        public static KeyGammaSequence CreateRandom(ushort length)
        {
            if (length < 1)
                throw new ArgumentOutOfRangeException(nameof(length));

            byte[] gammaSequence = new byte[length];
            byte[] startIndexBytes = new byte[2];
            var cryptRandGen = System.Security.Cryptography.RandomNumberGenerator.Create();

            cryptRandGen.GetBytes(gammaSequence);
            cryptRandGen.GetBytes(startIndexBytes);

            ushort startIndex = BitConverter.ToUInt16(startIndexBytes, 0);

            return new KeyGammaSequence(gammaSequence, startIndex);
        }


        public byte[] RawData
        {
            get => _rawData;
            private set
            {
                _rawData = value;
                _rawDataOffset = _startPosition % _rawData.Length;
            }
        }

        public ushort StartPosition
        {
            get => _startPosition;
            private set
            {
                _startPosition = value;
                _rawDataOffset = _startPosition % _rawData.Length;
            }
        }

        private int _rawDataOffset { get; set; }
    }
}
