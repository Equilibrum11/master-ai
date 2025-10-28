"""
Simple UI for Shannon-Fano, Huffman, and Arithmetic Coding Compression
Shows compression statistics, code tables, and comparison
"""

import tkinter as tk
from tkinter import ttk, scrolledtext, filedialog, messagebox
from Shannon_Huffman import (
    shannon_fano_compress, huffman_compress, arithmetic_compress,
    shannon_fano_decompress, huffman_decompress, arithmetic_decompress
)


class CompressionUI:
    def __init__(self, root):
        self.root = root
        self.root.title("Shannon-Fano vs Huffman vs Arithmetic Coding")
        self.root.geometry("1400x850")

        # Variables
        self.original_text = ""
        self.shannon_data = None
        self.huffman_data = None
        self.arithmetic_data = None

        self.create_widgets()

    def create_widgets(self):
        # Title
        title = tk.Label(self.root, text="Shannon-Fano vs Huffman vs Arithmetic Coding",
                        font=("Arial", 16, "bold"), pady=10)
        title.pack()

        # Input section
        input_frame = ttk.LabelFrame(self.root, text="Input Text", padding=10)
        input_frame.pack(fill="both", padx=10, pady=5, expand=False)

        # Buttons
        btn_frame = tk.Frame(input_frame)
        btn_frame.pack(fill="x", pady=(0, 5))

        ttk.Button(btn_frame, text="Load File",
                  command=self.load_file).pack(side="left", padx=5)
        ttk.Button(btn_frame, text="Load Sample",
                  command=self.load_sample).pack(side="left", padx=5)
        ttk.Button(btn_frame, text="Clear",
                  command=self.clear_input).pack(side="left", padx=5)

        # Text input
        self.input_text = scrolledtext.ScrolledText(input_frame, height=5,
                                                    wrap=tk.WORD, font=("Consolas", 9))
        self.input_text.pack(fill="both", expand=True)

        # Compress button
        compress_frame = tk.Frame(self.root)
        compress_frame.pack(pady=10)
        ttk.Button(compress_frame, text="COMPRESS WITH ALL THREE ALGORITHMS",
                  command=self.compress_all,
                  style="Big.TButton").pack()

        # Results section (3 columns)
        results_frame = tk.Frame(self.root)
        results_frame.pack(fill="both", expand=True, padx=10, pady=5)

        # Shannon-Fano results (left)
        shannon_frame = ttk.LabelFrame(results_frame, text="Shannon-Fano Results",
                                       padding=8)
        shannon_frame.pack(side="left", fill="both", expand=True, padx=(0, 3))

        self.shannon_stats = tk.Text(shannon_frame, height=6, width=30,
                                     font=("Consolas", 8), state="disabled")
        self.shannon_stats.pack(fill="x", pady=(0, 3))

        tk.Label(shannon_frame, text="Code Table:",
                font=("Arial", 8, "bold")).pack(anchor="w")
        self.shannon_codes = scrolledtext.ScrolledText(shannon_frame, height=12,
                                                       font=("Consolas", 7))
        self.shannon_codes.pack(fill="both", expand=True)

        # Huffman results (middle)
        huffman_frame = ttk.LabelFrame(results_frame, text="Huffman Results",
                                       padding=8)
        huffman_frame.pack(side="left", fill="both", expand=True, padx=3)

        self.huffman_stats = tk.Text(huffman_frame, height=6, width=30,
                                     font=("Consolas", 8), state="disabled")
        self.huffman_stats.pack(fill="x", pady=(0, 3))

        tk.Label(huffman_frame, text="Code Table:",
                font=("Arial", 8, "bold")).pack(anchor="w")
        self.huffman_codes = scrolledtext.ScrolledText(huffman_frame, height=12,
                                                       font=("Consolas", 7))
        self.huffman_codes.pack(fill="both", expand=True)

        # Arithmetic results (right)
        arithmetic_frame = ttk.LabelFrame(results_frame, text="Arithmetic Coding Results",
                                         padding=8)
        arithmetic_frame.pack(side="left", fill="both", expand=True, padx=(3, 0))

        self.arithmetic_stats = tk.Text(arithmetic_frame, height=6, width=30,
                                       font=("Consolas", 8), state="disabled")
        self.arithmetic_stats.pack(fill="x", pady=(0, 3))

        tk.Label(arithmetic_frame, text="Frequency Table:",
                font=("Arial", 8, "bold")).pack(anchor="w")
        self.arithmetic_codes = scrolledtext.ScrolledText(arithmetic_frame, height=12,
                                                         font=("Consolas", 7))
        self.arithmetic_codes.pack(fill="both", expand=True)

        # Comparison section
        comparison_frame = ttk.LabelFrame(self.root, text="Equivalence Test",
                                         padding=10)
        comparison_frame.pack(fill="x", padx=10, pady=5)

        self.comparison_text = tk.Text(comparison_frame, height=4,
                                      font=("Consolas", 9), state="disabled")
        self.comparison_text.pack(fill="x")

        # Configure button style
        style = ttk.Style()
        style.configure("Big.TButton", font=("Arial", 10, "bold"))

    def load_file(self):
        """Load text from file"""
        filename = filedialog.askopenfilename(
            title="Select text file",
            filetypes=[("Text files", "*.txt"), ("All files", "*.*")]
        )
        if filename:
            try:
                with open(filename, 'r', encoding='utf-8') as f:
                    text = f.read()
                self.input_text.delete(1.0, tk.END)
                self.input_text.insert(1.0, text)
            except Exception as e:
                messagebox.showerror("Error", f"Failed to load file: {e}")

    def load_sample(self):
        """Load sample text"""
        sample = """Information theory, developed by Claude Shannon in 1948, revolutionized modern communications. Compression algorithms like Huffman, Shannon-Fano, and Arithmetic Coding reduce redundancy in text."""
        self.input_text.delete(1.0, tk.END)
        self.input_text.insert(1.0, sample)

    def clear_input(self):
        """Clear input text"""
        self.input_text.delete(1.0, tk.END)

    def compress_all(self):
        """Compress with all three algorithms and show results"""
        # Get input text
        text = self.input_text.get(1.0, tk.END).strip()
        if not text:
            messagebox.showwarning("Warning", "Please enter some text first!")
            return

        self.original_text = text

        try:
            # Compress with Shannon-Fano
            shannon_compressed, shannon_tree, shannon_padding, shannon_codes = \
                shannon_fano_compress(text)

            # Compress with Huffman
            huffman_compressed, huffman_tree, huffman_padding, huffman_codes = \
                huffman_compress(text)

            # Compress with Arithmetic
            arithmetic_compressed, arithmetic_metadata, arithmetic_padding = \
                arithmetic_compress(text)

            # Store data
            self.shannon_data = (shannon_compressed, shannon_tree, shannon_padding)
            self.huffman_data = (huffman_compressed, huffman_tree, huffman_padding)
            self.arithmetic_data = (arithmetic_compressed, arithmetic_metadata, arithmetic_padding)

            # Display results
            self.display_stats(self.shannon_stats, "Shannon-Fano", text,
                             shannon_compressed, shannon_codes)
            self.display_codes(self.shannon_codes, shannon_codes)

            self.display_stats(self.huffman_stats, "Huffman", text,
                             huffman_compressed, huffman_codes)
            self.display_codes(self.huffman_codes, huffman_codes)

            self.display_stats(self.arithmetic_stats, "Arithmetic", text,
                             arithmetic_compressed, arithmetic_metadata['frequencies'])
            self.display_codes(self.arithmetic_codes, arithmetic_metadata['frequencies'], is_freq=True)

            # Test decompression
            shannon_decompressed = shannon_fano_decompress(
                shannon_compressed, shannon_tree, shannon_padding)
            huffman_decompressed = huffman_decompress(
                huffman_compressed, huffman_tree, huffman_padding)
            arithmetic_decompressed = arithmetic_decompress(
                arithmetic_compressed, arithmetic_metadata, arithmetic_padding)

            # Display comparison
            self.display_comparison(text, shannon_decompressed, huffman_decompressed,
                                  arithmetic_decompressed)

        except Exception as e:
            messagebox.showerror("Error", f"Compression failed: {e}")
            import traceback
            traceback.print_exc()

    def display_stats(self, widget, name, original, compressed, codes):
        """Display compression statistics"""
        original_size = len(original.encode('utf-8'))
        compressed_size = len(compressed)
        ratio = original_size / compressed_size if compressed_size > 0 else 0
        savings = (1 - compressed_size / original_size) * 100 if original_size > 0 else 0

        stats = f"""{name}:
{'-' * 35}
Original:     {original_size:>6} bytes
Compressed:   {compressed_size:>6} bytes
Ratio:        {ratio:>6.2f}x
Saved:        {savings:>6.2f}%
Symbols:      {len(codes):>6}
"""

        widget.config(state="normal")
        widget.delete(1.0, tk.END)
        widget.insert(1.0, stats)
        widget.config(state="disabled")

    def display_codes(self, widget, codes, is_freq=False):
        """Display code table or frequency table"""
        widget.delete(1.0, tk.END)

        if is_freq:
            # Display frequency table
            widget.insert(tk.END, "Char  Frequency\n")
            widget.insert(tk.END, "-" * 20 + "\n")
            sorted_items = sorted(codes.items(), key=lambda x: x[1], reverse=True)
        else:
            # Display code table
            widget.insert(tk.END, "Char  Code\n")
            widget.insert(tk.END, "-" * 20 + "\n")
            sorted_items = sorted(codes.items(), key=lambda x: (len(x[1]), x[0]))

        for char, value in sorted_items[:30]:  # Show first 30
            # Handle special characters
            if char == ' ':
                char_display = "' '"
            elif char == '\n':
                char_display = "'\\n'"
            elif char == '\t':
                char_display = "'\\t'"
            else:
                char_display = f"'{char}'"

            if is_freq:
                widget.insert(tk.END, f"{char_display:>4}  {value:>4}\n")
            else:
                widget.insert(tk.END, f"{char_display:>4}  {value}\n")

        if len(codes) > 30:
            widget.insert(tk.END, f"\n... ({len(codes) - 30} more)")

    def display_comparison(self, original, shannon_dec, huffman_dec, arithmetic_dec):
        """Display decompression comparison"""
        shannon_match = (shannon_dec == original)
        huffman_match = (huffman_dec == original)
        arithmetic_match = (arithmetic_dec == original)
        all_match = shannon_match and huffman_match and arithmetic_match

        if all_match:
            result = """SUCCESS: EQUIVALENCE VERIFIED!
""" + "=" * 60 + """
Shannon-Fano decompression:   PASS
Huffman decompression:        PASS
Arithmetic decompression:     PASS

All three algorithms produce IDENTICAL output!"""
            bg_color = "#d4edda"
            fg_color = "#155724"
        else:
            result = """FAILED: Decompression mismatch!
""" + "=" * 60 + """
Shannon-Fano:  """ + ("PASS" if shannon_match else "FAIL") + """
Huffman:       """ + ("PASS" if huffman_match else "FAIL") + """
Arithmetic:    """ + ("PASS" if arithmetic_match else "FAIL")
            bg_color = "#f8d7da"
            fg_color = "#721c24"

        self.comparison_text.config(state="normal", bg=bg_color, fg=fg_color)
        self.comparison_text.delete(1.0, tk.END)
        self.comparison_text.insert(1.0, result)
        self.comparison_text.config(state="disabled")


def main():
    root = tk.Tk()
    app = CompressionUI(root)
    root.mainloop()


if __name__ == '__main__':
    main()
