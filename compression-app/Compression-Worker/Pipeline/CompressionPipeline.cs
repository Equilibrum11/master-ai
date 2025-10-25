using Compression_Worker.Core.Interfaces;
using Compression_Worker.Core.Models;
using Compression_Worker.Utils;

namespace Compression_Worker.Pipeline;

/// <summary>
/// Orchestrates a chain of reversible processors to compress and decompress text.
/// Implements Chain of Responsibility pattern for flexible, composable processing.
/// Follows Open/Closed Principle: open for extension, closed for modification.
/// </summary>
public class CompressionPipeline : ICompressionPipeline
{
    private readonly List<IReversibleProcessor> _processors;

    /// <summary>
    /// Creates a pipeline with the specified processors.
    /// Processors are executed in the order provided during compression.
    /// </summary>
    public CompressionPipeline(IEnumerable<IReversibleProcessor> processors)
    {
        _processors = processors?.ToList() ?? throw new ArgumentNullException(nameof(processors));

        if (_processors.Count == 0)
            throw new ArgumentException("Pipeline must contain at least one processor", nameof(processors));
    }

    /// <summary>
    /// Executes all processors in sequence on the input text.
    /// Each processor receives the output of the previous processor.
    /// </summary>
    public ProcessingContext Execute(string inputText, bool lossy = false)
    {
        if (string.IsNullOrWhiteSpace(inputText))
            throw new ArgumentException("Input text cannot be null or empty", nameof(inputText));

        // Initialize context with tokenized input
        var context = new ProcessingContext(inputText, lossy);
        context.Tokens = TextTokenizer.Tokenize(inputText);

        // Chain processors: output of one becomes input of next
        return _processors.Aggregate(context, (currentContext, processor) =>
        {
            try
            {
                return processor.Process(currentContext);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Processor '{processor.GetType().Name}' failed during processing",
                    ex);
            }
        });
    }

    /// <summary>
    /// Reverses all processors in reverse order to restore original text.
    /// Uses metadata stored during compression to achieve perfect reversibility.
    /// </summary>
    public string Reverse(ProcessingContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        // Reverse the pipeline: process in opposite order
        var reversedContext = _processors
            .AsEnumerable()
            .Reverse()
            .Aggregate(context, (currentContext, processor) =>
            {
                try
                {
                    return processor.Reverse(currentContext);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Processor '{processor.GetType().Name}' failed during reversal",
                        ex);
                }
            });

        // Reconstruct text from tokens
        return TextTokenizer.Detokenize(reversedContext.Tokens);
    }

    /// <summary>
    /// Gets the number of processors in the pipeline.
    /// </summary>
    public int ProcessorCount => _processors.Count;

    /// <summary>
    /// Gets the names of all processors in the pipeline.
    /// </summary>
    public IReadOnlyList<string> GetProcessorNames()
    {
        return _processors.Select(p => p.GetType().Name).ToList();
    }
}
