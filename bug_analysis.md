# Analisi dei Bug - VB-GBA vs GBATEK

Analisi dei file sorgente confrontati con la documentazione GBATEK.
I bug sono classificati per gravità: 🔴 Critico (causa crash/freeze), 🟡 Importante (causa glitch visivi/comportamento errato), 🔵 Minore.

---

## 🔴 Bug Critici

### 1. IRQ: Indirizzo di ritorno errato (`GBACore.vb` riga 439)

**Codice attuale:**
```vb
R(14) = R(15) + 4
```

**GBATEK dice:**
> On interrupt, the CPU saves the address of the *next* instruction plus 4 in LR_irq (R14_irq).  
> Per ARM: `LR = PC + 4` dove PC al momento dell'IRQ vale già `ExePC + 4` (pipeline).  
> Per Thumb: `LR = PC + 4` dove PC = `ExePC + 2`.

**Problema:** `R(15)` al momento dell'IRQ è già stato aggiornato a `ExePC + 4` (ARM) o `ExePC + 2` (Thumb). 
Quindi stai salvando `ExePC + 8` (ARM) o `ExePC + 6` (Thumb), invece del corretto `ExePC + 4`.  
La `SUBS PC, LR, #4` del handler IRQ del BIOS calcolerà un indirizzo di ritorno sbagliato, causando loop infiniti o crash.

**Fix:**
```vb
' Prima di modificare R(15):
If ThumbMode Then
    R(14) = ExePC + 4UI  ' = next_instruction + 2 (come da ARM arch. ref.)
Else
    R(14) = ExePC + 4UI  ' = next_instruction + 4 (opcode corrente già fetchato)
End If
```

---

### 2. VBlank flag: Riga 227 dovrebbe essere esclusa (`GBACore.vb` riga 373)

**Codice attuale:**
```vb
Dim isVBlank = InternalVCount >= 160 AndAlso InternalVCount <= 227
```

**GBATEK dice:**
> Bit 0: V-Blank flag (set in line **160..226; not 227**)

**Problema:** La riga 227 viene inclusa erroneamente nel VBlank. Questo può far sparire il VBlank IRQ per l'ultima scanline, causando problemi di sincronizzazione nei giochi che aspettano il flag.

**Fix:**
```vb
Dim isVBlank = InternalVCount >= 160 AndAlso InternalVCount < 227
```

---

### 3. HBlank IRQ generato solo durante il rendering, non durante il VBlank (`GBACore.vb` riga 412-416)

**Codice attuale:**
```vb
' HBLANK IRQ (Non viene generato durante il V-Blank, righe 160-227)
If scanlineCycles = 960 AndAlso InternalVCount < 160 Then
```

**GBATEK dice:**
> "The H-Blank conditions are generated **once per scanline, including for the 'hidden' scanlines during V-Blank**."

**Problema:** Il commento nel codice è sbagliato e anche l'implementazione. L'HBlank IRQ **deve** essere generato anche durante il VBlank (righe 160-227). Molti giochi usano HBlank-DMA (timing=2) durante il VBlank per gestire audio o aggiornare dati. Senza questo, quei giochi si bloccano.

**Fix:**
```vb
' HBLANK IRQ (generato in TUTTE le scanline, incluso VBlank)
If scanlineCycles = 960 Then
    If (dispStat And &H10) <> 0 Then IF_reg = IF_reg Or 2US
    CheckPendingDMAs(2)
End If
```

---

### 4. DMA: Il contatore al reload non include la source dal registro (`GBACore.Memory.vb` riga 316)

**Codice attuale:**
```vb
Dim cnt As Integer = Read16(&H4000000UI + base + 8)
```

**Problema:** `base` è calcolato come `&HB0 + (ch * 12)`, quindi per ch=0 legge `0x4000000 + 0xB8` = indirizzo del Word Count (corretto). Ma poi in `CheckDMA` (riga 291-292) legge Source e Destination con `Read32` dallo stesso offset base, che usa `&HB0 + (ch * 12)`. 

Per DMA0: `base = 0xB0` → SAd=`0x4000000+0xB0`=`0x40000B0` ✓, DAD=`0x40000B4` ✓, CNT_L=`0x40000B8` ✓, CNT_H=`0x40000BA` ✓.  
Questo sembra corretto. Tuttavia il problema è che `RunDMA` rilegge `cnt` dal registro anziché dalla latch interna (i registri DMA CNT_L sono Write-only sul GBA reale - non si ri-leggono durante l'esecuzione).

---

### 5. Sprite: Threshold Y errata per wrapping (`GBACore.Graphics.vb` riga 432)

**Codice attuale:**
```vb
Dim y = a0 And &HFF : If y >= 128 Then y -= 256
```

**GBATEK dice:**
> Lo sprite Y è un valore 8-bit. Gli sprite con Y >= 160 sono normalmente fuori schermo (non wrappano di default). Il wrapping corretto per `y` negativo è `>= 192` non `>= 128`.

**Problema:** Con `>= 128`, sprite a Y=160 diventano `-96` invece di essere fuori schermo (`+160`). Questo causa sprite visibili nella parte alta dello schermo quando non dovrebbero esserlo, o sprite non visibili dove dovrebbero.

**Fix (conforme a GBA hardware):**
```vb
Dim y = CInt(a0 And &HFF)
If y >= 160 Then y -= 256   ' Wrapping corretto: 160..255 diventa -96..-1
```

---

## 🟡 Bug Importanti

### 6. Affine BG: Parametri pa/pb/pc/pd letti in modo errato (`GBACore.Graphics.vb` righe 290-298)

**Codice attuale:**
```vb
Dim line_cx = start_cx + m_y * pb
Dim line_cy = start_cy + m_y * pd
...
Dim rx = line_cx + m_x * pa
Dim ry = line_cy + m_x * pc
```

**GBATEK dice:** Le formule corrette sono:
- Per ogni pixel (screenX, screenY):
  - `texX = refX + pa * screenX + pb * screenY`
  - `texY = refY + pc * screenX + pd * screenY`

**Problema:** Il codice usa `pb` per il calcolo per riga e `pa` per colonna, ma accumula in modo sbagliato. Il metodo corretto è incrementare il reference point di `pb`/`pd` per ogni scanline e di `pa`/`pc` per ogni pixel. Il codice moltiplica per `m_y` e `m_x` direttamente sul reference point originale invece di lavorare in modo incrementale, il che è matematicamente equivalente ma solo se il reference point NON viene aggiornato frame per frame (cosa che però manca, vedi punto 7).

---

### 7. Affine BG: Reference Point non viene aggiornato per scanline (`GBACore.Graphics.vb`)

**GBATEK dice:**
> "The internal registers are then **incremented by dmx and dmy after each scanline**."  
> `refX_internal += pb` (per ogni scanline)  
> `refY_internal += pd` (per ogni scanline)

**Problema:** Il codice legge `BG2X`/`BG2Y` dai registri IO ogni frame e non mantiene registri interni persistenti aggiornati scanline per scanline. Questo impedisce effetti affine corretti (rotazione con il centro fisso), fondamentali in molti giochi come Mario Kart, F-Zero.

**Fix necessario:** Aggiungere campi `BG2X_internal`, `BG2Y_internal` (e BG3) che vengono copiati dai registri IO all'inizio di ogni VBlank e poi incrementati di `pb`/`pd` ogni scanline durante il rendering.

---

### 8. DISPSTAT: Bit 1 (HBlank) non viene letto dal registro `Read16` (`GBACore.Memory.vb` riga 54-59)

**Codice attuale:**
```vb
If off = 4 Then
    Dim savedStat = CUInt(IO(4)) Or (CUInt(IO(5)) << 8)
    Dim stat = savedStat And &HFFB8UI ' Tieni i bit scritti
    If InternalVCount >= 160 Then stat = stat Or 1UI
    If InternalVCount = (savedStat >> 8) Then stat = stat Or 4UI
    Return CUShort(stat)
End If
```

**Problema:** Il `Read16` di DISPSTAT non restituisce il bit 1 (HBlank flag). Il ciclo principale lo scrive in `IO(4)`, ma la lettura speciale sovrascrive con un calcolo che non include HBlank. I giochi che leggono DISPSTAT per aspettare HBlank (senza usare IRQ) non funzioneranno mai.

**Fix:**
```vb
If off = 4 Then
    Dim savedStat = CUInt(IO(4)) Or (CUInt(IO(5)) << 8)
    Dim stat = savedStat And &HFFB8UI
    If InternalVCount >= 160 AndAlso InternalVCount < 227 Then stat = stat Or 1UI
    Dim scanCycles = CycleCount Mod 1232
    If scanCycles >= 960 Then stat = stat Or 2UI  ' HBlank flag
    If InternalVCount = (savedStat >> 8) Then stat = stat Or 4UI
    Return CUShort(stat)
End If
```

---

### 9. Mode 1: BG2 in Affine non gestito, BG3 non deve esistere (`GBACore.Graphics.vb` riga 34-38)

**Codice attuale:**
```vb
If mode = 0 OrElse (mode = 1 And i < 2) Then
    RenderTileBG(i, bgCnt)
Else
    RenderAffineBG(i, bgCnt)
End If
```

**GBATEK dice:**
> Mode 1: BG0 e BG1 sono Tile mode, **BG2 è Affine**. BG3 **non esiste** in Mode 1.

**Problema:** Con `i < 2` il codice usa Tile per BG0/BG1 (corretto), ma per BG2 e BG3 usa `RenderAffineBG`. Il loop va da i=3 a i=0. In Mode 1, BG3 non dovrebbe essere renderizzato affatto (il registro DISPCNT bit 11 potrebbe essere abilitato da giochi scorretti, ma BG3 in Mode 1 è proibito).

**Fix:**
```vb
If mode = 0 Then
    RenderTileBG(i, bgCnt)  ' Tutti tile
ElseIf mode = 1 Then
    If i <= 1 Then RenderTileBG(i, bgCnt) ElseIf i = 2 Then RenderAffineBG(i, bgCnt)
    ' BG3 ignorato in Mode 1
ElseIf mode = 2 Then
    RenderAffineBG(i, bgCnt)  ' BG2 e BG3 affine
End If
```

---

### 10. BIOS non presente: SWI &H6 (DIV) non gestisce divisione per zero con eccezione (`GBACore.CPU.Thumb.vb` riga 217)

**Codice attuale:**
```vb
If d <> 0 Then
    R(0) = CUInt(n \ d)
    ...
End If
```

**GBATEK dice:** La SWI DIV sul GBA reale causa un loop infinito se `d=0`. Il codice semplicemente non fa nulla se `d=0`, lasciando R0 con il valore originale invece di comportarsi come il hardware reale (loop). Questo può portare a comportamenti diversi rispetto all'hardware, ma non è un crash dell'emulatore.

---

### 11. THUMB Push/Pop: Ordine errato per PUSH (`GBACore.CPU.Thumb.vb` righe 183-186)

**Codice attuale:**
```vb
If Not isPop Then
    If pLr Then R(13) -= 4 : Write32(R(13), R(14))  ' Prima LR
    For i = 7 To 0 Step -1 : ...  ' Poi R7..R0
```

**GBATEK / ARM Architecture Reference Manual:**
> PUSH decrements SP first, then stores. L'ordine corretto è: prima si decrementano e si scrivono i registri alti (R7..R0) e **poi LR** (R14 ha l'indirizzo più basso, R0 il più alto).

**Fix:**
```vb
If Not isPop Then
    ' Prima calcola il totale e poi scrivi nell'ordine corretto
    ' LR va scritto DOPO R7..R0 (all'indirizzo più basso)
    For i = 7 To 0 Step -1 : If (lst And (1 << i)) <> 0 Then R(13) -= 4 : Write32(R(13), R(i)) : Next
    If pLr Then R(13) -= 4 : Write32(R(13), R(14))
```
*(In realtà il codice attuale scrive LR all'indirizzo più alto - errore nell'ordine di push)*

---

## 🔵 Bug Minori / Comportamento non standard

### 12. Lettura BIOS da fuori BIOS (`GBACore.Memory.vb` righe 10, 43, 93)

**Codice attuale:**
```vb
If R(15) >= &H4000 Then Return 0
```

**GBATEK dice:** Quando si tenta di leggere la BIOS ROM da fuori di essa, il GBA restituisce l'**ultimo opcode letto dalla BIOS** (non zero). Molti giochi non dipendono da questo, ma è un comportamento non standard che può causare problemi in casi edge.

---

### 13. WRAM mirror mancante (`GBACore.Memory.vb`)

**GBATEK dice:** WRAM a 256K (0x02000000) ha un mirror ogni 256K fino a 0x02FFFFFF. Il codice usa `address And &H3FFFF` che è corretto per 256K, ma la maschera corretta per coprire tutti i mirror è semplicemente questa, quindi questo è in realtà OK.

Tuttavia, **IRAM (0x03000000)** dovrebbe mirrare fino a 0x03FFFFFF. La maschera attuale `&H7FFF` è corretta per 32K ma potrebbe mancare il mirror `0x03FFXXXX`. Molti giochi accedono all'IRAM tramite mirror `0x03007F00` (lo stack pointer default) che è corretto, ma GBATEK indica che i mirror vanno fino a `0x03FFFFFF`.

---

## Priorità di Fix Consigliata

| Priorità | Bug | Impatto |
|---|---|---|
| 1 | #3 HBlank durante VBlank | Molti giochi si bloccano, audio DMA non funziona |
| 2 | #1 Indirizzo ritorno IRQ | IRQ restituisce all'indirizzo sbagliato → crash |
| 3 | #2 VBlank flag riga 227 | Sincronizzazione errata |
| 4 | #8 HBlank bit in DISPSTAT | Giochi che fanno polling di DISPSTAT si bloccano |
| 5 | #7 Affine BG reference interno | Effetti affine scorretti (Mario Kart, F-Zero, ecc.) |
| 6 | #5 Sprite Y wrapping | Sprite in posizione errata |
| 7 | #11 PUSH ordine | Stack corrotto → crash casuale |
| 8 | #9 Mode 1 BG3 | Grafica errata in Mode 1 |
