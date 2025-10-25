Algoritmi de Compresie Text pentru Limba Română - Proiect Master
Problema pe care profesorul a adus-o în discuție se bazează pe conceptul de redundanță informațională din limba engleză, unde cercetările arată că aproximativ 50-60% din informație este redundantă și poate fi eliminată fără pierderea mesajului esențial. Pentru limba română, vei putea dezvolta un algoritm de compresie text bazat pe principii similare din teoria informației.​

Concepte Fundamentale
Redundanța Lingvistică și Entropia Shannon

Limba română, ca orice limbă naturală, prezintă redundanță semnificativă. Entropia Shannon pentru limba engleză este de aproximativ 1,0-1,5 biți pe literă, comparativ cu 8 biți folosiți în codificarea ASCII standard, ceea ce sugerează că există potențial teoretic pentru o compresie de până la 8 ori. Această redundanță există din mai multe motive: ajută la comunicarea robustă în medii zgomotoase, facilitează învățarea limbii și permite detectarea erorilor.​

Legea lui Zipf demonstrează că cuvintele frecvente sunt mai scurte, iar cuvintele rare sunt mai lungi - un principiu de optimizare natural al limbajului. Acest principiu poate fi exploatat pentru compresie eficientă.​

Algoritmi Recomandați pentru Limba Română
1. Eliminarea Stopwords și Preprocesare

Primul nivel de compresie poate include eliminarea cuvintelor funcționale care nu conțin informație semantică esențială. Pentru limba română există liste extinse de stopwords disponibile:​

Pronume personale (eu, tu, el, ea)

Articole (un, o, al, ai)

Prepoziții (de, la, cu, în)

Conjuncții (și, sau, dar, dacă)

Limba română are caracteristici morfologice specifice care necesită atenție specială:​

5 caractere diacritice (ă, â, î, ș, ț)

Sistem de genuri și cazuri complex

Flexiuni verbale bogate

2. Lemmatizare și Stemming

Reducerea cuvintelor la forma lor de bază elimină redundanța morfologică. Pentru română există algoritmi specializați:​

Stemming Snowball pentru română - elimină sufixele flexionale​

Lemmatizare - reduce cuvintele la forma lor de dicționar (lemma)​

Exemplu: "mergeau", "mergând", "mersese" → "merge" (lemma)

3. Codificare Huffman

Algoritmul Huffman atribuie coduri mai scurte caracterelor sau cuvintelor frecvente. Pentru text românesc:​

Caractere frecvente (e, a, i, t, r) primesc coduri scurte

Caractere rare primesc coduri lungi

Compresia poate atinge 50-60% pentru texte românești​

4. Byte-Pair Encoding (BPE)

BPE este un algoritm modern folosit în procesarea limbajului natural și Large Language Models:​

Identifică perechile de caractere cel mai frecvente

Înlocuiește perechile cu token-uri noi

Procesul iterativ continuă până la vocabularul dorit

Eficient pentru morfologia bogată a limbii române

Exemplu:

text
Text: "aaabdaaabac"
Iterația 1: "aa" → "Z" → "ZabdZabac"
Iterația 2: "ab" → "Y" → "ZYdZYac"
5. Algoritmi Lempel-Ziv (LZ77/LZ78)

Acești algoritmi înlocuiesc secvențe repetate cu referințe la apariții anterioare:​

LZ77 - menține o fereastră glisantă și codifică match-uri

LZ78 - construiește un dicționar dinamic

Baza pentru formate moderne (ZIP, PNG, DEFLATE)

6. Prediction by Partial Matching (PPM)

PPM este unul dintre cele mai eficiente algoritmi pentru compresie text:​

Folosește modelarea contextului pentru predicție

Prezice următorul simbol bazat pe simbolurile anterioare

PPM(n) - n reprezintă numărul de simboluri anterioare considerate

Oferă cele mai bune rate de compresie pentru text​

Exemplu comparativ pe text englezesc:​

ZIP: 490,883 bytes

7-Zip: 386,768 bytes

BZ2: 349,584 bytes

PPM: 323,934 bytes (cel mai eficient)

7. Codificare Aritmetică

Codificarea aritmetică reprezintă starea artei în compresie:​

Codifică întregul mesaj ca un singur număr între 0 și 1

Superior Huffman în majoritatea cazurilor

Simboluri frecvente primesc intervale mai mari

Permite separarea clară între model și codificare

Arhitectura Propusă pentru Proiectul Tău
Pipeline de Compresie pentru Text Românesc:

Faza 1: Preprocesare

Normalizarea diacriticelor (conversie ș/ț uniformă)​

Tokenizare (separare cuvinte, propoziții)

Lowercase (păstrare mapping pentru reconstrucție)

Faza 2: Compresie Semantică

Eliminare stopwords (listă optimizată pentru română)​

Lemmatizare sau stemming​

Înlocuirea sinonimelor cu cuvânt reprezentativ

Faza 3: Compresie Statistică

Aplicare BPE pentru vocabular subword​

Construcție model n-gram pentru predicție context​

Codificare Huffman sau Aritmetică​

Faza 4: Compresie Finală

LZ77/LZ78 pentru pattern-uri repetitive​

Sau PPM pentru compresie maximă​

Implementare Practică
Pseudo-cod pentru algoritm simplu:

python
# Faza 1: Preprocesare
def preprocess(text):
    text = normalize_diacritics(text)
    tokens = tokenize(text)
    return tokens

# Faza 2: Eliminare redundanță
def remove_redundancy(tokens):
    stopwords = load_romanian_stopwords()
    filtered = [t for t in tokens if t not in stopwords]
    lemmatized = [lemmatize(t) for t in filtered]
    return lemmatized

# Faza 3: Codificare Huffman
def huffman_compress(tokens):
    freq_table = calculate_frequencies(tokens)
    huffman_tree = build_huffman_tree(freq_table)
    codes = generate_codes(huffman_tree)
    compressed = encode_with_codes(tokens, codes)
    return compressed, huffman_tree

# Pipeline complet
def compress_romanian_text(text):
    tokens = preprocess(text)
    reduced_tokens = remove_redundancy(tokens)
    compressed_data, tree = huffman_compress(reduced_tokens)
    return compressed_data, tree
Metrici de Evaluare
Pentru a evalua algoritmul tău, folosește următoarele metrici:

1. Rata de Compresie
Compression Ratio
=
Dimensiune Original
a
˘
Dimensiune Comprimat
a
˘
Compression Ratio= 
Dimensiune Comprimat 
a
˘
 
Dimensiune Original 
a
˘
 
 

2. Economie Spațială
Space Savings
=
(
1
−
Dimensiune Comprimat
a
˘
Dimensiune Original
a
˘
)
×
100
%
Space Savings=(1− 
Dimensiune Original 
a
˘
 
Dimensiune Comprimat 
a
˘
 
 )×100%

3. Entropia Shannon​
H
(
X
)
=
−
∑
i
=
1
n
p
(
x
i
)
log
⁡
2
p
(
x
i
)
H(X)=−∑ 
i=1
n
 p(x 
i
 )log 
2
 p(x 
i
 )

4. Acuratețea Decompresiei

Pentru compresie lossy: măsoară similaritatea semantică

Pentru compresie lossless: verifică identitatea perfectă

Exemple de Corpora pentru Testare
Pentru limba română, poți folosi:

Wikipedia română - text formal, diverse domenii​

RoTex corpus - texte literare​

Oscar corpus - text web diversificat​

Texte juridice - Cod Penal, legislație​

Considerații Specifice pentru Română
Avantaje:

Morfologie bogată oferă oportunități pentru lemmatizare​

Diacriticele pot fi restaurate automat după decompresie​

Vocabularul latin facilitează pattern-uri predictibile

Provocări:

5 caractere diacritice necesită atenție specială​

Flexiuni complexe necesită algoritmi sofisticați de lemmatizare​

Ordinea liberă a cuvintelor în propoziții

Recomandări pentru Proiect
Abordare Progresivă:

Nivel 1 (Simplu):

Implementare Huffman coding pe caractere românești

Eliminare stopwords

Compresie lossless simplă

Nivel 2 (Mediu):

Adaugă BPE pentru subword tokenization

Implementare stemming/lemmatizare

Testare pe corpora multiple

Nivel 3 (Avansat):

Implementare PPM cu context modeling

Codificare aritmetică

Compresie lossy cu păstrarea semanticii

Modele neuronale pentru predicție context​

Resurse Tehnologice
Biblioteci Python:

nltk - stopwords și stemming pentru română​

spacy-stanza - lemmatizare și POS tagging​

huffman - implementare Huffman coding

zlib, bz2 - compresie standard Python

Tooluri Specializate:

TEPROLIN - platform pentru procesare text românesc​

RoBERT - model BERT pentru română​

Snowball Stemmer pentru română​

Concluzii
Pentru tema ta de master, recomand un algoritm hibrid care combină:

Eliminarea redundanței lingvistice (stopwords, lemmatizare)

Codificare statistică (Huffman sau BPE)

Compresie pattern-based (LZ77 sau PPM)

Această abordare va demonstra înțelegerea conceptelor fundamentale din teoria informației, va fi adaptată specificului limbii române și va oferi rate competitive de compresie. Rata de compresie așteptată: 60-75% pentru compresie lossless, sau 80-90% pentru compresie lossy cu păstrarea informației cheie.

Profesorul probabil caută să înțelegi cum redundanța naturală a limbii (Legea lui Zipf, entropia Shannon, contextul predicțibil) poate fi exploatată pentru compresie eficientă, păstrând doar informația esențială necesară pentru reconstrucția mesajului.​