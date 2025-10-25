using Compression_Worker.Data;
using Compression_Worker.Utils;
using System.Text;
using System.Text.Json;

namespace Compression_Worker;

/// <summary>
/// Standalone BPE vocabulary trainer for Romanian text.
/// Trains on a large corpus and saves a persistent model.
/// </summary>
public class BPETrainer
{
    public static void TrainAndSave(string inputFile, string outputModelFile, int vocabSize = 5000)
    {
        Console.WriteLine($"[BPE TRAINER] Reading corpus from: {inputFile}");

        // Read the training corpus
        var text = File.ReadAllText(inputFile, Encoding.UTF8);
        Console.WriteLine($"[BPE TRAINER] Corpus size: {text.Length:N0} characters");

        // Tokenize the text
        var tokens = TextTokenizer.Tokenize(text);
        Console.WriteLine($"[BPE TRAINER] Token count: {tokens.Count:N0}");

        // Filter to only word tokens (skip punctuation)
        var wordTokens = tokens.Where(t => !string.IsNullOrWhiteSpace(t) &&
                                           t.Length > 0 &&
                                           char.IsLetter(t[0])).ToList();

        Console.WriteLine($"[BPE TRAINER] Word tokens: {wordTokens.Count:N0}");
        Console.WriteLine($"[BPE TRAINER] Training BPE with {vocabSize} merges...");
        Console.WriteLine();

        // Train BPE vocabulary
        var vocabulary = new BPEVocabulary(vocabSize);
        vocabulary.Train(wordTokens, vocabSize);

        Console.WriteLine();
        Console.WriteLine($"[BPE TRAINER] Training complete!");
        Console.WriteLine($"[BPE TRAINER] Vocabulary size: {vocabulary.GetVocabSize()}");
        Console.WriteLine($"[BPE TRAINER] Merge operations: {vocabulary.GetMergeCount()}");

        // Save the model
        Console.WriteLine($"[BPE TRAINER] Saving model to: {outputModelFile}");

        var modelData = new
        {
            vocab_size = vocabSize,
            trained_on = Path.GetFileName(inputFile),
            training_date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            corpus_size = text.Length,
            token_count = tokens.Count,
            word_count = wordTokens.Count,
            vocabulary = vocabulary.ToMetadata()
        };

        var json = JsonSerializer.Serialize(modelData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Ensure directory exists
        var directory = Path.GetDirectoryName(outputModelFile);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(outputModelFile, json, Encoding.UTF8);

        var modelSize = new FileInfo(outputModelFile).Length;
        Console.WriteLine($"[BPE TRAINER] Model saved successfully!");
        Console.WriteLine($"[BPE TRAINER] Model file size: {modelSize:N0} bytes ({modelSize / 1024.0:F2} KB)");
        Console.WriteLine();
        Console.WriteLine("Sample vocabulary entries:");

        var sampleMerges = vocabulary.GetMerges().Take(10).ToList();
        for (int i = 0; i < sampleMerges.Count; i++)
        {
            var (pair, merged) = sampleMerges[i];
            Console.WriteLine($"  {i + 1}. \"{pair.Item1}\" + \"{pair.Item2}\" → \"{merged}\"");
        }
    }
}
