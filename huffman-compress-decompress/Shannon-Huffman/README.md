# Shannon-Fano vs Huffman vs Arithmetic Coding

Simple Python implementation demonstrating that **Shannon-Fano**, **Huffman**, and **Arithmetic Coding** compression algorithms all produce equivalent decompressed output.

## Features

- **Shannon-Fano**: Top-down recursive median splitting algorithm
- **Huffman**: Bottom-up priority queue optimal tree building
- **Arithmetic Coding**: Simplified implementation using Huffman-based encoding
- **Equivalence Demo**: Shows all three algorithms decompress to identical text
- **Pure Python**: No external dependencies, uses only standard library
- **GUI Interface**: Visual comparison with code tables and statistics (3-panel view)

## Usage

### Run GUI (Graphical Interface)

```bash
python compression_ui.py
```

This launches a window where you can:
- Enter text or load files
- See side-by-side compression results for all THREE algorithms
- View complete code tables for Shannon-Fano and Huffman
- View frequency table for Arithmetic Coding
- Verify equivalence with visual feedback

### Run CLI Demo (default English text)

```bash
python Shannon_Huffman.py
```

### Run CLI with Custom Text File

```bash
python Shannon_Huffman.py samples/romanian_medium.txt
```

## Output Example

```
======================================================================
SHANNON-FANO vs HUFFMAN vs ARITHMETIC CODING COMPARISON
======================================================================

Original Text (94 chars):
"Compresie text românesc folosind algoritmi Shannon și Huffman..."

----------------------------------------------------------------------
1. SHANNON-FANO COMPRESSION
----------------------------------------------------------------------

Shannon-Fano Results:
  Original Size: 99 bytes
  Compressed Size: 52 bytes
  Compression Ratio: 1.90x
  Space Saved: 47.47%
  Unique Characters: 28

----------------------------------------------------------------------
2. HUFFMAN COMPRESSION
----------------------------------------------------------------------

Huffman Results:
  Original Size: 99 bytes
  Compressed Size: 52 bytes
  Compression Ratio: 1.90x
  Space Saved: 47.47%
  Unique Characters: 28

----------------------------------------------------------------------
3. ARITHMETIC CODING COMPRESSION
----------------------------------------------------------------------

Arithmetic Results:
  Original Size: 99 bytes
  Compressed Size: 52 bytes
  Compression Ratio: 1.90x
  Space Saved: 47.47%
  Unique Characters: 28

----------------------------------------------------------------------
4. DECOMPRESSION TEST
----------------------------------------------------------------------
Shannon-Fano decompression:  PASS
Huffman decompression:       PASS
Arithmetic decompression:    PASS

======================================================================
SUCCESS: All three algorithms produce identical output!
======================================================================
```

## How It Works

### Compression

1. **Analyze frequencies**: Count occurrence of each character
2. **Build tree**:
   - Shannon-Fano: Recursively split by median frequency
   - Huffman: Merge two lowest-frequency nodes in priority queue
3. **Generate codes**: Traverse tree (0=left, 1=right)
4. **Encode**: Replace characters with binary codes
5. **Pack**: Convert bit string to bytes

### Decompression

1. **Unpack**: Convert bytes back to bit string
2. **Traverse tree**: Navigate using bits (0=left, 1=right)
3. **Output character** when reaching leaf node
4. **Repeat** until all bits consumed

## Algorithms Explained

### Shannon-Fano

- Top-down approach
- Sorts characters by frequency
- Recursively splits at median cumulative frequency
- Assigns 0 to top half, 1 to bottom half
- Not always optimal, but close

### Huffman

- Bottom-up approach
- Uses priority queue (min-heap)
- Repeatedly merges two lowest-frequency nodes
- **Provably optimal** for character-based encoding
- Typically 1-3% better compression than Shannon-Fano

### Arithmetic Coding

- Encodes entire message as single number
- Can achieve fractional bits per symbol
- Theoretically optimal (approaches entropy limit)
- This implementation: simplified version using Huffman tree
- Real arithmetic coding uses range encoding for better compression

## Sample Files

Four Romanian text samples included in `samples/`:

- `romanian_short.txt` - ~100 bytes (quick test)
- `romanian_medium.txt` - ~500 bytes (medium text)
- `romanian_long.txt` - ~3 KB (full article)
- `romanian_10kb.txt` - ~15 KB (comprehensive text, 10 chapters)

All include Romanian diacritics (ă, â, î, ș, ț).

### Compression Results by File Size

| File | Size | Shannon-Fano | Huffman | Space Saved |
|------|------|--------------|---------|-------------|
| Short | 99 B | 1.90x | 1.90x | ~47% |
| Medium | 558 B | 1.88x | 1.89x | ~47% |
| Long | 3 KB | 1.68x | 1.72x | ~40% |
| 10KB | 15 KB | 1.84x | 1.85x | ~46% |

*Larger files generally achieve better compression ratios.*

## Requirements

- Python 3.6 or higher
- No external dependencies (uses only standard library)

## Project Structure

```
Shannon-Huffman/
├── Shannon_Huffman.py    # CLI script (simple, no OOP)
├── compression_ui.py     # GUI application (tkinter)
├── samples/              # Test data files
│   ├── romanian_short.txt
│   ├── romanian_medium.txt
│   └── romanian_long.txt
└── README.md             # This file
```

## Technical Notes

- **Lossless**: Both algorithms are 100% reversible
- **Character-based**: Encodes individual characters, not byte sequences
- **Tree structure**: Different between Shannon-Fano and Huffman, but both work
- **Compression ratio**: Huffman slightly better (provably optimal)

## License

Educational project for Advanced Methods for Data Security course.

## Author

2025-10-28
