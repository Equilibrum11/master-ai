using Compression_Worker.Core.Interfaces;
using Compression_Worker.Core.Models;
using Compression_Worker.Data;
using Compression_Worker.Pipeline;
using Compression_Worker.Processors.Lemmatization;
using Compression_Worker.Processors.Normalization;
using Compression_Worker.Processors.Semantic;
using Compression_Worker.Processors.Statistical;
using Compression_Worker.Processors.Stopwords;
using Compression_Worker.Utils;
using System.Text;

namespace Compression_Worker;

/// <summary>
/// Romanian Text Compression Application
/// Compression Tester: Advanced Methods for Data Security
///
/// Demonstrates information-theoretic compression based on linguistic redundancy.
/// Research shows ~60% of natural language is redundant and can be eliminated.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        Console.WriteLine("=================================================");
        Console.WriteLine("  Romanian Text Compression Tool");
        Console.WriteLine("  Compression Tester - Data Security Methods");
        Console.WriteLine("=================================================\n");

        if (args.Length == 0)
        {
            ShowUsage();
            RunInteractiveDemo();
            return;
        }

        try
        {
            var options = ParseArguments(args);

            // Check special modes
            if (options.Mode == "train-bpe")
            {
                TrainBPEModel(options);
            }
            else if (options.Mode == "build-model")
            {
                BuildLanguageModel(options);
            }
            else
            {
                ProcessFile(options);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"\nException Type: {ex.GetType().Name}");
            Console.WriteLine($"Stack Trace:\n{ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"\nInner Exception: {ex.InnerException.Message}");
            }
            Console.WriteLine("\nRun without arguments for interactive demo.");
            Environment.Exit(1);
        }
    }

    static void RunInteractiveDemo()
    {
        Console.WriteLine("\n--- Interactive Demo Mode ---\n");

        // Example Romanian text
        string sampleText = @"Copiii mergeau foarte repede spre casele lor frumoase.
Ei aveau multe jucării și cărți interesante.
Toți copiii erau foarte fericiți și veseli când mergeau acasă.";

        // First, test the RT.ArithmeticCoding library
        // Tests.TestRTArithmeticCoding.RunTests();

        Console.WriteLine("Sample Romanian text:");
        Console.WriteLine($"\"{sampleText}\"\n");

        // Build compression pipeline (default: Accurate mode)
        var pipeline = BuildPipeline(StemmingMode.Accurate);

        Console.WriteLine("Pipeline processors:");
        foreach (var processor in ((CompressionPipeline)pipeline).GetProcessorNames())
        {
            Console.WriteLine($"  - {processor}");
        }
        Console.WriteLine($"Stemming Mode: Accurate (with wordlist validation)");
        Console.WriteLine();

        // Compress
        Console.WriteLine("--- COMPRESSION ---");
        var context = pipeline.Execute(sampleText);
        string compressed = context.Text;

        Console.WriteLine($"Compressed: \"{compressed}\"\n");

        // Show metrics
        var report = CompressionMetrics.GenerateReport(sampleText, compressed);
        Console.WriteLine(report);
        Console.WriteLine();

        // Decompress
        Console.WriteLine("--- DECOMPRESSION ---");
        string decompressed = pipeline.Reverse(context);
        Console.WriteLine($"Decompressed: \"{decompressed}\"\n");

        // Verify reversibility
        bool isReversible = string.Equals(sampleText, decompressed, StringComparison.Ordinal);
        Console.WriteLine($"Reversibility check: {(isReversible ? "✓ PASSED" : "✗ FAILED")}");

        if (!isReversible)
        {
            Console.WriteLine("\nDifferences detected:");
            Console.WriteLine($"Original length:     {sampleText.Length}");
            Console.WriteLine($"Decompressed length: {decompressed.Length}");
        }

        Console.WriteLine("\n--- Demo Complete ---");
        Console.WriteLine("Run with --help to see command-line options.");
    }

    static void BuildLanguageModel(CommandLineOptions options)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("  Building Romanian Language Model");
        Console.WriteLine("=================================================\n");

        Console.WriteLine("Loading linguistic data from source files...");
        var model = RomanianLanguageModel.BuildDefault();

        var outputPath = string.IsNullOrEmpty(options.OutputFile)
            ? "Models/romanian_language.model"
            : options.OutputFile;

        Console.WriteLine($"\nSaving model to: {outputPath}");
        model.Save(outputPath);

        var fileSize = new FileInfo(outputPath).Length;
        Console.WriteLine($"\nModel file size: {fileSize:N0} bytes ({fileSize / 1024.0:F2} KB)");

        Console.WriteLine("\n=================================================");
        Console.WriteLine("  Language Model Build Complete!");
        Console.WriteLine("=================================================");
        Console.WriteLine("\nThis model will be used by all compressions to reduce metadata size.");
    }

    static void TrainBPEModel(CommandLineOptions options)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("  BPE Model Training for Romanian Text");
        Console.WriteLine("=================================================\n");

        int vocabSize = options.VocabSize > 0 ? options.VocabSize : 5000;

        Console.WriteLine($"Training corpus: {options.InputFile}");
        Console.WriteLine($"Output model: {options.OutputFile}");
        Console.WriteLine($"Vocabulary size: {vocabSize} merges\n");

        BPETrainer.TrainAndSave(options.InputFile, options.OutputFile, vocabSize);

        Console.WriteLine("\n=================================================");
        Console.WriteLine("  Training Complete!");
        Console.WriteLine("=================================================");
    }

    static void ProcessFile(CommandLineOptions options)
    {
        // Read input with FileShare.Read to avoid access conflicts
        string inputText;
        using (var stream = new FileStream(options.InputFile, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        {
            inputText = reader.ReadToEnd();
        }
        Console.WriteLine($"Read {inputText.Length} characters from: {options.InputFile}");

        // Parse stemming mode
        var stemmingMode = options.StemmingMode.ToLowerInvariant() == "fast"
            ? StemmingMode.Fast
            : StemmingMode.Accurate;

        // Enable arithmetic coding with aggressive mode (they work well together)
        var useArithmeticCoding = options.Aggressive;

        var pipeline = BuildPipeline(stemmingMode, options.Aggressive, useArithmeticCoding);

        if (options.Mode == "compress")
        {
            Console.WriteLine($"Stemming Mode: {stemmingMode}");
            if (options.Lossy)
                Console.WriteLine("Compression Mode: LOSSY (no reversibility metadata)");
            else
                Console.WriteLine("Compression Mode: Lossless (full reversibility)");
            if (options.Aggressive)
                Console.WriteLine("Aggressive Mode: ENABLED (keep only nouns/verbs + arithmetic coding)");
            Console.WriteLine("Compressing...\n");

            // Compress
            var context = pipeline.Execute(inputText, options.Lossy);

            // Save in binary format (with optional GZIP)
            var pipelineNames = ((CompressionPipeline)pipeline).GetProcessorNames();
            BinaryCompressedFormat.SaveCompressed(options.OutputFile, context, inputText, pipelineNames, options.UseGzip);

            // Get final file size
            var fileSize = new FileInfo(options.OutputFile).Length;
            var compressionRatio = (double)inputText.Length / fileSize;

            // Show report
            var report = CompressionMetrics.GenerateReport(inputText, context.Text);
            Console.WriteLine(report);

            Console.WriteLine($"\n✓ Compressed successfully!");
            Console.WriteLine($"  Output: {options.OutputFile}");
            Console.WriteLine($"  Format: Binary{(options.UseGzip ? " + GZIP" : " (no GZIP)")}");
            Console.WriteLine($"  File size: {fileSize:N0} bytes ({fileSize / 1024.0:F2} KB)");
            Console.WriteLine($"  Compression ratio: {compressionRatio:F2}x");
            Console.WriteLine($"  Space savings: {(1 - 1.0 / compressionRatio) * 100:F2}%");
        }
        else if (options.Mode == "decompress")
        {
            Console.WriteLine("Decompressing...\n");

            // Try binary format first, fallback to old text format for backward compatibility
            (string compressedText, Dictionary<string, object> metadata, string humanHeader) loadResult;

            if (BinaryCompressedFormat.IsValidFormat(options.InputFile))
            {
                Console.WriteLine("Detected: Binary+GZIP format");
                loadResult = BinaryCompressedFormat.LoadCompressed(options.InputFile);
            }
            else if (SelfContainedFormat.IsValidFormat(options.InputFile))
            {
                Console.WriteLine("Detected: Legacy text format");
                loadResult = SelfContainedFormat.LoadCompressed(options.InputFile);
            }
            else
            {
                throw new InvalidOperationException("Unknown file format - not a valid .cmp file");
            }

            var (compressedText, metadata, humanHeader) = loadResult;

            // Show header info
            Console.WriteLine("--- Compression Info ---");
            Console.WriteLine(humanHeader);
            Console.WriteLine();

            // Create context for decompression
            // Note: After arithmetic coding, compressedText might be empty (all data is in metadata)
            var context = new ProcessingContext(string.IsNullOrWhiteSpace(compressedText) ? "PLACEHOLDER" : compressedText)
            {
                Tokens = string.IsNullOrWhiteSpace(compressedText) ? new List<string>() : TextTokenizer.Tokenize(compressedText)
            };

            // Restore metadata
            foreach (var kvp in metadata)
            {
                context.SetMetadata(kvp.Key, kvp.Value);
            }

            // Decompress
            string decompressed = pipeline.Reverse(context);

            // Save decompressed text
            using (var stream = new FileStream(options.OutputFile, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.Write(decompressed);
            }

            Console.WriteLine($"✓ Decompressed successfully!");
            Console.WriteLine($"  Output: {options.OutputFile}");
            Console.WriteLine($"  Size: {decompressed.Length} characters");
        }
    }

    static CompressionPipeline BuildPipeline(StemmingMode mode = StemmingMode.Accurate, bool enableAggressiveMode = false, bool enableArithmeticCoding = false)
    {
        var processors = new List<IReversibleProcessor>
        {
            new DiacriticsNormalizer(),      // Faza 1: Preprocesare - Normalize capitalization
            new StopwordsProcessor(),         // Faza 2: Compresie Semantică - Remove ~300 stopwords
            new SynonymReplacer(),            // Faza 2: Compresie Semantică - Replace synonyms
            new RomanianLemmatizer(mode)      // Faza 2: Compresie Semantică - Stemming (165 rules)
        };

        // Aggressive mode: Remove non-essential words (keep only nouns/verbs)
        if (enableAggressiveMode)
        {
            processors.Add(new Processors.Aggressive.AggressiveCompressionProcessor());
        }

        // Phase 3: Arithmetic coding for statistical compression
        if (enableArithmeticCoding)
        {
            processors.Add(new ArithmeticCodingProcessor());   // Faza 3: Arithmetic coding (final compression)
        }

        return new CompressionPipeline(processors);
    }

    static CommandLineOptions ParseArguments(string[] args)
    {
        var options = new CommandLineOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--mode":
                case "-m":
                    options.Mode = args[++i];
                    break;

                case "--input":
                case "-i":
                    options.InputFile = args[++i];
                    break;

                case "--output":
                case "-o":
                    options.OutputFile = args[++i];
                    break;

                case "--stemming-mode":
                case "-s":
                    options.StemmingMode = args[++i];
                    break;

                case "--vocab-size":
                case "-v":
                    options.VocabSize = int.Parse(args[++i]);
                    break;

                case "--gzip":
                case "-g":
                    options.UseGzip = true;
                    break;

                case "--lossy":
                case "-l":
                    options.Lossy = true;
                    break;

                case "--aggressive":
                case "-a":
                    options.Aggressive = true;
                    break;

                case "--help":
                case "-h":
                    ShowUsage();
                    Environment.Exit(0);
                    break;

                default:
                    throw new ArgumentException($"Unknown argument: {args[i]}");
            }
        }

        // Validate
        if (string.IsNullOrEmpty(options.Mode))
            throw new ArgumentException("Mode is required (--mode compress|decompress|train-bpe|build-model)");

        // build-model doesn't require input file
        if (options.Mode != "build-model")
        {
            if (string.IsNullOrEmpty(options.InputFile))
                throw new ArgumentException("Input file is required (--input <file>)");

            if (string.IsNullOrEmpty(options.OutputFile))
                throw new ArgumentException("Output file is required (--output <file>)");

            if (!File.Exists(options.InputFile))
                throw new FileNotFoundException($"Input file not found: {options.InputFile}");
        }

        return options;
    }

    static void ShowUsage()
    {
        Console.WriteLine(@"Usage:
  Compression-Worker --mode <compress|decompress|train-bpe> --input <file> --output <file> [options]

Options:
  --mode, -m          Operation mode: 'compress', 'decompress', or 'train-bpe'
  --input, -i         Input file path
  --output, -o        Output file path
  --stemming-mode, -s Stemming mode: 'fast' or 'accurate' (default: accurate)
                      - fast: No wordlist validation (~5ms load)
                      - accurate: Validates stems with dictionary (~500ms load, better quality)
  --gzip, -g          Enable GZIP compression on output (default: disabled, pure semantic compression)
  --vocab-size, -v    BPE vocabulary size (for train-bpe mode, default: 5000)
  --help, -h          Show this help message

Examples:
  # Train BPE model on large corpus
  Compression-Worker -m train-bpe -i corpus.txt -o romanian_bpe.model -v 5000

  # Compress with accurate stemming (default, best quality)
  Compression-Worker -m compress -i input.txt -o compressed.cmp

  # Compress with fast stemming (faster, good quality)
  Compression-Worker -m compress -i input.txt -o compressed.cmp -s fast

  # Decompress (stemming mode not needed, stored in file)
  Compression-Worker -m decompress -i compressed.cmp -o output.txt

Output Format:
  - Self-contained format (.cmp recommended extension)
  - Binary metadata + human-readable header + compressed text
  - Single file, no separate .meta file needed

Pipeline:
  Faza 1 - Preprocesare:
    • Diacritics normalization (ă, â, î, ș, ț)
    • Capitalization tracking

  Faza 2 - Compresie Semantică:
    • Stopwords removal (~300 Romanian functional words)
    • Synonym replacement (consolidate synonyms)
    • Lemmatization/Stemming (165 suffix rules from file)

Interactive Demo:
  Run without arguments to see a demonstration with sample text.
");
    }

    record CommandLineOptions
    {
        public string Mode { get; set; } = "";
        public string InputFile { get; set; } = "";
        public string OutputFile { get; set; } = "";
        public string StemmingMode { get; set; } = "accurate"; // default: accurate
        public int VocabSize { get; set; } = 5000; // default: 5000 merges
        public bool UseGzip { get; set; } = false; // default: no GZIP (pure semantic compression)
        public bool Lossy { get; set; } = false; // default: lossless (store metadata for reversibility)
        public bool Aggressive { get; set; } = false; // default: normal compression (keep all words)
    }
}
