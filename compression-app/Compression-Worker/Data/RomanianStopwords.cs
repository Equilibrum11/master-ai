namespace Compression_Worker.Data;

/// <summary>
/// Static repository of Romanian stopwords (functional words with minimal semantic content).
/// Organized by grammatical categories for maintainability.
/// Based on linguistic research showing ~60% information redundancy in natural language.
/// </summary>
public static class RomanianStopwords
{
    private static readonly HashSet<string> _stopwords;

    static RomanianStopwords()
    {
        _stopwords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        LoadStopwords();
    }

    /// <summary>
    /// Returns the complete set of Romanian stopwords.
    /// </summary>
    public static HashSet<string> GetStopwords() => _stopwords;

    private static void LoadStopwords()
    {
        // Pronume personale (Personal Pronouns)
        AddCategory("eu", "tu", "el", "ea", "noi", "voi", "ei", "ele",
                   "mine", "tine", "lui", "ei", "nouă", "vouă", "lor",
                   "mie", "ție", "îmi", "îți", "își", "ne", "vă", "se");

        // Articole (Articles)
        AddCategory("un", "o", "al", "ai", "ale", "unei", "unui",
                   "celui", "celei", "celor", "lui", "ei");

        // Prepoziții (Prepositions)
        AddCategory("de", "la", "cu", "în", "din", "pe", "pentru", "către",
                   "spre", "până", "după", "înainte", "asupra", "dintr",
                   "dintre", "deasupra", "dedesubt", "înaintea", "înăuntru",
                   "afară", "lângă", "printre", "contra");

        // Conjuncții (Conjunctions)
        AddCategory("și", "sau", "dar", "că", "dacă", "când", "unde", "cum",
                   "ce", "care", "căci", "deși", "deoarece", "fiindcă",
                   "însă", "ori", "precum", "ca");

        // Determinanți și adverbe (Determiners and Adverbs)
        AddCategory("acest", "această", "acestui", "acestei", "acești",
                   "aceste", "acelor", "acel", "acea", "acela", "aceea",
                   "cel", "cea", "cei", "cele");

        // Adverbe frecvente (Frequent Adverbs)
        AddCategory("foarte", "mai", "doar", "numai", "încă", "deja",
                   "aproape", "destul", "prea", "tot", "toată", "toți",
                   "toate", "atât", "câtă", "câți", "câte", "aici",
                   "acolo", "acum", "atunci", "ieri", "mâine", "astăzi");

        // Verbe auxiliare și copulative (Auxiliary and Copulative Verbs)
        AddCategory("am", "ai", "are", "a", "avea", "avem", "aveți", "au",
                   "fost", "fi", "fiind", "fie", "fiu", "fii",
                   "sunt", "ești", "este", "suntem", "sunteți",
                   "eram", "erai", "era", "erați", "erau",
                   "voi", "va", "vom", "veți", "vor");

        // Pronume demonstrative și relative (Demonstrative and Relative Pronouns)
        AddCategory("care", "cine", "ce", "carui", "căruia", "căreia",
                   "cărora", "cui", "al", "ai", "ale");

        // Pronume posesive (Possessive Pronouns)
        AddCategory("meu", "mea", "mei", "mele", "tău", "ta", "tăi", "tale",
                   "său", "sa", "săi", "sale", "nostru", "noastră",
                   "noștri", "noastre", "vostru", "voastră", "voștri", "voastre");

        // Particule și interjecții comune (Particles and Common Interjections)
        AddCategory("nu", "nici", "nici", "n", "nimic", "nimeni", "nicăieri",
                   "niciodată", "da", "ba");

        // Numerale și cuantificatori (Numerals and Quantifiers)
        AddCategory("un", "doi", "două", "trei", "patru", "cinci",
                   "mult", "multă", "mulți", "multe", "puțin", "puțină",
                   "puțini", "puține", "câțiva", "câteva", "fiecare",
                   "toți", "toate", "amândoi", "ambele");

        // Verbe modale și semi-auxiliare frecvente (Modal and Semi-Auxiliary Verbs)
        AddCategory("pot", "poți", "poate", "putem", "puteți", "putea",
                   "vreau", "vrei", "vrea", "vrem", "vreți", "vor", "a vrea",
                   "trebuie", "trebuia");

        // Cuvinte de legătură și discourse markers
        AddCategory("adică", "altfel", "anume", "așadar", "astfel",
                   "deci", "dimpotrivă", "însă", "iată", "parcă",
                   "bine", "poftim", "hai");
    }

    /// <summary>
    /// Helper method to add multiple words to the stopwords set.
    /// Follows DRY principle.
    /// </summary>
    private static void AddCategory(params string[] words)
    {
        foreach (var word in words)
        {
            _stopwords.Add(word);
        }
    }

    /// <summary>
    /// Checks if a word is a stopword.
    /// </summary>
    public static bool IsStopword(string word)
    {
        return !string.IsNullOrWhiteSpace(word) && _stopwords.Contains(word);
    }

    /// <summary>
    /// Returns the count of stopwords in the dictionary.
    /// </summary>
    public static int Count => _stopwords.Count;
}
