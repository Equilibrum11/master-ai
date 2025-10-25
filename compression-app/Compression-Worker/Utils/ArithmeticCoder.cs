using System.Text;

namespace Compression_Worker.Utils;

/// <summary>
/// Arithmetic coding encoder/decoder with adaptive probability distribution.
/// Achieves better compression than Huffman by using fractional bits.
///
/// Uses 32-bit precision for good balance between accuracy and performance.
/// Based on the algorithm by Witten, Neal, and Cleary (1987).
/// </summary>
public class ArithmeticCoder
{
    private const uint MaxRange = 0xFFFFFFFF; // 32-bit max
    private const uint QuarterRange = MaxRange / 4;
    private const uint HalfRange = MaxRange / 2;
    private const uint ThreeQuarterRange = 3 * QuarterRange;

    /// <summary>
    /// Encodes a sequence of symbols using arithmetic coding.
    /// </summary>
    /// <param name="symbols">Symbols to encode</param>
    /// <param name="getProbability">Function that returns (low, high, total) probabilities for each symbol</param>
    /// <returns>Encoded byte array</returns>
    public static byte[] Encode(List<string> symbols, Func<string, int, (uint low, uint high, uint total)> getProbability)
    {
        var output = new List<byte>();
        uint low = 0;
        uint high = MaxRange;
        int pendingBits = 0;

        for (int i = 0; i < symbols.Count; i++)
        {
            var symbol = symbols[i];
            var (symLow, symHigh, total) = getProbability(symbol, i);

            // Update range (use 64-bit arithmetic to avoid overflow)
            ulong range = (ulong)high - (ulong)low + 1;
            uint newHigh = (uint)(low + (range * symHigh / total) - 1);
            uint newLow = (uint)(low + (range * symLow / total));

            // DEBUG
            if (i >= 3 && i <= 5)
            {
                Console.WriteLine($"[AC-ENCODE] i={i}, symbol=\"{symbol}\", old[{low}, {high}], new[{newLow}, {newHigh}], symRange[{symLow}, {symHigh}]/{total}");
            }

            high = newHigh;
            low = newLow;

            // Output bits to prevent overflow
            int shiftCount = 0;
            string shiftLog = "";
            while (true)
            {
                char shiftType;
                if (high < HalfRange)
                {
                    // Output 0
                    OutputBit(output, 0, ref pendingBits);
                    shiftType = '0';
                }
                else if (low >= HalfRange)
                {
                    // Output 1
                    OutputBit(output, 1, ref pendingBits);
                    low -= HalfRange;
                    high -= HalfRange;
                    shiftType = '1';
                }
                else if (low >= QuarterRange && high < ThreeQuarterRange)
                {
                    // Middle convergence
                    pendingBits++;
                    low -= QuarterRange;
                    high -= QuarterRange;
                    shiftType = 'M';
                }
                else
                {
                    break;
                }

                low = 2 * low;
                high = 2 * high + 1;
                shiftCount++;
                shiftLog += shiftType;
            }

            // DEBUG - show state after bit shifting
            if (i >= 3 && i <= 5)
            {
                Console.WriteLine($"[AC-ENCODE-FINAL] i={i}, after_shift[{low}, {high}], shifts={shiftCount}, pattern={shiftLog}");
            }
        }

        // Output final bits to flush the encoder
        // Standard termination: output pendingBits+1 using the current low value
        pendingBits++;
        if (low < HalfRange)
        {
            OutputBit(output, 0, ref pendingBits);
        }
        else
        {
            OutputBit(output, 1, ref pendingBits);
        }

        // Pad to byte boundary
        while (output.Count % 8 != 0)
        {
            output.Add(0);
        }

        return ConvertBitsToBytes(output);
    }

    /// <summary>
    /// Decodes a byte array back to symbols.
    /// </summary>
    /// <param name="encoded">Encoded byte array</param>
    /// <param name="symbolCount">Number of symbols to decode</param>
    /// <param name="getSymbol">Function that returns symbol given cumulative probability and position</param>
    /// <returns>Decoded symbols</returns>
    public static List<string> Decode(byte[] encoded, int symbolCount,
                                      Func<uint, int, (string symbol, uint low, uint high, uint total)> getSymbol)
    {
        var bits = ConvertBytesToBits(encoded);
        var symbols = new List<string>();

        uint low = 0;
        uint high = MaxRange;
        uint value = 0;

        // Read initial value (32 bits)
        for (int i = 0; i < 32 && i < bits.Count; i++)
        {
            value = (value << 1) | (uint)bits[i];
        }

        // DEBUG
        Console.WriteLine($"[AC-DECODE] Initial: bits.Count={bits.Count}, value={value}, first 16 bits={string.Join("", bits.Take(16))}");

        int bitIndex = 32;

        for (int i = 0; i < symbolCount; i++)
        {
            // Find symbol that contains current value
            // Use 64-bit arithmetic to avoid overflow
            ulong range = (ulong)high - (ulong)low + 1;

            // Safety check to prevent divide by zero
            if (range == 0)
            {
                throw new InvalidOperationException($"Range became zero at position {i}. high={high}, low={low}");
            }

            // Calculate which symbol this value corresponds to
            // value is in range [low, high], we need to map to [0, total)
            ulong scaledValue = ((ulong)(value - low) * (ulong)getSymbol(0, i).total) / range;

            var (symbol, symLow, symHigh, total) = getSymbol((uint)scaledValue, i);

            // DEBUG
            if (i >= 3 && i <= 5)
            {
                Console.WriteLine($"[AC-DECODE] i={i}, value={value}, low={low}, high={high}, range={range}, total={total}, scaledValue={scaledValue}, symbol=\"{symbol}\"");
            }

            symbols.Add(symbol);

            // Update range (using 64-bit arithmetic to avoid overflow)
            uint newHigh = (uint)(low + (range * symHigh / total) - 1);
            uint newLow = (uint)(low + (range * symLow / total));

            // DEBUG
            if (i >= 3 && i <= 5)
            {
                Console.WriteLine($"[AC-DECODE-UPDATE] i={i}, old[{low}, {high}], new[{newLow}, {newHigh}], symRange[{symLow}, {symHigh}]/{total}");
            }

            high = newHigh;
            low = newLow;

            // Remove processed bits
            int shiftCount = 0;
            string shiftLog = "";
            while (true)
            {
                char shiftType;
                if (high < HalfRange)
                {
                    // Nothing
                    shiftType = '0';
                }
                else if (low >= HalfRange)
                {
                    value -= HalfRange;
                    low -= HalfRange;
                    high -= HalfRange;
                    shiftType = '1';
                }
                else if (low >= QuarterRange && high < ThreeQuarterRange)
                {
                    value -= QuarterRange;
                    low -= QuarterRange;
                    high -= QuarterRange;
                    shiftType = 'M';
                }
                else
                {
                    break;
                }

                low = 2 * low;
                high = 2 * high + 1;
                value = 2 * value;

                if (bitIndex < bits.Count)
                {
                    value |= (uint)bits[bitIndex++];
                }
                shiftCount++;
                shiftLog += shiftType;
            }

            // DEBUG - show state after bit shifting
            if (i >= 3 && i <= 5)
            {
                Console.WriteLine($"[AC-DECODE-FINAL] i={i}, after_shift[{low}, {high}], value={value}, shifts={shiftCount}, pattern={shiftLog}");
            }
        }

        return symbols;
    }

    private static void OutputBit(List<byte> output, byte bit, ref int pendingBits)
    {
        output.Add(bit);

        // Output pending bits (opposite of current bit)
        while (pendingBits > 0)
        {
            output.Add((byte)(1 - bit));
            pendingBits--;
        }
    }

    private static byte[] ConvertBitsToBytes(List<byte> bits)
    {
        var bytes = new List<byte>();
        for (int i = 0; i < bits.Count; i += 8)
        {
            byte b = 0;
            for (int j = 0; j < 8 && (i + j) < bits.Count; j++)
            {
                if (bits[i + j] == 1)
                {
                    b |= (byte)(1 << (7 - j));
                }
            }
            bytes.Add(b);
        }
        return bytes.ToArray();
    }

    private static List<byte> ConvertBytesToBits(byte[] bytes)
    {
        var bits = new List<byte>();
        foreach (var b in bytes)
        {
            for (int i = 7; i >= 0; i--)
            {
                bits.Add((byte)((b >> i) & 1));
            }
        }
        return bits;
    }

    /// <summary>
    /// Helper class to build cumulative probability distributions for encoding.
    /// </summary>
    public class ProbabilityModel
    {
        private readonly Dictionary<string, (uint low, uint high)> _symbolRanges;
        private readonly List<(string symbol, uint low, uint high)> _rangeList;
        private uint _total;

        public uint Total => _total;

        public ProbabilityModel()
        {
            _symbolRanges = new Dictionary<string, (uint, uint)>();
            _rangeList = new List<(string, uint, uint)>();
            _total = 0;
        }

        /// <summary>
        /// Adds a symbol with its probability to the model.
        /// </summary>
        public void AddSymbol(string symbol, double probability)
        {
            uint frequency = (uint)(probability * 10000); // Scale to integer
            if (frequency == 0) frequency = 1; // Minimum frequency

            uint low = _total;
            uint high = _total + frequency;

            _symbolRanges[symbol] = (low, high);
            _rangeList.Add((symbol, low, high));
            _total = high;
        }

        /// <summary>
        /// Gets cumulative probability range for a symbol.
        /// </summary>
        public (uint low, uint high, uint total) GetRange(string symbol)
        {
            if (_symbolRanges.TryGetValue(symbol, out var range))
            {
                return (range.low, range.high, _total);
            }

            // Unknown symbol - use uniform distribution
            return (0, 1, _total);
        }

        /// <summary>
        /// Finds symbol that contains given cumulative probability.
        /// </summary>
        public (string symbol, uint low, uint high, uint total) FindSymbol(uint cumulativeProb)
        {
            foreach (var (symbol, low, high) in _rangeList)
            {
                if (cumulativeProb >= low && cumulativeProb < high)
                {
                    return (symbol, low, high, _total);
                }
            }

            // Fallback to first symbol
            var first = _rangeList.FirstOrDefault();
            return (first.symbol ?? "<ERROR>", first.low, first.high, _total);
        }
    }
}
