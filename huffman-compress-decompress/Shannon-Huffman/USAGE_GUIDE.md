# Usage Guide - Shannon-Fano vs Huffman Compression

## Quick Start

### Option 1: Graphical Interface (Recommended)

```bash
python compression_ui.py
```

**What you'll see:**

```
┌─────────────────────────────────────────────────────────┐
│     Shannon-Fano vs Huffman Compression                 │
├─────────────────────────────────────────────────────────┤
│ Input Text                                              │
│ [Load File] [Load Sample] [Clear]                      │
│ ┌─────────────────────────────────────────────────┐   │
│ │ Enter or paste your text here...                │   │
│ └─────────────────────────────────────────────────┘   │
│                                                         │
│         [COMPRESS WITH BOTH ALGORITHMS]                │
│                                                         │
├──────────────────────┬──────────────────────────────────┤
│ Shannon-Fano Results │ Huffman Results                  │
├──────────────────────┼──────────────────────────────────┤
│ Statistics:          │ Statistics:                      │
│ Original: 294 bytes  │ Original: 294 bytes              │
│ Compressed: 167 B    │ Compressed: 166 B                │
│ Ratio: 1.76x         │ Ratio: 1.77x                     │
│ Space Saved: 43.20%  │ Space Saved: 43.54%              │
│                      │                                  │
│ Code Table:          │ Code Table:                      │
│ 'e'  →  000          │ 'e'  →  010                      │
│ ' '  →  001          │ ' '  →  011                      │
│ 't'  →  010          │ 't'  →  000                      │
│ ...                  │ ...                              │
├──────────────────────┴──────────────────────────────────┤
│ Equivalence Test                                        │
│ ✓ SUCCESS: EQUIVALENCE VERIFIED!                       │
│ Shannon-Fano decompression: PASS ✓                     │
│ Huffman decompression: PASS ✓                          │
└─────────────────────────────────────────────────────────┘
```

**Features:**
- **Load File**: Open any .txt file
- **Load Sample**: Quick test with built-in text
- **Side-by-side comparison**: See both algorithms at once
- **Complete code tables**: View all character encodings
- **Visual feedback**: Green success, red if failed

### Option 2: Command Line

```bash
# Default demo with English text
python Shannon_Huffman.py

# Custom file
python Shannon_Huffman.py samples/romanian_medium.txt
```

## Step-by-Step Examples

### Example 1: Compare Algorithms with Sample Text

1. Run: `python compression_ui.py`
2. Click **"Load Sample"**
3. Click **"COMPRESS WITH BOTH ALGORITHMS"**
4. **Observe:**
   - Left panel shows Shannon-Fano results
   - Right panel shows Huffman results
   - Bottom shows equivalence verification (✓ PASS)
   - Code tables show different encodings

### Example 2: Test with Romanian Text

1. Run: `python compression_ui.py`
2. Click **"Load File"**
3. Select `samples/romanian_medium.txt`
4. Click **"COMPRESS WITH BOTH ALGORITHMS"**
5. **Notice:**
   - Both algorithms handle Romanian diacritics (ă, â, î, ș, ț)
   - Huffman typically 1-2% better compression
   - Both decompress perfectly to original

### Example 3: Enter Custom Text

1. Run: `python compression_ui.py`
2. Type or paste text in the input box
3. Click **"COMPRESS WITH BOTH ALGORITHMS"**
4. **Compare:**
   - Compression ratios
   - Code table sizes
   - Space savings percentages

## Understanding the Results

### Statistics Explained

```
Original Size: 294 bytes
  → Size of input text in UTF-8 encoding

Compressed Size: 166 bytes
  → Size after compression (actual binary data)

Compression Ratio: 1.77x
  → Original size ÷ Compressed size
  → Higher is better

Space Saved: 43.54%
  → Percentage reduction in size
  → Higher is better

Unique Characters: 39
  → Number of different characters in text
  → More unique chars = harder to compress
```

### Code Table Explained

Shows how each character is encoded:

```
Char  →  Binary Code
━━━━━━━━━━━━━━━━━━━━
'e'   →  010          (3 bits - frequent character)
' '   →  011          (3 bits - space is common)
't'   →  000          (3 bits)
'x'   →  111010101    (9 bits - rare character)
```

**Key insight:** Frequent characters get shorter codes!

### Equivalence Test

```
✓ SUCCESS: EQUIVALENCE VERIFIED!
Shannon-Fano decompression: PASS ✓
Huffman decompression: PASS ✓

Both algorithms successfully decompress to IDENTICAL output!
```

This proves:
- Both algorithms are **lossless** (no data lost)
- Different compression methods produce **same final result**
- You can compress with Shannon-Fano and decompress with Huffman (conceptually)

## Typical Results

### English Text (300 characters)
- **Shannon-Fano**: 1.76x ratio, 43% savings
- **Huffman**: 1.77x ratio, 43.5% savings
- **Winner**: Huffman (+0.5%)

### Romanian Text (500 characters)
- **Shannon-Fano**: 1.88x ratio, 46.8% savings
- **Huffman**: 1.89x ratio, 47% savings
- **Winner**: Huffman (+0.2%)

### Why Huffman Wins
- Huffman is **provably optimal** for character-based encoding
- Shannon-Fano is close but not always optimal
- Difference is usually 0.5-3%

## Tips

### Best Compression Results
✓ **Longer text** = better compression ratios
✓ **Repetitive text** = higher space savings
✓ **Natural language** = ~40-50% compression
✗ **Random data** = minimal compression
✗ **Already compressed** = may increase size!

### Testing Ideas
- Compare different languages (English vs Romanian)
- Test with code (Python, C++, etc.)
- Try highly repetitive text ("aaaaabbbbcccc")
- Test worst case (all unique characters)

## Troubleshooting

### UI doesn't start
- Make sure you're using Python 3.6+
- tkinter is included in Python standard library
- On Linux: `sudo apt-get install python3-tk`

### Unicode errors with Romanian text
- Files must be UTF-8 encoded
- Use a proper text editor (VS Code, Notepad++, etc.)

### "No compression" or ratio < 1.0
- Text is too short (need at least ~100 chars)
- Text has too many unique characters
- Already compressed data can't be compressed further

## Next Steps

Want to go deeper? Try:
1. Modify the code to save/load compressed files
2. Add tree visualization (graphical tree structure)
3. Implement other algorithms (LZW, arithmetic coding)
4. Compare compression speeds
5. Add dictionary-based compression

---

**Have fun exploring compression algorithms!**
