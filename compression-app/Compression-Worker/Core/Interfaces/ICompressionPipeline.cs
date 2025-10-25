using Compression_Worker.Core.Models;

namespace Compression_Worker.Core.Interfaces;

/// <summary>
/// Pipeline orchestrator that chains multiple processors.
/// Implements Chain of Responsibility pattern.
/// </summary>
public interface ICompressionPipeline
{
    /// <summary>
    /// Executes all processors in sequence on the input text.
    /// </summary>
    ProcessingContext Execute(string inputText, bool lossy = false);

    /// <summary>
    /// Reverses all processors in reverse order to restore original text.
    /// </summary>
    string Reverse(ProcessingContext context);
}
