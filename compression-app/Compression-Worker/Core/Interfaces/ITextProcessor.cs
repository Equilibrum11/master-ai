using Compression_Worker.Core.Models;

namespace Compression_Worker.Core.Interfaces;

/// <summary>
/// Core contract for any text processor.
/// Follows Interface Segregation Principle - minimal, focused interface.
/// </summary>
public interface ITextProcessor
{
    /// <summary>
    /// Processes the given context and returns a modified version.
    /// </summary>
    ProcessingContext Process(ProcessingContext context);
}
