# Claude Code Instructions - Romanian Text Compression Project

## Project Context

This is a compression tester project for "Advanced Methods for Data Security" course. The goal is to implement a **reversible text compression algorithm** for Romanian language based on linguistic redundancy principles (research shows ~60% of natural language is redundant).

## Architecture Overview

### Design Patterns Applied
- **Strategy Pattern**: Interchangeable processors with common interface
- **Chain of Responsibility**: Pipeline of processors executed in sequence
- **Template Method**: Base class with common logic, derived classes implement specifics
- **Dependency Injection**: Processors injected into pipeline

### SOLID Principles
- **S**ingle Responsibility: Each processor does ONE thing
- **O**pen/Closed: Easy to add new processors without modifying existing code
- **L**iskov Substitution: All processors are interchangeable via `IReversibleProcessor`
- **I**nterface Segregation: Small, focused interfaces (`ITextProcessor`, `IReversibleProcessor`)
- **D**ependency Inversion: Depend on abstractions, not concrete implementations

### Project Structure

```
Compression-Worker/
├── Core/
│   ├── Interfaces/                    # Contracts for processors and pipeline
│   │   ├── ITextProcessor.cs         # Base processor interface
│   │   ├── IReversibleProcessor.cs   # Reversible processor interface
│   │   └── ICompressionPipeline.cs   # Pipeline orchestrator interface
│   └── Models/
│       └── ProcessingContext.cs      # DTO carrying text + metadata through pipeline
├── Processors/
│   ├── Base/
│   │   └── ReversibleProcessorBase.cs  # Abstract base with common logic (DRY)
│   ├── Normalization/
│   │   └── DiacriticsNormalizer.cs     # Normalizes case + Romanian diacritics (ă,â,î,ș,ț)
│   ├── Stopwords/
│   │   └── StopwordsProcessor.cs       # Removes ~300 Romanian stopwords + whitespace
│   └── Lemmatization/
│       └── RomanianLemmatizer.cs       # Reduces to base form (Snowball algorithm)
├── Data/
│   ├── RomanianStopwords.cs          # Static list of ~300 stopwords (categorized)
│   └── SnowballRules.cs              # Romanian stemming rules
├── Pipeline/
│   └── CompressionPipeline.cs        # Chains processors, handles reversibility
├── Utils/
│   ├── TextTokenizer.cs              # Tokenizes text (words + punctuation + whitespace)
│   └── CompressionMetrics.cs         # Calculates compression ratio, entropy, etc.
└── Program.cs                         # CLI entry point + interactive demo
```

## Key Implementation Details

### 1. Reversibility Mechanism
**Every processor stores metadata in `ProcessingContext` to enable perfect reconstruction:**

```csharp
// During compression
context.SetMetadata("stopwords_removed", removedWordsDict);

// During decompression
var removedWords = context.GetMetadata<Dictionary<int, string>>("stopwords_removed");
// Restore words at original positions
```

### 2. Pipeline Execution Flow

**Compression (forward):**
1. `DiacriticsNormalizer` → lowercase + save capitalization
2. `StopwordsProcessor` → remove stopwords + whitespace, save positions
3. `RomanianLemmatizer` → stem words, save original forms

**Decompression (reverse):**
1. `RomanianLemmatizer.Reverse()` → restore inflected forms
2. `StopwordsProcessor.Reverse()` → restore stopwords at positions
3. `DiacriticsNormalizer.Reverse()` → restore capitalization

### 3. Romanian Language Specifics

**Stopwords Categories (~300 total):**
- Pronume personale: eu, tu, el, ea, mine, ție...
- Articole: un, o, al, ai, ale, celui...
- Prepoziții: de, la, cu, în, din, pe...
- Conjuncții: și, sau, dar, că, dacă...
- Verbe auxiliare: am, ai, are, sunt, fost, fi...

**Stemming Rules (Snowball simplified):**
- Plurale: -urilor, -ilor, -elor, -uri → remove
- Diminutive: -ușor, -ișor, -uleț → remove
- Verbale: -ând, -ind, -ește, -eau → remove
- Adjective: -abil, -ibil, -mente → remove

### 4. Current Performance

**Test Results (164-character Romanian text):**
- Original: 164 chars, 25 words
- Compressed: 125 chars, 17 words
- **Compression Ratio: 1.31x**
- **Space Savings: 23.78%**
- **Reversibility: 100% (lossless)**

## How to Work with This Codebase

### Adding a New Processor

**Step 1:** Create class inheriting from `ReversibleProcessorBase`

```csharp
public class MyNewProcessor : ReversibleProcessorBase
{
    private const string MetadataKey = "my_metadata";

    protected override ProcessingContext ProcessCore(ProcessingContext context)
    {
        // Transform tokens
        // Save metadata for reversibility
        context.SetMetadata(MetadataKey, myMetadata);
        return context;
    }

    protected override ProcessingContext ReverseCore(ProcessingContext context)
    {
        // Restore using metadata
        var metadata = context.GetMetadata<MyType>(MetadataKey);
        return context;
    }
}
```

**Step 2:** Add to pipeline in `Program.cs`

```csharp
var processors = new List<IReversibleProcessor>
{
    new DiacriticsNormalizer(),
    new StopwordsProcessor(),
    new RomanianLemmatizer(),
    new MyNewProcessor()  // ← Add here
};
```

**That's it!** No other code changes needed (Open/Closed Principle).

### Testing Compression

**Interactive demo:**
```bash
dotnet run
```

**Compress a file:**
```bash
dotnet run -- --mode compress --input input.txt --output compressed.txt
```

**Decompress:**
```bash
dotnet run -- --mode decompress --input compressed.txt --output restored.txt
```

## Future Extensions (Not Implemented Yet)

### Phase 2: Huffman Coding on Words
- Build frequency table of compressed words
- Assign shorter bit codes to frequent words
- Expected improvement: +20-30% compression

### Phase 3: Byte-Pair Encoding (BPE)
- Identify frequent character pairs
- Replace with single tokens
- Expected improvement: +10-15% compression

### Phase 4: LZ77 Dictionary Compression
- Find repeated sequences
- Replace with back-references
- Expected improvement: +15-25% compression

## Common Issues & Solutions

### Issue: Whitespace not preserved correctly
**Solution:** Ensure `TextTokenizer.Tokenize()` captures whitespace tokens (`\s+` in regex)

### Issue: Reversibility fails
**Solution:** Check that all processors save/restore metadata correctly. Use debugger to inspect `context.Metadata`.

### Issue: Poor compression ratio
**Solution:**
1. Verify stopwords list is comprehensive
2. Check stemming rules are applied correctly
3. Add more processors to pipeline (Huffman, BPE)

### Issue: New processor causes build errors
**Solution:** Ensure proper using statements for `Compression_Worker.Core.Models` and `Compression_Worker.Core.Interfaces`

## Testing Checklist

When making changes, verify:
- [ ] Project builds without errors: `dotnet build`
- [ ] Interactive demo runs: `dotnet run`
- [ ] Reversibility check passes (✓ PASSED)
- [ ] Compression ratio is reasonable (>1.2x)
- [ ] No exceptions thrown during compression/decompression

## Key Files to Understand First

1. **ProcessingContext.cs** - Understand how data flows through pipeline
2. **ReversibleProcessorBase.cs** - Understand template method pattern
3. **CompressionPipeline.cs** - Understand chain of responsibility
4. **Program.cs** - Understand CLI and demo flow

## Prompting Tips for Claude

**Good prompts:**
- "Add a new processor that removes duplicate consecutive words"
- "Improve the stemming rules for Romanian past tense verbs"
- "Add Huffman coding as a new processor in the pipeline"
- "Create unit tests for StopwordsProcessor"

**Bad prompts (too vague):**
- "Make it better"
- "Optimize the code"
- "Fix the compression"

**Always specify:**
- What processor/component to modify
- What behavior you want to change
- Whether you need new features or bug fixes

## Code Style Guidelines

- Use XML comments (`///`) for public APIs
- Follow C# naming conventions (PascalCase for classes/methods, camelCase for variables)
- Keep methods small (<50 lines)
- Prefer immutability (use `record` for DTOs)
- Use meaningful variable names (`removedWords` not `temp`)

## Resources

- **Snowball Stemmer (Romanian):** https://snowballstem.org/algorithms/romanian/stemmer.html
- **Shannon Entropy:** https://en.wikipedia.org/wiki/Entropy_(information_theory)
- **Huffman Coding:** https://en.wikipedia.org/wiki/Huffman_coding
- **BPE Algorithm:** https://en.wikipedia.org/wiki/Byte_pair_encoding

---

**Last Updated:** 2025-10-24
**Version:** 1.0 (Phase 1 Complete - Stopwords + Lemmatization)
