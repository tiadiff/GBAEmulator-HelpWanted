# Save States (Salvataggio Seriale) Tasks

- [x] 1. Serializzazione in `GBACore.vb`
  - [x] Implementare `Public Sub SaveState(path As String)` utilizzando `BinaryWriter`.
  - [x] Implementare `Public Sub LoadState(path As String)` utilizzando `BinaryReader`.
  - [x] Assicurarsi di salvare e caricare tutte le variabili di stato e gli array RAM/Registri nello stesso ordine.

- [x] 2. Aggiornamento UI in `Form1.Designer.vb`
  - [x] Aggiungere `SaveStatesToolStripMenuItem` e `LoadStatesToolStripMenuItem` al menu `Emulation`.

- [x] 3. Logica UI in `Form1.vb`
  - [x] Popolare dinamicamente `SaveStatesToolStripMenuItem` con gli Slot da 1 a 9.
  - [x] Creare metodo `UpdateLoadStatesMenu()` per mostrare solo gli slot esistenti.
  - [x] Chiamare `UpdateLoadStatesMenu()` ogni volta che si carica una ROM o si salva uno stato.

- [x] 4. Verification
  - [x] Compilare il codice.
  - [x] Aggiornare il walkthrough.
  - [x] Fix Race Condition (EmulationThread Join) durante il salvataggio/caricamento.
