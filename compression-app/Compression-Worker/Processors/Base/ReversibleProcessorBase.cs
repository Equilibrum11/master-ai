using Compression_Worker.Core.Interfaces;
using Compression_Worker.Core.Models;

namespace Compression_Worker.Processors.Base;

/// <summary>
/// Abstract base class implementing Template Method pattern.
/// Provides common validation and metadata handling logic (DRY principle).
/// Derived classes only implement core processing logic (Single Responsibility).
/// </summary>
public abstract class ReversibleProcessorBase : IReversibleProcessor
{
    /// <summary>
    /// Template method for processing. Ensures validation and metadata handling.
    /// </summary>
    public ProcessingContext Process(ProcessingContext context)
    {
        ValidateContext(context);
        var processed = ProcessCore(context);
        SaveMetadata(processed);
        return processed;
    }

    /// <summary>
    /// Template method for reversing. Ensures validation before reversal.
    /// </summary>
    public ProcessingContext Reverse(ProcessingContext context)
    {
        ValidateContext(context);
        return ReverseCore(context);
    }

    /// <summary>
    /// Core processing logic to be implemented by derived classes.
    /// </summary>
    protected abstract ProcessingContext ProcessCore(ProcessingContext context);

    /// <summary>
    /// Core reversal logic to be implemented by derived classes.
    /// </summary>
    protected abstract ProcessingContext ReverseCore(ProcessingContext context);

    /// <summary>
    /// Validates the processing context. Can be overridden for custom validation.
    /// </summary>
    protected virtual void ValidateContext(ProcessingContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (context.Tokens == null)
            throw new InvalidOperationException("Context tokens cannot be null");
    }

    /// <summary>
    /// Hook for derived classes to save additional metadata after processing.
    /// Default implementation does nothing.
    /// </summary>
    protected virtual void SaveMetadata(ProcessingContext context)
    {
        // Default: no additional metadata to save
    }

    /// <summary>
    /// Helper method to get the processor name for metadata keys.
    /// Ensures unique metadata keys per processor.
    /// </summary>
    protected string GetMetadataKey(string suffix)
    {
        return $"{GetType().Name}_{suffix}";
    }
}
