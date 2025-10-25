using Compression_Worker.Core.Models;
using Compression_Worker.Data;
using Compression_Worker.Processors.Base;

namespace Compression_Worker.Processors.Statistical;

/// <summary>
/// N-gram language model processor for context-based probability estimation.
/// Phase 3: Statistical Compression - Step 2
///
/// Builds a trigram model from tokens to estimate probability distributions.
/// These probabilities are used by ArithmeticCodingProcessor for optimal compression.
///
/// Reversible: 100% - doesn't modify tokens, only adds probability metadata.
/// </summary>
public class NGramProcessor : ReversibleProcessorBase
{
    private const string MetadataKey = "ngram_model";
    private const int NGramOrder = 3; // Trigram

    protected override ProcessingContext ProcessCore(ProcessingContext context)
    {
        var tokens = context.Tokens;

        // Step 1: Train n-gram model on current tokens
        var model = new NGramModel(NGramOrder);
        model.Train(tokens);

        // Step 2: Store model in metadata for use by ArithmeticCodingProcessor
        // No changes to tokens - this processor only adds statistical information
        context.SetMetadata(MetadataKey, model.ToMetadata());

        return context;
    }

    protected override ProcessingContext ReverseCore(ProcessingContext context)
    {
        // N-gram processor doesn't modify tokens during compression,
        // so there's nothing to reverse. The metadata is used by
        // ArithmeticCodingProcessor for decompression.

        // We keep the metadata in case it's needed for debugging/analysis
        return context;
    }
}
