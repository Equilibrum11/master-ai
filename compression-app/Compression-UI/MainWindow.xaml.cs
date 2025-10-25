using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using Compression_Worker.Core.Interfaces;
using Compression_Worker.Core.Models;
using Compression_Worker.Pipeline;
using Compression_Worker.Processors.Normalization;
using Compression_Worker.Processors.Stopwords;
using Compression_Worker.Processors.Semantic;
using Compression_Worker.Processors.Lemmatization;
using Compression_Worker.Processors.Statistical;
using Compression_Worker.Processors.Aggressive;
using Compression_Worker.Data;
using Compression_Worker.Utils;

namespace Compression_UI;

/// <summary>
/// Romanian Text Compression Visual Tester
/// Provides interactive UI for testing compression/decompression functionality
/// </summary>
public partial class MainWindow : Window
{
    private ProcessingContext? _lastCompressionContext;
    private string _originalText = string.Empty;
    private StringBuilder _compressionLogs = new StringBuilder();
    private SettingsWindow.PipelineSettings _pipelineSettings = new SettingsWindow.PipelineSettings();

    public MainWindow()
    {
        InitializeComponent();
        InputTextBox.TextChanged += (s, e) => UpdateInputStats();
        OutputTextBox.TextChanged += (s, e) => UpdateOutputStats();
        UpdateInputStats();
        UpdateSettingsSummary();
    }

    private void UpdateInputStats()
    {
        var text = InputTextBox.Text;
        InputCharCount.Text = text.Length.ToString();
        InputWordCount.Text = CountWords(text).ToString();
    }

    private void UpdateOutputStats()
    {
        var text = OutputTextBox.Text;
        OutputCharCount.Text = text.Length.ToString();
        OutputWordCount.Text = CountWords(text).ToString();
    }

    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private void LoadFileButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "All Supported Files (*.txt;*.cmp)|*.txt;*.cmp|Text Files (*.txt)|*.txt|Compressed Files (*.cmp)|*.cmp|All Files (*.*)|*.*",
            Title = "Load File"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var extension = Path.GetExtension(dialog.FileName).ToLower();

                if (extension == ".cmp")
                {
                    // Load binary compressed file
                    var fileInfo = new FileInfo(dialog.FileName);
                    var (compressedText, metadata, humanHeader) = BinaryCompressedFormat.LoadCompressed(dialog.FileName);

                    // Create a ProcessingContext from the loaded data
                    var context = new ProcessingContext(compressedText, lossy: false);
                    context.Tokens = TextTokenizer.Tokenize(compressedText);

                    // Restore metadata
                    foreach (var kvp in metadata)
                    {
                        context.SetMetadata(kvp.Key, kvp.Value);
                    }

                    // Show compressed text
                    InputTextBox.Text = compressedText;
                    _lastCompressionContext = context;

                    UpdateStatus($"Loaded compressed file: {Path.GetFileName(dialog.FileName)} ({FormatSize(fileInfo.Length)})", "#3498DB");
                    MessageBox.Show(
                        $"Compressed file loaded successfully!\n\n" +
                        $"{humanHeader}\n\n" +
                        $"File size: {FormatSize(fileInfo.Length)}\n" +
                        $"Compressed text: {compressedText.Length} chars\n\n" +
                        $"Click 'DECOMPRESS' to restore original text.",
                        "File Loaded",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    // Load plain text file
                    using var stream = new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    InputTextBox.Text = reader.ReadToEnd();

                    UpdateStatus($"Loaded text file: {Path.GetFileName(dialog.FileName)}", "#27AE60");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ClearInputButton_Click(object sender, RoutedEventArgs e)
    {
        InputTextBox.Clear();
        UpdateStatus("Input cleared", "#95A5A6");
    }

    private void CompressButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(InputTextBox.Text))
        {
            MessageBox.Show("Please enter or load Romanian text to compress.", "No Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            UpdateStatus("Compressing...", "#F39C12");
            CompressButton.IsEnabled = false;

            // Clear previous steps and logs
            StepsPanel.Children.Clear();
            _compressionLogs.Clear();

            // Store original text
            _originalText = InputTextBox.Text;

            // Log header
            _compressionLogs.AppendLine("=".PadRight(80, '='));
            _compressionLogs.AppendLine($"COMPRESSION SESSION - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _compressionLogs.AppendLine("=".PadRight(80, '='));
            _compressionLogs.AppendLine();
            _compressionLogs.AppendLine("PIPELINE CONFIGURATION:");
            _compressionLogs.AppendLine($"  [{(_pipelineSettings.EnableDiacritics ? "X" : " ")}] Diacritics Normalization");
            _compressionLogs.AppendLine($"  [{(_pipelineSettings.EnableStopwords ? "X" : " ")}] Stopwords Removal (~218)");
            _compressionLogs.AppendLine($"  [{(_pipelineSettings.EnableSynonyms ? "X" : " ")}] Synonym Replacement (21)");
            string reductionMethod = _pipelineSettings.ReductionMethod switch
            {
                SettingsWindow.WordReductionMethod.None => "None",
                SettingsWindow.WordReductionMethod.Stemming => "Stemming (rule-based)",
                SettingsWindow.WordReductionMethod.Lemmatization => "Lemmatization (dictionary, 165 rules)",
                _ => "Unknown"
            };
            _compressionLogs.AppendLine($"  Word Reduction: {reductionMethod}");
            _compressionLogs.AppendLine($"  [{(_pipelineSettings.EnableAggressive ? "X" : " ")}] Aggressive (nouns/verbs only)");
            _compressionLogs.AppendLine($"  [{(_pipelineSettings.EnableArithmeticCoding ? "X" : " ")}] Arithmetic Coding (statistical compression)");
            _compressionLogs.AppendLine($"  Reversibility: {(_pipelineSettings.UseLossyMode ? "LOSSY (No)" : "LOSSLESS (Yes)")}");
            _compressionLogs.AppendLine();

            // Determine modes
            bool lossyMode = _pipelineSettings.UseLossyMode;

            // Build processors list based on pipeline settings
            var processors = new List<IReversibleProcessor>();

            // Phase 1: Preprocessing
            if (_pipelineSettings.EnableDiacritics)
            {
                processors.Add(new DiacriticsNormalizer());
            }

            // Phase 2: Semantic Compression
            if (_pipelineSettings.EnableStopwords)
            {
                processors.Add(new StopwordsProcessor());
            }

            if (_pipelineSettings.EnableSynonyms)
            {
                processors.Add(new SynonymReplacer());
            }

            // Word Reduction: Stemming or Lemmatization
            if (_pipelineSettings.ReductionMethod == SettingsWindow.WordReductionMethod.Stemming)
            {
                processors.Add(new RomanianLemmatizer(StemmingMode.Fast)); // Stemming = Fast mode
            }
            else if (_pipelineSettings.ReductionMethod == SettingsWindow.WordReductionMethod.Lemmatization)
            {
                processors.Add(new RomanianLemmatizer(StemmingMode.Accurate)); // Lemmatization = Accurate mode
            }
            // If None, don't add any word reduction processor

            // Advanced: Aggressive compression
            if (_pipelineSettings.EnableAggressive)
            {
                processors.Add(new AggressiveCompressionProcessor());
            }

            // Phase 3: Statistical Compression
            if (_pipelineSettings.EnableArithmeticCoding)
            {
                processors.Add(new ArithmeticCodingProcessor());
            }

            // Validate that at least one processor is enabled
            if (processors.Count == 0)
            {
                MessageBox.Show("Please enable at least one processor from the Pipeline Processors panel.",
                    "No Processors Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Execute each processor and capture intermediate results
            var context = new ProcessingContext(_originalText, lossyMode);
            context.Tokens = TextTokenizer.Tokenize(_originalText);

            AddProcessingStep("0. Original Input", _originalText, _originalText.Length, CountWords(_originalText));

            int stepNumber = 1;
            foreach (var processor in processors)
            {
                context = processor.Process(context);
                var stepText = context.Text;
                var processorName = processor.GetType().Name;

                // For Arithmetic Coding, use token count instead of word count
                int wordCount;
                if (processorName == "ArithmeticCodingProcessor")
                {
                    wordCount = context.Tokens.Count; // Will be 0 after encoding (tokens cleared)
                    // Extract token count from metadata instead
                    var arithmeticData = context.GetMetadata<Dictionary<string, object>>("arithmetic_data");
                    if (arithmeticData != null && arithmeticData.TryGetValue("original_length", out var lengthObj))
                    {
                        wordCount = Convert.ToInt32(lengthObj);
                    }
                }
                else
                {
                    wordCount = CountWords(stepText);
                }

                AddProcessingStep(
                    $"{stepNumber}. {FormatProcessorName(processorName)}",
                    stepText,
                    stepText.Length,
                    wordCount);

                stepNumber++;
            }

            // Store context for decompression
            _lastCompressionContext = context;
            var compressedText = context.Text;

            // Calculate binary size by saving to a temporary memory stream
            long binarySize;
            var tempFile = Path.GetTempFileName();
            try
            {
                BinaryCompressedFormat.SaveCompressed(tempFile, context, _originalText, useGzip: false);
                binarySize = new FileInfo(tempFile).Length;
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }

            // Add binary encoding step
            AddProcessingStep(
                $"{stepNumber}. Binary Encoding (VLE + Bit-Packing)",
                $"[Binary data: {binarySize} bytes]\nVocabulary-based word encoding with variable-length indices\nBit-packed whitespace (2 bits per entry)\nOptimized metadata storage",
                (int)binarySize,
                CountWords(compressedText));

            // Display results
            OutputTextBox.Text = compressedText;

            // Calculate and display statistics
            var originalLength = _originalText.Length;
            var compressedLength = compressedText.Length;
            var originalWords = CountWords(_originalText);
            var compressedWords = CountWords(compressedText);

            var textRatio = originalLength > 0 ? (double)originalLength / compressedLength : 0;
            var binaryRatio = originalLength > 0 ? (double)originalLength / binarySize : 0;
            var savings = originalLength > 0 ? (1 - (double)binarySize / originalLength) * 100 : 0;
            var wordsRemoved = originalWords - compressedWords;

            // Display ratios with better formatting
            CompressionRatio.Text = $"{textRatio:F2}x text → {binaryRatio:F2}x binary";

            if (savings >= 0)
            {
                SpaceSavings.Text = $"{savings:F2}% saved";
                SpaceSavings.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
            }
            else
            {
                SpaceSavings.Text = $"{Math.Abs(savings):F2}% overhead";
                SpaceSavings.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
            }

            WordsRemoved.Text = $"{wordsRemoved} words";

            BinarySize.Text = $"{FormatSize(binarySize)} (from {FormatSize(originalLength)})";
            BinaryStatsText.Visibility = Visibility.Visible;

            var statusMsg = lossyMode ? "LOSSY" : "LOSSLESS";
            if (_pipelineSettings.EnableAggressive) statusMsg += " + AGGRESSIVE";

            // Add final statistics to logs
            _compressionLogs.AppendLine("=".PadRight(80, '='));
            _compressionLogs.AppendLine("FINAL COMPRESSION STATISTICS");
            _compressionLogs.AppendLine("=".PadRight(80, '='));
            _compressionLogs.AppendLine($"Mode: {statusMsg}");
            _compressionLogs.AppendLine($"Word Reduction: {reductionMethod}");
            _compressionLogs.AppendLine();
            _compressionLogs.AppendLine($"Original Size:     {originalLength:N0} chars, {originalWords:N0} words");
            _compressionLogs.AppendLine($"Compressed Text:   {compressedLength:N0} chars, {compressedWords:N0} words");
            _compressionLogs.AppendLine($"Binary File Size:  {binarySize:N0} bytes ({FormatSize(binarySize)})");
            _compressionLogs.AppendLine();
            _compressionLogs.AppendLine($"Text Compression Ratio:   {textRatio:F2}x (semantic only)");
            _compressionLogs.AppendLine($"Binary Compression Ratio: {binaryRatio:F2}x (semantic + binary encoding)");

            if (savings >= 0)
                _compressionLogs.AppendLine($"Space Savings:            {savings:F2}% (saved {originalLength - binarySize:N0} bytes)");
            else
                _compressionLogs.AppendLine($"Space Overhead:           {Math.Abs(savings):F2}% (+{binarySize - originalLength:N0} bytes metadata)");

            _compressionLogs.AppendLine($"Words Removed:            {wordsRemoved:N0}");
            _compressionLogs.AppendLine();

            if (binarySize > originalLength)
            {
                _compressionLogs.AppendLine("NOTE: Binary file is larger than original due to metadata overhead.");
                _compressionLogs.AppendLine("      This is normal for small files. Compression improves with larger files (>1KB).");
                _compressionLogs.AppendLine();
            }

            _compressionLogs.AppendLine($"Completed at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _compressionLogs.AppendLine("=".PadRight(80, '='));

            UpdateStatus($"{statusMsg} Compression complete! Binary ratio: {binaryRatio:F2}x, Text ratio: {textRatio:F2}x", "#27AE60");
            ShowStatusBadge($"COMPRESSED ({statusMsg})", "#27AE60");

            // Auto-expand steps panel
            StepByStepExpander.IsExpanded = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during compression: {ex.Message}\n\n{ex.StackTrace}", "Compression Error", MessageBoxButton.OK, MessageBoxImage.Error);
            UpdateStatus("Compression failed", "#E74C3C");
        }
        finally
        {
            CompressButton.IsEnabled = true;
        }
    }

    private void DecompressButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastCompressionContext == null)
        {
            MessageBox.Show("Please compress text first before decompressing.", "No Compression Data", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Check if lossy mode was used
        if (_lastCompressionContext.Lossy)
        {
            MessageBox.Show("Cannot decompress: This text was compressed in LOSSY mode.\nReversibility is not possible in lossy mode.",
                "Lossy Compression", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            UpdateStatus("Decompressing...", "#F39C12");
            DecompressButton.IsEnabled = false;

            // Build pipeline (same as compression)
            var pipeline = BuildPipeline();

            // Execute decompression
            var decompressed = pipeline.Reverse(_lastCompressionContext);

            // Display results
            OutputTextBox.Text = decompressed;

            // Check reversibility
            bool isReversible = string.Equals(_originalText, decompressed, StringComparison.Ordinal);

            if (isReversible)
            {
                UpdateStatus("Decompression complete! ✓ Perfect reversibility achieved!", "#27AE60");
                ShowStatusBadge("DECOMPRESSED ✓", "#27AE60");
            }
            else
            {
                UpdateStatus("Decompression complete, but reversibility check failed", "#E74C3C");
                ShowStatusBadge("DECOMPRESSED ✗", "#E74C3C");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during decompression: {ex.Message}\n\n{ex.StackTrace}", "Decompression Error", MessageBoxButton.OK, MessageBoxImage.Error);
            UpdateStatus("Decompression failed", "#E74C3C");
        }
        finally
        {
            DecompressButton.IsEnabled = true;
        }
    }

    private void VerifyReversibilityButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_originalText))
        {
            MessageBox.Show("Please compress and decompress text first.", "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var decompressed = OutputTextBox.Text;
        bool isReversible = string.Equals(_originalText, decompressed, StringComparison.Ordinal);

        if (isReversible)
        {
            MessageBox.Show(
                "✓ REVERSIBILITY TEST PASSED!\n\n" +
                "The decompressed text matches the original text exactly.\n" +
                "100% reversibility achieved!",
                "Reversibility Check",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            UpdateStatus("✓ Reversibility verified - 100% match!", "#27AE60");
        }
        else
        {
            var result = MessageBox.Show(
                "✗ REVERSIBILITY TEST FAILED\n\n" +
                "The decompressed text does not match the original.\n" +
                $"Original: {_originalText.Length} chars\n" +
                $"Decompressed: {decompressed.Length} chars\n\n" +
                "Show detailed comparison?",
                "Reversibility Check",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                ShowDifferences(_originalText, decompressed);
            }
            UpdateStatus("✗ Reversibility check failed", "#E74C3C");
        }
    }

    private void ShowDifferences(string original, string decompressed)
    {
        var minLength = Math.Min(original.Length, decompressed.Length);
        var firstDiff = -1;

        for (int i = 0; i < minLength; i++)
        {
            if (original[i] != decompressed[i])
            {
                firstDiff = i;
                break;
            }
        }

        var message = new StringBuilder();
        message.AppendLine("First difference found:");
        message.AppendLine($"Position: {firstDiff}");

        if (firstDiff >= 0)
        {
            var start = Math.Max(0, firstDiff - 20);
            var length = Math.Min(40, minLength - start);
            message.AppendLine($"\nOriginal context: ...{original.Substring(start, length)}...");
            message.AppendLine($"Decompressed context: ...{decompressed.Substring(start, length)}...");
        }

        MessageBox.Show(message.ToString(), "Difference Details", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void CopyOutputButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(OutputTextBox.Text))
        {
            Clipboard.SetText(OutputTextBox.Text);
            UpdateStatus("Output copied to clipboard", "#3498DB");
        }
    }

    private void SaveFileButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastCompressionContext == null)
        {
            MessageBox.Show("Please compress text first.", "No Compression Data", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "Compressed Binary Files (*.cmp)|*.cmp|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            DefaultExt = ".cmp",
            Title = "Save Compressed File"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var extension = Path.GetExtension(dialog.FileName).ToLower();

                if (extension == ".cmp")
                {
                    // Save as binary compressed format
                    BinaryCompressedFormat.SaveCompressed(dialog.FileName, _lastCompressionContext, _originalText, useGzip: false);
                    var fileInfo = new FileInfo(dialog.FileName);
                    UpdateStatus($"Saved binary compressed file: {Path.GetFileName(dialog.FileName)} ({FormatSize(fileInfo.Length)})", "#27AE60");
                }
                else
                {
                    // Save as plain text
                    using var stream = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None);
                    using var writer = new StreamWriter(stream, Encoding.UTF8);
                    writer.Write(OutputTextBox.Text);
                    UpdateStatus($"Saved text file: {Path.GetFileName(dialog.FileName)}", "#27AE60");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private CompressionPipeline BuildPipeline()
    {
        var processors = new List<IReversibleProcessor>();

        // Build pipeline based on settings (for decompression, use same order as compression)
        if (_pipelineSettings.EnableDiacritics)
        {
            processors.Add(new DiacriticsNormalizer());
        }

        if (_pipelineSettings.EnableStopwords)
        {
            processors.Add(new StopwordsProcessor());
        }

        if (_pipelineSettings.EnableSynonyms)
        {
            processors.Add(new SynonymReplacer());
        }

        // Word Reduction: Stemming or Lemmatization
        if (_pipelineSettings.ReductionMethod == SettingsWindow.WordReductionMethod.Stemming)
        {
            processors.Add(new RomanianLemmatizer(StemmingMode.Fast)); // Stemming = Fast mode
        }
        else if (_pipelineSettings.ReductionMethod == SettingsWindow.WordReductionMethod.Lemmatization)
        {
            processors.Add(new RomanianLemmatizer(StemmingMode.Accurate)); // Lemmatization = Accurate mode
        }
        // If None, don't add any word reduction processor

        if (_pipelineSettings.EnableAggressive)
        {
            processors.Add(new AggressiveCompressionProcessor());
        }

        if (_pipelineSettings.EnableArithmeticCoding)
        {
            processors.Add(new ArithmeticCodingProcessor());
        }

        return new CompressionPipeline(processors);
    }

    private string FormatSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        else if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F2} KB";
        else
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }

    private void UpdateStatus(string message, string colorHex)
    {
        StatusBarText.Text = message;
        StatusBarText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
    }

    private void ShowStatusBadge(string text, string colorHex)
    {
        StatusText.Text = text;
        StatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
        StatusBadge.Visibility = Visibility.Visible;
    }

    private void AddProcessingStep(string stepName, string text, int chars, int words)
    {
        // Log to compression logs
        _compressionLogs.AppendLine($"{stepName}");
        _compressionLogs.AppendLine($"Characters: {chars:N0} | Words: {words:N0}");

        // For Arithmetic Coding, show full output (includes hex/base64)
        if (stepName.Contains("Arithmetic"))
        {
            _compressionLogs.AppendLine(text);
        }
        else
        {
            _compressionLogs.AppendLine($"Preview: {(text.Length > 200 ? text.Substring(0, 200) + "..." : text)}");
        }
        _compressionLogs.AppendLine();

        var border = new System.Windows.Controls.Border
        {
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 5, 0, 5),
            Padding = new Thickness(10),
            CornerRadius = new System.Windows.CornerRadius(3),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F9F9F9"))
        };

        var stackPanel = new System.Windows.Controls.StackPanel();

        // Step header
        var header = new System.Windows.Controls.TextBlock
        {
            Text = stepName,
            FontWeight = FontWeights.Bold,
            FontSize = 13,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50")),
            Margin = new Thickness(0, 0, 0, 5)
        };

        // Stats
        var stats = new System.Windows.Controls.TextBlock
        {
            FontSize = 11,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F8C8D")),
            Margin = new Thickness(0, 0, 0, 5)
        };
        stats.Inlines.Add(new System.Windows.Documents.Run($"Characters: {chars} | Words: {words}"));

        // Text preview (first 150 chars)
        var preview = new System.Windows.Controls.TextBox
        {
            Text = text.Length > 150 ? text.Substring(0, 150) + "..." : text,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11,
            Background = Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(5)
        };

        stackPanel.Children.Add(header);
        stackPanel.Children.Add(stats);
        stackPanel.Children.Add(preview);

        border.Child = stackPanel;
        StepsPanel.Children.Add(border);
    }

    private string FormatProcessorName(string processorName)
    {
        // Make processor names more readable
        return processorName switch
        {
            "DiacriticsNormalizer" => "Diacritics Normalization",
            "StopwordsProcessor" => "Stopwords Removal (~218 words)",
            "SynonymReplacer" => "Synonym Replacement (21 groups)",
            "RomanianLemmatizer" => "Lemmatization/Stemming (165 rules)",
            "AggressiveCompressionProcessor" => "Aggressive Compression (keep only nouns/verbs)",
            "BPEProcessor" => "BPE Subword Tokenization",
            "NGramProcessor" => "N-gram Context Modeling",
            "ArithmeticCodingProcessor" => "Arithmetic Coding",
            _ => processorName
        };
    }

    private void ViewLogsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_compressionLogs.Length == 0)
        {
            MessageBox.Show("No compression logs available. Please compress some text first.", "No Logs", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var logsWindow = new LogsWindow(_compressionLogs.ToString())
        {
            Owner = this
        };
        logsWindow.ShowDialog();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(_pipelineSettings)
        {
            Owner = this
        };
        settingsWindow.ShowDialog();

        if (settingsWindow.SettingsSaved)
        {
            _pipelineSettings = settingsWindow.Settings;
            UpdateSettingsSummary();
        }
    }

    private void UpdateSettingsSummary()
    {
        var enabledProcessors = new List<string>();

        if (_pipelineSettings.EnableDiacritics) enabledProcessors.Add("Diacritics");
        if (_pipelineSettings.EnableStopwords) enabledProcessors.Add("Stopwords");
        if (_pipelineSettings.EnableSynonyms) enabledProcessors.Add("Synonyms");

        // Add word reduction method if enabled
        if (_pipelineSettings.ReductionMethod == SettingsWindow.WordReductionMethod.Stemming)
            enabledProcessors.Add("Stemming");
        else if (_pipelineSettings.ReductionMethod == SettingsWindow.WordReductionMethod.Lemmatization)
            enabledProcessors.Add("Lemmatization");

        if (_pipelineSettings.EnableAggressive) enabledProcessors.Add("Aggressive");
        if (_pipelineSettings.EnableArithmeticCoding) enabledProcessors.Add("Arithmetic");

        if (enabledProcessors.Count == 0)
        {
            SettingsSummary.Text = "⚠️ No processors enabled!";
            SettingsSummary.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
        }
        else
        {
            var reductionMethod = _pipelineSettings.ReductionMethod switch
            {
                SettingsWindow.WordReductionMethod.None => "None",
                SettingsWindow.WordReductionMethod.Stemming => "Stemming",
                SettingsWindow.WordReductionMethod.Lemmatization => "Lemmatization",
                _ => "None"
            };
            var reversibility = _pipelineSettings.UseLossyMode ? "Lossy" : "Lossless";
            SettingsSummary.Text = $"{enabledProcessors.Count} processors: {string.Join(", ", enabledProcessors)}\nWord Reduction: {reductionMethod} | {reversibility}";
            SettingsSummary.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F8C8D"));
        }
    }
}