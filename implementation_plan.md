# Miglioramento della Qualità Audio GBA (APU)

Il comparto audio attualmente produce un suono corretto a livello di intonazione, ma la qualità risulta "grezza" e soggetta a rumori di fondo e distorsioni.
Questo è dovuto all'assenza di due componenti fondamentali dell'hardware originale e di un piccolo errore di miscelazione.

## Proposed Changes

Per raggiungere un suono fedele alla console originale interverremo su 3 fronti all'interno di `GBACore.APU.vb`.

### 1. Frame Sequencer (Inviluppo e Decadimento)
Attualmente i canali storici (Pulse 1, Pulse 2, Noise) generano suono senza mai attenuarsi, rendendo gli effetti sonori innaturalmente lunghi.
Il Game Boy originale utilizza un "Frame Sequencer" hardware che gira a 512 Hz e si occupa di scalare automaticamente i volumi e spegnere i canali nel tempo.
- **[MODIFY] [GBACore.APU.vb](file:///c:/Users/matti/source/repos/vb-gba/GBACore.APU.vb)**:
  - Aggiungerò un `fsCycleAccumulator` che scatta ogni 32.768 cicli di clock della CPU (esattamente 512 Hz).
  - Questo orologio chiamerà una nuova funzione `StepFrameSequencer()` che a cadenze specifiche (es. 64 Hz per l'inviluppo di volume) abbasserà progressivamente l'onda dei canali Pulse e Noise, dando vita agli effetti sonori (es. il suono dei salti o delle monete corti e puliti).

### 2. Filtro Passa-Basso Analogico (Low-Pass Filter)
Il GBA non interpola il Direct Sound, ma sputa fuori l'audio raw scalinato. Se ascoltato così com'è, si percepiscono frequenze acute sgradevoli (Aliasing e Quantization Noise). Il GBA reale ha un circuito elettrico Resistenza-Condensatore (RC Filter) prima dello speaker che ammorbidisce il suono.
- **[MODIFY] [GBACore.APU.vb](file:///c:/Users/matti/source/repos/vb-gba/GBACore.APU.vb)**:
  - Applicherò un First-Order IIR Low-Pass Filter matematico (`y[n] = alpha * x[n] + (1 - alpha) * y[n-1]`) al segnale Left e Right appena prima di inviarlo al buffer. Questo donerà al suono un timbro molto più caldo e fedele a quello emesso dalla plastica del GBA.

### 3. Bilanciamento del Mixer Anti-Clipping
Al momento misceliamo fino a 2 canali Direct Sound e 4 canali PSG. La somma matematica delle onde viene poi moltiplicata per `64.0F`, causando picchi oltre il limite dei 16-bit (32767) che generano una fastidiosa distorsione armonica detta "Hard Clipping".
- **[MODIFY] [GBACore.APU.vb](file:///c:/Users/matti/source/repos/vb-gba/GBACore.APU.vb)**:
  - Ridurrò il moltiplicatore a `32.0F` (o simile proporzione sicura calcolata) in modo da garantire il massimo volume possibile (Headroom) senza sforare MAI il muro del clipping digitale.

## User Review Required
> [!IMPORTANT]
> L'implementazione del filtro Passa-Basso cambia la "pasta" del suono rendendolo leggermente più cupo (come la vera console) anziché stridulo. Ti andrà bene questa sfumatura retro, o preferisci un mix digitale iper-cristallino a prescindere dall'accuratezza hardware?

Attendo la tua approvazione per cominciare a programmare questi tre step e raffinare definitivamente l'APU!
