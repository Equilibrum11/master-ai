# Curs complet: Teoria transmiterii și codificării informației

**Explicat detaliat cu exemple practice pentru studenți informatică - IA.**

---

## **MODUL 1: Măsurarea Cantitativă a Informației**

### **1.1. Informația – generalități**

**Ce este informația?**
Informația este orice știre care poartă în sine urma unui fapt, eveniment sau proces. Aflarea unei informații înseamnă reducerea incertitudinii privind acel eveniment.

**Proprietăți ale informației:**
- Informația poate fi măsurată
- Nu se uzează prin utilizare — dimpotrivă, generează cantități noi de informație
- Apare mereu cu zgomot/perturbații în sisteme reale
- Poate fi stocată și transmisă

**Exemple:**
- Dacă arunci o monedă, rezultatul (cap/pajură) conține informație deoarece nu știai dinainte care va fi rezultatul.
- La un semafor cu trei culori (roșu, galben, verde), aflarea culorii actuale este o informație.

### **1.2. Sistemul de transmitere a informației**

Orice transmisie de date folosește următoarea schemă:

**Sursa → Coder → Canal → Decoder → Destinatar**

- **Sursa**: generează mesajul (om, senzor, dispozitiv)
- **Coder (sursei)**: comprimă și codifică datele pentru a reduce redundanța
- **Coder (canalului)**: adaugă biți de control pentru detectarea și corectarea erorilor
- **Canalul**: mediul fizic (fir, radio, internet), afectat de zgomot
- **Decoder (canalului)**: detectează și corectează erorile
- **Decoder (sursei)**: decomprimă mesajul
- **Destinatar**: primește mesajul (om sau dispozitiv)

**Exemplu:**
O convorbire telefonică: vocea ta (sursa) → microfon + encoder → semnal electric pe fir (canal) → receptor + decoder → difuzor (destinatar).

### **1.3. Modele probabilistice ale semnalelor**

Pentru o sursă discretă cu alfabetul X = {x₁, x₂, ..., xₙ}, modelul probabilistic este:

\[
X = 
\begin{bmatrix}
x_1 & x_2 & \cdots & x_n \\
p(x_1) & p(x_2) & \cdots & p(x_n)
\end{bmatrix}
\]

unde \(p(x_i)\) este probabilitatea de apariție a simbolului \(x_i\).

**Exemplu:**
Dacă arunci un zar echilibrat, fiecare față are probabilitatea \(p = 1/6\).

### **1.4. Măsuri informaționale — Entropia**

**Entropia lui Shannon** măsoară cantitatea medie de informație pe simbol:

\[
H(X) = -\sum_{i=1}^n p_i \log_2 p_i
\]

Unitate de măsură: **biți**

**Proprietăți:**
1. \(H(X) \geq 0\) (entropia este pozitivă)
2. \(H(X)\) este maximă când toate simbolurile sunt echiprobabile: \(H(X) = \log_2 n\)

**Exemple rezolvate:**

#### **Problema 1: Fax cu 2.25 milioane pixeli și 12 tonuri de gri**
Informația per pixel:
\[
I = \log_2 12 \approx 3.58 \text{ biți}
\]
Informația totală:
\[
I_{total} = 2.25 \times 10^6 \times 3.58 \approx 8.06 \text{ milioane de biți}
\]

#### **Problema 2: Monedă echilibrată**
Pentru o monedă cu \(p(\text{cap}) = p(\text{pajură}) = 0.5\):
\[
H(X) = -2 \times 0.5 \log_2 0.5 = 1 \text{ bit}
\]

#### **Problema 3: Poziția unei piese pe tabla de șah**
Sunt 64 poziții posibile:
\[
I = \log_2 64 = 6 \text{ biți}
\]

### **1.5. Informația proprie și condiționată**

**Informația proprie** a unui eveniment \(x_k\):
\[
I(x_k) = \log_2 \frac{1}{p(x_k)} = -\log_2 p(x_k)
\]

**Informația condiționată** \(I(x|y)\) reprezintă informația necesară pentru a specifica \(x\) după ce știm că a avut loc \(y\).

**Transinformația** (informația transmisă):
\[
I(X;Y) = H(X) - H(X|Y)
\]

Arată cu cât s-a redus incertitudinea despre \(X\) prin observarea lui \(Y\).

### **1.6. Capacitatea canalului**

**Capacitatea** unui canal de transmisie este cantitatea maximă de informație pe care o poate transmite:
\[
C = \max_{p(x_i)} I(X;Y)
\]

**Exemplu:**
Pentru un canal binar simetric cu probabilitatea de eroare \(p\):
\[
C = 1 + p \log_2 p + (1-p) \log_2 (1-p)
\]

### **Teste de autoevaluare — Modulul 1**

**Test 1:** Un alfabet format din {A, B, C}. Câte mesaje de lungime 3 se pot forma și câtă informație conține fiecare?
- Număr de mesaje: \(3^3 = 27\)
- Informație per mesaj: \(\log_2 27 \approx 4.75\) biți

**Test 2:** Calculați cantitatea de informație pentru poziția unei figuri pe tabla de șah.
- Răspuns: \(\log_2 64 = 6\) biți

---

## **MODUL 2: Codarea Surselor Informaționale**

### **2.1. Ce este codarea sursei?**

Codarea sursei transformă mesajele în șiruri binare astfel încât să ocupe cât mai puțin spațiu, menținând decodarea unică.

**Tipuri de coduri:**
- **Bloc-bloc**: lungime fixă pentru mesaj și cod
- **Variabil-variabil**: lungime variabilă
- **Cod cu proprietate de prefix (instantaneu)**: niciun cod nu este prefixul altuia

### **2.2. Teorema Kraft**

Condiția necesară și suficientă pentru existența unui cod ireductibil:
\[
\sum_{i=1}^N q^{-n_i} \leq 1
\]
unde \(q\) = numărul de litere din alfabetul codului, \(n_i\) = lungimea cuvântului \(i\).

### **2.3. Teorema Shannon de codare a sursei (Teorema I)**

Pentru o sursă cu entropia \(H(X)\), există un cod astfel încât lungimea medie \(\bar{n}\) să satisfacă:
\[
\frac{H(X)}{\log_2 q} \leq \bar{n} < \frac{H(X)}{\log_2 q} + 1
\]

**Eficiența codului:**
\[
\eta = \frac{H(X)}{\bar{n} \log_2 q}
\]

**Redundanța:**
\[
\rho = 1 - \eta
\]

### **2.4. Codul Huffman**

**Principiu:** Simbolurile frecvente primesc coduri scurte.

**Algoritm:**
1. Ordonează simbolurile descrescător după probabilități
2. Grupează ultimele 2 simboluri (cele mai puțin probabile)
3. Formează o sursă restrânsă și re-ordonează
4. Repetă până rămân 2 simboluri
5. Atribuie 0 și 1 fiecărei ramuri, citind de la rădăcină la frunze

**Exemplu:**

| Simbol | Probabilitate | Cod Huffman |
|--------|---------------|-------------|
| A      | 0.5           | 0           |
| B      | 0.25          | 10          |
| C      | 0.125         | 110         |
| D      | 0.125         | 111         |

Lungime medie:
\[
\bar{n} = 0.5 \times 1 + 0.25 \times 2 + 0.125 \times 3 + 0.125 \times 3 = 1.75 \text{ biți}
\]

Entropia:
\[
H(X) = 0.5 \log_2 2 + 0.25 \log_2 4 + 2 \times 0.125 \log_2 8 = 1.75 \text{ biți}
\]

Eficiență: \(\eta = 1\) (cod optimal!)

### **2.5. Codul Shannon-Fano**

Similar cu Huffman, dar partajează sursa în două grupe cu probabilități cât mai egale.

**Algoritm:**
1. Ordonează simbolurile descrescător
2. Împarte în 2 grupe cu suma probabilităților cât mai egale
3. Atribuie 0 primei grupe, 1 celei de-a doua
4. Repetă pentru fiecare grupă până rămâne un simbol

**Exemplu:**

| Simbol | Probabilitate | Cod Shannon-Fano |
|--------|---------------|------------------|
| A      | 0.4           | 0                |
| B      | 0.3           | 10               |
| C      | 0.2           | 110              |
| D      | 0.1           | 111              |

Lungime medie: \(\bar{n} = 1.9\) biți

### **Teste de autoevaluare — Modulul 2**

**Test 1:** Pentru sursa cu probabilități {0.48, 0.14, 0.14, 0.07, 0.07, 0.04, 0.02, 0.02, 0.02}, codificați cu Huffman și calculați lungimea medie.

**Test 2:** Același exercițiu cu Shannon-Fano și comparați eficiențele.

---

## **MODUL 3: Coduri Detectoare și Corectoare de Erori**

### **3.1. Codarea pe canale perturbate**

Pe canalele cu zgomot, biții pot fi alterați: 0→1 sau 1→0.

**Obiective:**
1. **Detecția erorilor**: identificarea prezenței erorilor
2. **Corecția erorilor**: corectarea automată fără retransmisie

### **3.2. Distanța Hamming**

Distanța între două cuvinte binare = numărul de poziții pe care diferă.

**Exemplu:**
\[
d(1011, 0011) = 1
\]
\[
d(1100, 0001) = 3
\]

**Proprietăți:**
- \(d(u,v) \geq 0\), cu egalitate dacă \(u = v\)
- \(d(u,v) = d(v,u)\)
- \(d(u,w) \leq d(u,v) + d(v,w)\)

**Teoreme:**
- Pentru a **detecta** \(t\) erori: \(d_{min} \geq t + 1\)
- Pentru a **corecta** \(t\) erori: \(d_{min} \geq 2t + 1\)

### **3.3. Coduri bloc liniare**

Un cod bloc \((n, k)\):
- \(k\) = biți de informație
- \(n-k\) = biți de control
- \(n\) = lungimea cuvântului de cod

**Matricea generatoare:**
\[
G = [I_k | P]
\]
unde \(I_k\) = matricea identitate \(k \times k\), \(P\) = matricea de paritate.

**Codarea:**
\[
c = m \cdot G
\]
unde \(m\) = mesajul, \(c\) = cuvântul de cod.

### **3.4. Codul Hamming**

Cod \((n, k)\) cu:
- \(n = 2^m - 1\)
- \(k = 2^m - m - 1\)
- \(d_{min} = 3\)

**Capacitate:** corectează **1 eroare**, detectează **2 erori**.

**Exemplu: Codul Hamming (7, 4)**
- 4 biți de informație
- 3 biți de control
- Lungime cuvânt: 7

**Exercițiu rezolvat:**

Fie mesajul \(m = [1011]\). Calculați cuvântul de cod:

Matricea \(G\):
\[
G = \begin{bmatrix}
1 & 0 & 0 & 0 & 1 & 1 & 0 \\
0 & 1 & 0 & 0 & 1 & 0 & 1 \\
0 & 0 & 1 & 0 & 0 & 1 & 1 \\
0 & 0 & 0 & 1 & 1 & 1 & 1
\end{bmatrix}
\]

Cuvântul de cod:
\[
c = [1011] \cdot G = [1011011]
\]

Dacă se recepționează \(r = [1111011]\), sindromul detectează eroarea pe poziția 3.

### **3.5. Codul Reed-Muller**

**Parametri:**
- Lungime: \(n = 2^m\)
- Biți de informație: \(k = \sum_{i=0}^r C_m^i\)
- Distanță minimă: \(d = 2^{m-r}\)
- Capacitate de corecție: \(t = \lfloor \frac{d-1}{2} \rfloor = 2^{m-r-1} - 1\)

**Exemplu: RM(5,2)**
- \(m=5\), \(r=2\)
- \(n = 32\)
- \(k = 16\)
- \(d = 8\)
- \(t = 3\) (corectează până la 3 erori)

**Decodarea** se bazează pe **logica majoritară**: fiecare bit al mesajului este determinat prin mai multe relații de control, alegându-se valoarea care apare cel mai des.

### **3.6. Codul BCH (Bose-Chaudhuri-Hocquenghem)**

Generalizare a codurilor Hamming pentru corecția erorilor multiple.

**Parametri:**
- Lungime: \(n = 2^m - 1\)
- Biți de control: \(n - k \leq mt\)
- Distanță: \(d \geq 2t + 1\)

**Exemplu: BCH(15, 7)**
- Corectează 2 erori
- Polinom generator: \(g(x) = 1 + x^4 + x^6 + x^7 + x^8\)

### **Exercițiu rezolvat — Cod Hamming (7,4)**

**Date:**
- 8 mesaje de 3 biți
- Cod Hamming grup corector de 1 eroare

**Soluție:**

a) \(k = 3\), \(m = 3\), \(n = 7\)

b) Matricea de control:
\[
H = \begin{bmatrix}
0 & 0 & 0 & 1 & 1 & 1 & 1 \\
0 & 1 & 1 & 0 & 0 & 1 & 1 \\
1 & 0 & 1 & 0 & 1 & 0 & 1
\end{bmatrix}
\]

c) Pentru eroare pe \(c_2\), sindromul este \([1, 1, 0]^T\)

d) Pentru erori pe \(c_2\) și \(c_1\), sindromul este \([0, 0, 0]^T\) → detectare eroare dublă

e) Cuvintele de cod se calculează cu \(c = m \cdot G\)

f) Pentru \(v = [1100110]\), calculați \(s = v \cdot H^T\). Dacă \(s = 0\), este cuvânt de cod.

### **Teste de autoevaluare — Modulul 3**

**Test 1:** Pentru un cod cu \(H = \begin{bmatrix} 1 & 0 & 0 & 1 & 1 \\ 0 & 1 & 0 & 0 & 1 \\ 0 & 0 & 1 & 1 & 0 \end{bmatrix}\), determinați capacitatea de corecție.

**Test 2:** Verificați dacă \(G = \begin{bmatrix} 1 & 0 & 1 & 1 & 0 \\ 1 & 1 & 0 & 0 & 1 \end{bmatrix}\) poate fi matricea generatoare a codului.

---

## **Rezumat general**

**Modul 1** explică cum se măsoară informația folosind entropia lui Shannon, concepte probabilistice și exemple practice (fax, șah, monede).

**Modul 2** prezintă algoritmii de codificare eficientă (Huffman, Shannon-Fano) și teorema lui Shannon care stabilește limitele teoreti ce ale compresiei.

**Modul 3** detaliază codurile corectoare de erori (Hamming, Reed-Muller, BCH), distanța Hamming și cum se detectează/corectează erori în transmisii cu zgomot.

---

## **Bibliografie**

1. A. Spătaru: *Teoria Transmisiunii Informației*, Ed. Didactică și Pedagogică, București, 1983.
2. J.C. Moreira, P.G. Farrell: *Essentials of Error-Control Coding*, John Wiley & Sons, 2006.
3. I. Angheloiu: *Teoria codurilor*, Ed. Militară, București, 1972.
4. Fișiere din curs: Modulul 1+2, Modulul 3, Teoria informației și codurilor.

---

**Document creat pentru învățare eficientă și pregătire pentru examene la Informatică - Inteligență Artificială.**
