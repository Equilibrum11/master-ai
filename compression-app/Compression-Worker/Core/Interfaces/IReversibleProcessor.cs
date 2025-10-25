using Compression_Worker.Core.Models;

namespace Compression_Worker.Core.Interfaces;

/// <summary>
/// Contract for processors that support reversible operations.
/// Essential for lossless compression/decompression.
/// </summary>
public interface IReversibleProcessor : ITextProcessor
{
    /// <summary>
    /// Reverses the processing using metadata stored in context.
    /// </summary>
    ProcessingContext Reverse(ProcessingContext context);
}
