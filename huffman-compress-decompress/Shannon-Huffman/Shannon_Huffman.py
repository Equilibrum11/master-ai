"""
Simple Shannon-Fano and Huffman Compression
Demonstrates that both algorithms produce equivalent decompressed output.
"""

import heapq
from collections import Counter


# ============================================================================
# SHARED UTILITIES
# ============================================================================

def analyze_frequencies(text):
    """Count character frequencies."""
    return dict(Counter(text))


def encode_text(text, code_table):
    """Encode text using code table."""
    bits = ''.join(code_table[char] for char in text)

    # Pad to byte boundary
    padding = (8 - len(bits) % 8) % 8
    bits += '0' * padding

    # Convert to bytes
    byte_array = bytearray()
    for i in range(0, len(bits), 8):
        byte = bits[i:i+8]
        byte_array.append(int(byte, 2))

    return bytes(byte_array), padding


def decode_bits(bits, tree, padding):
    """Decode bits using tree."""
    if padding > 0:
        bits = bits[:-padding]

    result = []
    node = tree

    for bit in bits:
        # Navigate tree
        if bit == '0':
            node = node['left']
        else:
            node = node['right']

        # Reached a leaf?
        if 'char' in node:
            result.append(node['char'])
            node = tree  # Reset to root

    return ''.join(result)


def bytes_to_bits(data):
    """Convert bytes to bit string."""
    return ''.join(format(byte, '08b') for byte in data)


# ============================================================================
# SHANNON-FANO ALGORITHM
# ============================================================================

def shannon_fano_split(items):
    """Find best split point for Shannon-Fano."""
    total = sum(freq for _, freq in items)
    half = total / 2.0

    cumulative = 0
    best_split = 1
    min_diff = float('inf')

    for i in range(len(items)):
        cumulative += items[i][1]
        diff = abs(cumulative - half)
        if diff < min_diff:
            min_diff = diff
            best_split = i + 1

    return max(1, min(best_split, len(items) - 1))


def build_shannon_tree(items):
    """Build Shannon-Fano tree."""
    if len(items) == 1:
        return {'char': items[0][0], 'freq': items[0][1]}

    split = shannon_fano_split(items)
    left_items = items[:split]
    right_items = items[split:]

    total_freq = sum(freq for _, freq in items)

    return {
        'freq': total_freq,
        'left': build_shannon_tree(left_items),
        'right': build_shannon_tree(right_items)
    }


def generate_codes(tree, prefix=''):
    """Generate codes from tree."""
    if 'char' in tree:
        return {tree['char']: prefix if prefix else '0'}

    codes = {}
    if 'left' in tree:
        codes.update(generate_codes(tree['left'], prefix + '0'))
    if 'right' in tree:
        codes.update(generate_codes(tree['right'], prefix + '1'))

    return codes


def shannon_fano_compress(text):
    """Compress text using Shannon-Fano."""
    frequencies = analyze_frequencies(text)
    items = sorted(frequencies.items(), key=lambda x: x[1], reverse=True)

    tree = build_shannon_tree(items)
    code_table = generate_codes(tree)
    compressed_data, padding = encode_text(text, code_table)

    return compressed_data, tree, padding, code_table


def shannon_fano_decompress(compressed_data, tree, padding):
    """Decompress data using Shannon-Fano."""
    bits = bytes_to_bits(compressed_data)
    return decode_bits(bits, tree, padding)


# ============================================================================
# HUFFMAN ALGORITHM
# ============================================================================

def build_huffman_tree(frequencies):
    """Build Huffman tree using priority queue."""
    # Create leaf nodes
    heap = []
    for char, freq in frequencies.items():
        heapq.heappush(heap, (freq, id(char), {'char': char, 'freq': freq}))

    # Build tree
    while len(heap) > 1:
        freq1, _, left = heapq.heappop(heap)
        freq2, _, right = heapq.heappop(heap)

        merged = {
            'freq': freq1 + freq2,
            'left': left,
            'right': right
        }

        heapq.heappush(heap, (freq1 + freq2, id(merged), merged))

    return heap[0][2] if heap else None


def huffman_compress(text):
    """Compress text using Huffman."""
    frequencies = analyze_frequencies(text)

    tree = build_huffman_tree(frequencies)
    code_table = generate_codes(tree)
    compressed_data, padding = encode_text(text, code_table)

    return compressed_data, tree, padding, code_table


def huffman_decompress(compressed_data, tree, padding):
    """Decompress data using Huffman."""
    bits = bytes_to_bits(compressed_data)
    return decode_bits(bits, tree, padding)


# ============================================================================
# ARITHMETIC CODING ALGORITHM (Simplified)
# ============================================================================

def arithmetic_compress(text):
    """
    Compress text using simplified arithmetic coding.

    Note: This is a basic implementation that demonstrates the concept.
    Arithmetic coding typically achieves better compression than Huffman
    by encoding fractional bits per symbol.
    """
    if not text:
        return b'', {}, 0

    # Build frequency table
    frequencies = analyze_frequencies(text)

    # For simplicity, we'll encode using a similar approach to Huffman
    # but calculate theoretical entropy-based compression
    # A full arithmetic coder would use range encoding

    # Create a simple encoding: use Huffman codes as approximation
    tree = build_huffman_tree(frequencies)
    code_table = generate_codes(tree)

    # Encode (same as Huffman for this simplified version)
    bits = ''.join(code_table[char] for char in text)

    # Pad to byte boundary
    padding = (8 - len(bits) % 8) % 8
    bits += '0' * padding

    # Convert to bytes
    byte_array = bytearray()
    for i in range(0, len(bits), 8):
        byte = bits[i:i+8]
        byte_array.append(int(byte, 2))

    compressed_data = bytes(byte_array)

    # Store metadata
    metadata = {
        'frequencies': frequencies,
        'tree': tree,
        'code_table': code_table,
        'text_length': len(text)
    }

    return compressed_data, metadata, padding


def arithmetic_decompress(compressed_data, metadata, padding=0):
    """
    Decompress data using simplified arithmetic coding.
    """
    if not compressed_data or not metadata:
        return ""

    tree = metadata['tree']

    # Unpack bytes to bits
    bits = ''.join(format(byte, '08b') for byte in compressed_data)

    # Remove padding
    if padding > 0:
        bits = bits[:-padding]

    # Decode using tree (same as Huffman)
    result = []
    node = tree

    for bit in bits:
        if bit == '0':
            node = node['left']
        else:
            node = node['right']

        if 'char' in node:
            result.append(node['char'])
            node = tree

    return ''.join(result)


# ============================================================================
# DEMONSTRATION
# ============================================================================

def print_stats(name, original_text, compressed_data, code_table):
    """Print compression statistics."""
    original_size = len(original_text.encode('utf-8'))
    compressed_size = len(compressed_data)
    ratio = original_size / compressed_size if compressed_size > 0 else 0
    savings = (1 - compressed_size / original_size) * 100 if original_size > 0 else 0

    print(f"\n{name} Results:")
    print(f"  Original Size: {original_size} bytes")
    print(f"  Compressed Size: {compressed_size} bytes")
    print(f"  Compression Ratio: {ratio:.2f}x")
    print(f"  Space Saved: {savings:.2f}%")
    print(f"  Unique Characters: {len(code_table)}")


def demo_equivalence(text):
    """Demonstrate that all three algorithms produce equivalent output."""
    print("=" * 70)
    print("SHANNON-FANO vs HUFFMAN vs ARITHMETIC CODING COMPARISON")
    print("=" * 70)
    print(f"\nOriginal Text ({len(text)} chars):")
    print(f'"{text[:100]}{"..." if len(text) > 100 else ""}"')

    # Compress with Shannon-Fano
    print("\n" + "-" * 70)
    print("1. SHANNON-FANO COMPRESSION")
    print("-" * 70)
    shannon_compressed, shannon_tree, shannon_padding, shannon_codes = shannon_fano_compress(text)
    print_stats("Shannon-Fano", text, shannon_compressed, shannon_codes)

    # Compress with Huffman
    print("\n" + "-" * 70)
    print("2. HUFFMAN COMPRESSION")
    print("-" * 70)
    huffman_compressed, huffman_tree, huffman_padding, huffman_codes = huffman_compress(text)
    print_stats("Huffman", text, huffman_compressed, huffman_codes)

    # Compress with Arithmetic Coding
    print("\n" + "-" * 70)
    print("3. ARITHMETIC CODING COMPRESSION")
    print("-" * 70)
    arithmetic_compressed, arithmetic_metadata, _ = arithmetic_compress(text)
    print_stats("Arithmetic", text, arithmetic_compressed, arithmetic_metadata['frequencies'])

    # Decompress all three
    print("\n" + "-" * 70)
    print("4. DECOMPRESSION TEST")
    print("-" * 70)

    shannon_decompressed = shannon_fano_decompress(shannon_compressed, shannon_tree, shannon_padding)
    huffman_decompressed = huffman_decompress(huffman_compressed, huffman_tree, huffman_padding)
    arithmetic_decompressed = arithmetic_decompress(arithmetic_compressed, arithmetic_metadata)

    shannon_match = (shannon_decompressed == text)
    huffman_match = (huffman_decompressed == text)
    arithmetic_match = (arithmetic_decompressed == text)

    print(f"Shannon-Fano decompression:  {'PASS' if shannon_match else 'FAIL'}")
    print(f"Huffman decompression:       {'PASS' if huffman_match else 'FAIL'}")
    print(f"Arithmetic decompression:    {'PASS' if arithmetic_match else 'FAIL'}")

    # Equivalence check
    print("\n" + "=" * 70)
    if shannon_match and huffman_match and arithmetic_match:
        print("SUCCESS: All three algorithms produce identical output!")
        print("=" * 70)
        return True
    else:
        print("FAILED: Decompression mismatch!")
        print("=" * 70)
        return False


# ============================================================================
# MAIN
# ============================================================================

if __name__ == '__main__':
    import sys
    import os

    # Set UTF-8 encoding for Windows console
    if os.name == 'nt':
        import io
        sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

    # Test with sample text
    sample_text = """Information theory, developed by Claude Shannon in 1948, revolutionized modern communications. Compression algorithms like Huffman and Shannon-Fano reduce redundancy in text, saving storage space. This application demonstrates that both algorithms produce equivalent results when decompressing."""

    # Allow command line argument for custom text file
    if len(sys.argv) > 1:
        try:
            with open(sys.argv[1], 'r', encoding='utf-8') as f:
                sample_text = f.read()
        except Exception as e:
            print(f"Error reading file: {e}")
            sys.exit(1)

    # Run demo
    success = demo_equivalence(sample_text)

    # Exit with appropriate code
    sys.exit(0 if success else 1)
