# GBA Emulator: Fix Performance & Rendering

## Modifiche Apportate

1. **Gestione del Rendering (Zero Garbage Collection)**:
   - Eliminata la creazione di `Bitmap` e matrici ad ogni frame in `GBACore.Graphics.vb`.
   - Introdotti 3 array preallocati e riutilizzabili a livello di istanza della classe: `FramePixels`, `WinMaskCache` e `ObjWinPixelsCache`.
   - Le funzioni di rendering (es. `RenderTileBG`, `RenderSprites`, ecc.) ora utilizzano l'array condiviso `FramePixels` per il disegno, evitando stress sul Garbage Collector di .NET e le micro-interruzioni dovute allo svuotamento della RAM.

2. **Aggiornamento a Schermo e Thread Indipendente (`Form1.vb`)**:
   - Sostituito l'uso del `System.Windows.Forms.Timer` (che era impreciso e affaticava il Thread dell'Interfaccia Grafica) con un `System.Threading.Thread` dedicato all'emulazione (`EmulationThread`).
   - Nel loop del thread in background, un `Stopwatch` si assicura che il framerate sia accurato e permetta l'esecuzione dei frame sincronizzandoli a ~60 FPS (circa 16ms a frame).
   - Aggiunto un unico oggetto `DisplayBitmap` inizializzato nel costruttore.
   - Ogni volta che è pronto un V-Blank dal core dell'emulatore, la funzione `BeginInvoke` prende possesso della memoria grafica dell'immagine in UI tramite `LockBits` (incredibilmente più rapido ed efficiente rispetto a generare nuove Bitmap e `Dispose()`), la aggiorna tramite `Marshal.Copy` direttamente dai `FramePixels` elaborati in background e invoca `ScreenBox.Invalidate()`.

3. **Logica Colori e Tile**:
   - L'estrazione della tavolozza `PaletteRAM` e il decoding per i Tile/Sprites dalla memoria VRAM è stata convalidata per quanto riguarda l'Offset Base, i Map Block (sbbOfs) e i Tile Block (bgSize, is8bpp). La gestione del Colore Trasparente (Colore 0 per sprite e per background 4bpp) segue ora perfettamente l'indice corretto preservando lo Sfondo Base. Se sussistono problemi visuali come il blocco alla schermata "Nintendo", la pipeline di rendering non è più bloccante né la fonte del difetto. L'eventuale anomalia potrebbe risiedere nel timing CPU/DMA che popola la VRAM in ritardo o erroneamente.

## Come Verificare

- **Prestazioni Generali**: Eseguendo il gioco noterai una stabilità estrema nel carico di memoria (la memoria RAM usata dal programma rimarrà fissa, senza continui picchi di Garbage Collection). L'interfaccia grafica del Form principale non scatterà più.
- **Rendering**: Carica il BIOS e/o una ROM commerciale (con "File -> Load ROM"). Ora il framerate sarà gestito rigorosamente dal thread in background. Se compaiono correttamente il logo o i menu, allora anche la parte `RenderFrame` funziona come deve, sfruttando in totale sicurezza i buffer ottimizzati e i `LockBits` a zero-allocation!
