# Implementation Plan: Save States (Salvataggio Seriale)

I Save States (salvataggi di stato) permettono di "congelare" l'intero emulatore in un momento esatto e ripristinarlo all'istante, bypassando il sistema di salvataggio interno del gioco.

## Proposed Changes

### 1. Metodi di Serializzazione in `GBACore.vb`
Per evitare file giganteschi (es. serializzare i 32MB della ROM), implementeremo un salvataggio binario *manuale e controllato* (custom serialization via `BinaryWriter`/`BinaryReader`) che salverà solo la RAM mutabile e i registri della CPU.
Aggiungeremo due nuovi metodi a `GBACore`:
- [NEW] `Public Sub SaveState(path As String)`: Questo metodo scriverà in sequenza nel file:
  - Registri CPU (`UserRegs`, `FIQRegs`, ecc.)
  - Registri di stato (`CPSR`, `WaitCnt`, `MemCtrl`, Timers, DMA)
  - Memorie RAM (`WRAM`, `IRAM`, `VRAM`, `PaletteRAM`, `OAM`, `IO`)
  - Memorie Cartuccia (`SRAM`, `EEPROMData`, `FlashData`)
- [NEW] `Public Sub LoadState(path As String)`: Leggerà i dati nello stesso esatto ordine ripristinando l'intero stato dell'emulatore.

### 2. Gestione UI nel `Form1.vb`
Nel `MenuStrip`, sotto il menu `Emulation`, aggiungeremo una nuova voce **Save States** con due sottomenu:
- **Save State to Slot**: Mostrerà Slot 1..9. Cliccando su uno, verrà generato un file con estensione `.stX` (es. `gioco.st1`).
- **Load State from Slot**: Mostrerà *dinamicamente* solo gli slot che effettivamente esistono su disco per la ROM attualmente in esecuzione.

Quando si carica un gioco, verrà letta la directory per trovare i file `[NomeRom].stX` esistenti e il menu `Load State` verrà popolato di conseguenza.

## Open Questions
> [!QUESTION]
> Il salvataggio dello stato serializzerà la CPU, la memoria grafica e i registri I/O. Il processore audio (APU) è molto complesso: va bene se il suono viene ignorato nel Save State? Questo significa che quando caricherai uno stato, l'audio potrebbe essere muto per una frazione di secondo (finché il gioco non suona la nota successiva), ma manterrà i salvataggi leggeri, stabili e rapidissimi. Sei d'accordo?

## Verification Plan
1. Avvieremo un gioco.
2. Metteremo in pausa o andremo in un punto specifico.
3. Salveremo nello "Slot 1".
4. Resetteremo la ROM o cambieremo area nel gioco.
5. Caricheremo lo "Slot 1" per verificare il ripristino istantaneo e perfetto dell'immagine e dello stato.
