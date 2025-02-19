//  Copyright (c) 2021 Demerzel Solutions Limited
//  This file is part of the Nethermind library.
//
//  The Nethermind library is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  The Nethermind library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.

using System.Numerics;
using System.Text;
using Nethermind.Int256;
using Nethermind.Utils;
using Nethermind.Utils.Crypto;
using Nethermind.Utils.Extensions;

namespace Nethermind.Serialization.Rlp
{
    public class RlpStream
    {
        private static readonly LogEntryDecoder _logEntryDecoder = LogEntryDecoder.Instance;

        protected RlpStream()
        {
        }

        public long MemorySize => MemorySizes.SmallObjectOverhead
                                  + MemorySizes.Align(MemorySizes.ArrayOverhead + Length)
                                  + MemorySizes.Align(sizeof(int));

        public RlpStream(int length)
        {
            Data = new byte[length];
        }

        public RlpStream(byte[] data)
        {
            Data = data;
        }


        // public void Encode(LogEntry value)
        // {
        //     _logEntryDecoder.Encode(this, value);
        // }

        public void StartByteArray(int contentLength, bool firstByteLessThan128)
        {
            switch (contentLength)
            {
                case 0:
                    WriteByte(EmptyArrayByte);
                    break;
                case 1 when firstByteLessThan128:
                    // the single byte of content will be written without any prefix
                    break;
                case < 56:
                    {
                        byte smallPrefix = (byte)(contentLength + 128);
                        WriteByte(smallPrefix);
                        break;
                    }
                default:
                    {
                        int lengthOfLength = Rlp.LengthOfLength(contentLength);
                        byte prefix = (byte)(183 + lengthOfLength);
                        WriteByte(prefix);
                        WriteEncodedLength(contentLength);
                        break;
                    }
            }
        }

        public void StartSequence(int contentLength)
        {
            byte prefix;
            if (contentLength < 56)
            {
                prefix = (byte)(192 + contentLength);
                WriteByte(prefix);
            }
            else
            {
                prefix = (byte)(247 + Rlp.LengthOfLength(contentLength));
                WriteByte(prefix);
                WriteEncodedLength(contentLength);
            }
        }

        private void WriteEncodedLength(int value)
        {
            switch (value)
            {
                case < 1 << 8:
                    WriteByte((byte)value);
                    return;
                case < 1 << 16:
                    WriteByte((byte)(value >> 8));
                    WriteByte((byte)value);
                    return;
                case < 1 << 24:
                    WriteByte((byte)(value >> 16));
                    WriteByte((byte)(value >> 8));
                    WriteByte((byte)value);
                    return;
                default:
                    WriteByte((byte)(value >> 24));
                    WriteByte((byte)(value >> 16));
                    WriteByte((byte)(value >> 8));
                    WriteByte((byte)value);
                    return;
            }
        }

        public virtual void WriteByte(byte byteToWrite)
        {
            Data[Position++] = byteToWrite;
        }

        public virtual void Write(Span<byte> bytesToWrite)
        {
            bytesToWrite.CopyTo(Data.AsSpan(Position, bytesToWrite.Length));
            Position += bytesToWrite.Length;
        }

        protected virtual string Description =>
            Data?.Slice(0, Math.Min(Rlp.DebugMessageContentLength, Length)).ToHexString() ?? "0x";

        public byte[]? Data { get; }

        public virtual int Position { get; set; }

        public virtual int Length => Data!.Length;

        public virtual bool HasBeenRead => Position >= Data!.Length;

        public bool IsSequenceNext()
        {
            return PeekByte() >= 192;
        }

        public void Encode(Commitment? Commitment)
        {
            if (Commitment is null)
            {
                WriteByte(EmptyArrayByte);
            }
            else if (ReferenceEquals(Commitment, Commitment.EmptyTreeHash))
            {
                Write(Rlp.OfEmptyTreeHash.Bytes);
            }
            else if (ReferenceEquals(Commitment, Commitment.OfAnEmptyString))
            {
                Write(Rlp.OfEmptyStringHash.Bytes);
            }
            else
            {
                WriteByte(160);
                Write(Commitment.Bytes);
            }
        }

        public void Encode(Commitment[] Commitments)
        {
            if (Commitments is null)
            {
                EncodeNullObject();
            }
            else
            {
                var length = Rlp.LengthOf(Commitments);
                StartSequence(length);
                for (int i = 0; i < Commitments.Length; i++)
                {
                    Encode(Commitments[i]);
                }
            }
        }

        public void Encode(Address? address)
        {
            if (address is null)
            {
                WriteByte(EmptyArrayByte);
            }
            else
            {
                WriteByte(148);
                Write(address.Bytes);
            }
        }

        public void Encode(Rlp? rlp)
        {
            if (rlp is null)
            {
                WriteByte(EmptyArrayByte);
            }
            else
            {
                Write(rlp.Bytes);
            }
        }

        public void Encode(Bloom? bloom)
        {
            if (ReferenceEquals(bloom, Bloom.Empty))
            {
                WriteByte(185);
                WriteByte(1);
                WriteByte(0);
                WriteZero(256);
            }
            else if (bloom is null)
            {
                WriteByte(EmptyArrayByte);
            }
            else
            {
                WriteByte(185);
                WriteByte(1);
                WriteByte(0);
                Write(bloom.Bytes);
            }
        }

        protected virtual void WriteZero(int length)
        {
            Position += 256;
        }

        public void Encode(byte value)
        {
            if (value == 0)
            {
                WriteByte(128);
            }
            else if (value < 128)
            {
                WriteByte(value);
            }
            else
            {
                WriteByte(129);
                WriteByte(value);
            }
        }

        public void Encode(bool value)
        {
            Encode(value ? (byte)1 : (byte)0);
        }

        public void Encode(int value)
        {
            Encode((long)value);
        }

        public void Encode(BigInteger bigInteger, int outputLength = -1)
        {
            Rlp rlp = bigInteger == 0
                ? Rlp.OfEmptyByteArray
                : Rlp.Encode(bigInteger.ToBigEndianByteArray(outputLength));
            Write(rlp.Bytes);
        }

        public void Encode(long value)
        {
            if (value == 0L)
            {
                EncodeEmptyByteArray();
                return;
            }

            if (value > 0)
            {
                byte byte6 = (byte)(value >> 8);
                byte byte5 = (byte)(value >> 16);
                byte byte4 = (byte)(value >> 24);
                byte byte3 = (byte)(value >> 32);
                byte byte2 = (byte)(value >> 40);
                byte byte1 = (byte)(value >> 48);
                byte byte0 = (byte)(value >> 56);

                if (value < 256L * 256L * 256L * 256L * 256L * 256L * 256L)
                {
                    if (value < 256L * 256L * 256L * 256L * 256L * 256L)
                    {
                        if (value < 256L * 256L * 256L * 256L * 256L)
                        {
                            if (value < 256L * 256L * 256L * 256L)
                            {
                                if (value < 256 * 256 * 256)
                                {
                                    if (value < 256 * 256)
                                    {
                                        if (value < 128)
                                        {
                                            WriteByte((byte)value);
                                            return;
                                        }

                                        if (value < 256)
                                        {
                                            WriteByte(129);
                                            WriteByte((byte)value);
                                            return;
                                        }

                                        WriteByte(130);
                                        WriteByte(byte6);
                                        WriteByte((byte)value);
                                        return;
                                    }

                                    WriteByte(131);
                                    WriteByte(byte5);
                                    WriteByte(byte6);
                                    WriteByte((byte)value);
                                    return;
                                }

                                WriteByte(132);
                                WriteByte(byte4);
                                WriteByte(byte5);
                                WriteByte(byte6);
                                WriteByte((byte)value);
                                return;
                            }

                            WriteByte(133);
                            WriteByte(byte3);
                            WriteByte(byte4);
                            WriteByte(byte5);
                            WriteByte(byte6);
                            WriteByte((byte)value);
                            return;
                        }

                        WriteByte(134);
                        WriteByte(byte2);
                        WriteByte(byte3);
                        WriteByte(byte4);
                        WriteByte(byte5);
                        WriteByte(byte6);
                        WriteByte((byte)value);
                        return;
                    }

                    WriteByte(135);
                    WriteByte(byte1);
                    WriteByte(byte2);
                    WriteByte(byte3);
                    WriteByte(byte4);
                    WriteByte(byte5);
                    WriteByte(byte6);
                    WriteByte((byte)value);
                    return;
                }

                WriteByte(136);
                WriteByte(byte0);
                WriteByte(byte1);
                WriteByte(byte2);
                WriteByte(byte3);
                WriteByte(byte4);
                WriteByte(byte5);
                WriteByte(byte6);
                WriteByte((byte)value);
                return;
            }

            Encode(new BigInteger(value), 8);
        }

        public void EncodeNonce(ulong value)
        {
            Encode((UInt256)value, 8);
        }

        public void Encode(ulong value)
        {
            Encode((UInt256)value);
        }

        public void Encode(in UInt256 value, int length = -1)
        {
            if (value.IsZero && length == -1)
            {
                WriteByte(EmptyArrayByte);
            }
            else
            {
                Span<byte> bytes = stackalloc byte[32];
                value.ToBigEndian(bytes);
                if (length != -1)
                {
                    Encode(bytes.Slice(bytes.Length - length, length));
                }
                else
                {
                    Encode(bytes.WithoutLeadingZeros());
                }
            }
        }

        public void Encode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                WriteByte(128);
            }
            else
            {
                // todo: can avoid allocation here but benefit is rare
                Encode(Encoding.ASCII.GetBytes(value));
            }
        }

        public void Encode(Span<byte> input)
        {
            if (input.IsEmpty || input.Length == 0)
            {
                WriteByte(EmptyArrayByte);
            }
            else if (input.Length == 1 && input[0] < 128)
            {
                WriteByte(input[0]);
            }
            else if (input.Length < 56)
            {
                byte smallPrefix = (byte)(input.Length + 128);
                WriteByte(smallPrefix);
                Write(input);
            }
            else
            {
                int lengthOfLength = Rlp.LengthOfLength(input.Length);
                byte prefix = (byte)(183 + lengthOfLength);
                WriteByte(prefix);
                WriteEncodedLength(input.Length);
                Write(input);
            }
        }

        public int ReadNumberOfItemsRemaining(int? beforePosition = null, int maxSearch = int.MaxValue)
        {
            int positionStored = Position;
            int numberOfItems = 0;
            while (Position < (beforePosition ?? Length))
            {
                int prefix = ReadByte();
                if (prefix <= 128)
                {
                }
                else if (prefix <= 183)
                {
                    int length = prefix - 128;
                    SkipBytes(length);
                }
                else if (prefix < 192)
                {
                    int lengthOfLength = prefix - 183;
                    int length = DeserializeLength(lengthOfLength);
                    if (length < 56)
                    {
                        throw new RlpException("Expected length greater or equal 56 and was {length}");
                    }

                    SkipBytes(length);
                }
                else
                {
                    Position--;
                    int sequenceLength = ReadSequenceLength();
                    SkipBytes(sequenceLength);
                }

                numberOfItems++;
                if (numberOfItems >= maxSearch)
                {
                    break;
                }
            }

            Position = positionStored;
            return numberOfItems;
        }

        public void SkipLength()
        {
            SkipBytes(PeekPrefixAndContentLength().PrefixLength);
        }

        public int PeekNextRlpLength()
        {
            (int a, int b) = PeekPrefixAndContentLength();
            return a + b;
        }

        public (int PrefixLength, int ContentLength) ReadPrefixAndContentLength()
        {
            (int prefixLength, int contentLength) result;
            int prefix = ReadByte();
            if (prefix <= 128)
            {
                result = (0, 1);
            }
            else if (prefix <= 183)
            {
                result = (1, prefix - 128);
            }
            else if (prefix < 192)
            {
                int lengthOfLength = prefix - 183;
                if (lengthOfLength > 4)
                {
                    // strange but needed to pass tests - seems that spec gives int64 length and tests int32 length
                    throw new RlpException("Expected length of length less or equal 4");
                }

                int length = DeserializeLength(lengthOfLength);
                if (length < 56)
                {
                    throw new RlpException("Expected length greater or equal 56 and was {length}");
                }

                result = (lengthOfLength + 1, length);
            }
            else if (prefix <= 247)
            {
                result = (1, prefix - 192);
            }
            else
            {
                int lengthOfContentLength = prefix - 247;
                int contentLength = DeserializeLength(lengthOfContentLength);
                if (contentLength < 56)
                {
                    throw new RlpException($"Expected length greater or equal 56 and got {contentLength}");
                }


                result = (lengthOfContentLength + 1, contentLength);
            }

            return result;
        }

        public (int PrefixLength, int ContentLength) PeekPrefixAndContentLength()
        {
            int memorizedPosition = Position;
            (int PrefixLength, int ContentLength) result = ReadPrefixAndContentLength();

            Position = memorizedPosition;
            return result;
        }

        public int ReadSequenceLength()
        {
            int prefix = ReadByte();
            if (prefix < 192)
            {
                throw new RlpException(
                    $"Expected a sequence prefix to be in the range of <192, 255> and got {prefix} at position {Position} in the message of length {Length} starting with {Description}");
            }

            if (prefix <= 247)
            {
                return prefix - 192;
            }

            int lengthOfContentLength = prefix - 247;
            int contentLength = DeserializeLength(lengthOfContentLength);
            if (contentLength < 56)
            {
                throw new RlpException($"Expected length greater or equal 56 and got {contentLength}");
            }

            return contentLength;
        }

        private int DeserializeLength(int lengthOfLength)
        {
            int result;
            if (PeekByte() == 0)
            {
                throw new RlpException("Length starts with 0");
            }

            if (lengthOfLength == 1)
            {
                result = PeekByte();
            }
            else if (lengthOfLength == 2)
            {
                result = PeekByte(1) | (PeekByte() << 8);
            }
            else if (lengthOfLength == 3)
            {
                result = PeekByte(2) | (PeekByte(1) << 8) | (PeekByte() << 16);
            }
            else if (lengthOfLength == 4)
            {
                result = PeekByte(3) | (PeekByte(2) << 8) | (PeekByte(1) << 16) |
                         (PeekByte() << 24);
            }
            else
            {
                // strange but needed to pass tests - seems that spec gives int64 length and tests int32 length
                throw new InvalidOperationException($"Invalid length of length = {lengthOfLength}");
            }

            SkipBytes(lengthOfLength);
            return result;
        }

        public virtual byte ReadByte()
        {
            return Data![Position++];
        }

        public virtual byte PeekByte()
        {
            return Data![Position];
        }

        protected virtual byte PeekByte(int offset)
        {
            return Data![Position + offset];
        }

        protected virtual void SkipBytes(int length)
        {
            Position += length;
        }

        public virtual Span<byte> Read(int length)
        {
            Span<byte> data = Data.AsSpan(Position, length);
            Position += length;
            return data;
        }

        public void Check(int nextCheck)
        {
            if (Position != nextCheck)
            {
                throw new RlpException($"Data checkpoint failed. Expected {nextCheck} and is {Position}");
            }
        }

        public Commitment? DecodeCommitment()
        {
            int prefix = ReadByte();
            if (prefix == 128)
            {
                return null;
            }

            if (prefix != 128 + 32)
            {
                throw new RlpException(
                    $"Unexpected prefix of {prefix} when decoding {nameof(Commitment)} at position {Position} in the message of length {Length} starting with {Description}");
            }

            Span<byte> CommitmentSpan = Read(32);
            if (CommitmentSpan.SequenceEqual(Commitment.OfAnEmptyString.Bytes))
            {
                return Commitment.OfAnEmptyString;
            }

            if (CommitmentSpan.SequenceEqual(Commitment.EmptyTreeHash.Bytes))
            {
                return Commitment.EmptyTreeHash;
            }

            return new Commitment(CommitmentSpan.ToArray());
        }

        public Address? DecodeAddress()
        {
            int prefix = ReadByte();
            if (prefix == 128)
            {
                return null;
            }

            if (prefix != 128 + 20)
            {
                throw new RlpException(
                    $"Unexpected prefix of {prefix} when decoding {nameof(Commitment)} at position {Position} in the message of length {Length} starting with {Description}");
            }

            byte[] buffer = Read(20).ToArray();
            return new Address(buffer);
        }

        public UInt256 DecodeUInt256()
        {
            byte byteValue = PeekByte();
            if (byteValue < 128)
            {
                SkipBytes(1);
                return byteValue;
            }

            ReadOnlySpan<byte> byteSpan = DecodeByteArraySpan();
            if (byteSpan.Length > 32)
            {
                throw new ArgumentException();
            }

            return new UInt256(byteSpan, true);
        }

        public UInt256? DecodeNullableUInt256()
        {
            if (PeekByte() == 0)
            {
                Position++;
                return null;
            }

            return DecodeUInt256();
        }

        public BigInteger DecodeUBigInt()
        {
            ReadOnlySpan<byte> bytes = DecodeByteArraySpan();
            return bytes.ToUnsignedBigInteger();
        }

        public Bloom? DecodeBloom()
        {
            ReadOnlySpan<byte> bloomBytes;

            // tks: not sure why but some nodes send us Blooms in a sequence form
            // https://github.com/NethermindEth/nethermind/issues/113
            if (PeekByte() == 249)
            {
                SkipBytes(5); // tks: skip 249 1 2 129 127 and read 256 bytes
                bloomBytes = Read(256);
            }
            else
            {
                bloomBytes = DecodeByteArraySpan();
                if (bloomBytes.Length == 0)
                {
                    return null;
                }
            }

            if (bloomBytes.Length != 256)
            {
                throw new InvalidOperationException("Incorrect bloom RLP");
            }

            return bloomBytes.SequenceEqual(Bloom.Empty.Bytes) ? Bloom.Empty : new Bloom(bloomBytes.ToArray());
        }

        public Span<byte> PeekNextItem()
        {
            int length = PeekNextRlpLength();
            return Peek(length);
        }

        public Span<byte> Peek(int length)
        {
            Span<byte> item = Read(length);
            Position -= item.Length;
            return item;
        }

        public bool IsNextItemEmptyArray()
        {
            return PeekByte() == Rlp.EmptyArrayByte;
        }

        public bool IsNextItemNull()
        {
            return PeekByte() == Rlp.NullObjectByte;
        }

        public bool DecodeBool()
        {
            int prefix = ReadByte();
            if (prefix <= 128)
            {
                return prefix == 1;
            }

            if (prefix <= 183)
            {
                int length = prefix - 128;
                if (length == 1 && PeekByte() < 128)
                {
                    throw new RlpException($"Unexpected byte value {PeekByte()}");
                }

                bool result = PeekByte() == 1;
                SkipBytes(length);
                return result;
            }

            if (prefix < 192)
            {
                int lengthOfLength = prefix - 183;
                if (lengthOfLength > 4)
                {
                    // strange but needed to pass tests - seems that spec gives int64 length and tests int32 length
                    throw new RlpException("Expected length of length less or equal 4");
                }

                int length = DeserializeLength(lengthOfLength);
                if (length < 56)
                {
                    throw new RlpException("Expected length greater or equal 56 and was {length}");
                }

                bool result = PeekByte() == 1;
                SkipBytes(length);
                return result;
            }

            throw new RlpException(
                $"Unexpected prefix of {prefix} when decoding a byte array at position {Position} in the message of length {Length} starting with {Description}");
        }

        public T[] DecodeArray<T>(Func<RlpStream, T> decodeItem, bool checkPositions = true,
            T defaultElement = default(T))
        {
            int positionCheck = ReadSequenceLength() + Position;
            int count = ReadNumberOfItemsRemaining(checkPositions ? positionCheck : (int?)null);
            T[] result = new T[count];
            for (int i = 0; i < result.Length; i++)
            {
                if (PeekByte() == Rlp.OfEmptySequence[0])
                {
                    result[i] = defaultElement;
                    Position++;
                }
                else
                {
                    result[i] = decodeItem(this);
                }
            }

            return result;
        }

        public string DecodeString()
        {
            ReadOnlySpan<byte> bytes = DecodeByteArraySpan();
            return Encoding.UTF8.GetString(bytes);
        }

        public byte DecodeByte()
        {
            byte byteValue = PeekByte();
            if (byteValue < 128)
            {
                SkipBytes(1);
                return byteValue;
            }

            ReadOnlySpan<byte> bytes = DecodeByteArraySpan();
            return bytes.Length == 0 ? (byte)0
                : bytes.Length == 1 ? bytes[0] == (byte)128
                    ? (byte)0
                    : bytes[0]
                : bytes[1];
        }

        public int DecodeInt()
        {
            int prefix = ReadByte();
            if (prefix < 128)
            {
                return prefix;
            }

            if (prefix == 128)
            {
                return 0;
            }

            int length = prefix - 128;
            if (length > 4)
            {
                throw new RlpException($"Unexpected length of int value: {length}");
            }

            int result = 0;
            for (int i = 4; i > 0; i--)
            {
                result = result << 8;
                if (i <= length)
                {
                    result = result | PeekByte(length - i);
                }
            }

            SkipBytes(length);

            return result;
        }

        public uint DecodeUInt()
        {
            ReadOnlySpan<byte> bytes = DecodeByteArraySpan();
            return bytes.Length == 0 ? 0 : bytes.ReadEthUInt32();
        }

        public long DecodeLong()
        {
            int prefix = ReadByte();
            if (prefix < 128)
            {
                return prefix;
            }

            if (prefix == 128)
            {
                return 0;
            }

            int length = prefix - 128;
            if (length > 8)
            {
                throw new RlpException($"Unexpected length of long value: {length}");
            }

            long result = 0;
            for (int i = 8; i > 0; i--)
            {
                result = result << 8;
                if (i <= length)
                {
                    result = result | PeekByte(length - i);
                }
            }

            SkipBytes(length);

            return result;
        }

        public ulong DecodeULong()
        {
            int prefix = ReadByte();
            if (prefix < 128)
            {
                return (ulong)prefix;
            }

            if (prefix == 128)
            {
                return 0;
            }

            int length = prefix - 128;
            if (length > 8)
            {
                throw new RlpException($"Unexpected length of long value: {length}");
            }

            ulong result = 0;
            for (int i = 8; i > 0; i--)
            {
                result = result << 8;
                if (i <= length)
                {
                    result = result | PeekByte(length - i);
                }
            }

            SkipBytes(length);

            return result;
        }

        public ulong DecodeUlong()
        {
            ReadOnlySpan<byte> bytes = DecodeByteArraySpan();
            return bytes.Length == 0 ? 0L : bytes.ReadEthUInt64();
        }

        public byte[] DecodeByteArray()
        {
            return DecodeByteArraySpan().ToArray();
        }

        public ReadOnlySpan<byte> DecodeByteArraySpan()
        {
            int prefix = ReadByte();
            if (prefix == 0)
            {
                return new byte[] { 0 };
            }

            if (prefix < 128)
            {
                return new[] { (byte)prefix };
            }

            if (prefix == 128)
            {
                return Array.Empty<byte>();
            }

            if (prefix <= 183)
            {
                int length = prefix - 128;
                Span<byte> buffer = Read(length);
                if (length == 1 && buffer[0] < 128)
                {
                    throw new RlpException($"Unexpected byte value {buffer[0]}");
                }

                return buffer;
            }

            if (prefix < 192)
            {
                int lengthOfLength = prefix - 183;
                if (lengthOfLength > 4)
                {
                    // strange but needed to pass tests - seems that spec gives int64 length and tests int32 length
                    throw new RlpException("Expected length of length less or equal 4");
                }

                int length = DeserializeLength(lengthOfLength);
                if (length < 56)
                {
                    throw new RlpException($"Expected length greater or equal 56 and was {length}");
                }

                return Read(length);
            }

            throw new RlpException($"Unexpected prefix value of {prefix} when decoding a byte array.");
        }

        public void SkipItem()
        {
            (int prefix, int content) = PeekPrefixAndContentLength();
            SkipBytes(prefix + content);
        }

        public void Reset()
        {
            Position = 0;
        }

        public void EncodeNullObject()
        {
            WriteByte(EmptySequenceByte);
        }

        public void EncodeEmptyByteArray()
        {
            WriteByte(EmptyArrayByte);
        }

        private const byte EmptyArrayByte = 128;

        private const byte EmptySequenceByte = 192;

        public override string ToString()
        {
            return $"[{nameof(RlpStream)}|{Position}/{Length}]";
        }
    }
}
