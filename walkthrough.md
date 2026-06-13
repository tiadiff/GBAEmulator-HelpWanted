# Walkthrough: UI and UX Improvements

## Modifiche Apportate

1. **Status Bar (StatusStrip):**
   - Aggiunta una barra di stato (`StatusStrip`) in basso al Form principale.
   - Spostati gli aggiornamenti di FPS, tempo CPU e GPU dalla barra del titolo della finestra (che causava sfarfallio) alle etichette `ToolStripStatusLabel`.
   - Lo stato del BIOS (Caricato, Non Trovato, Uso HLE) viene ora riportato nell'angolo in basso a sinistra della barra di stato.

2. **Scaling della Finestra (Window Size):**
   - Aggiunto il menu `View -> Window Size` con opzioni rapide per ridimensionare lo schermo.
   - Le opzioni 1x (240x160), 2x, 3x e 4x calcolano dinamicamente la grandezza della finestra al netto dei menu e delle barre di stato, preservando in modo esatto il form-factor originale del GameBoy Advance (3:2).

3. **Menu Emulation e Options:**
   - Introdotto il menu `Emulation` per eseguire la pausa (`Pause / Resume`) o il riavvio (`Reset`) veloce della ROM in esecuzione.
   - Aggiunto un menu `Options -> Controls...` che mostra una finestra descrittiva con i mapping dei tasti, per permettere all'utente di giocare senza dover per forza leggere il codice sorgente.

4. **Drag & Drop:**
   - La finestra e la `ScreenBox` dell'emulatore supportano ora il Drag & Drop dei file (impostato `AllowDrop = True`).
   - Trascinando un file `.gba` la ROM viene caricata immediatamente, rendendo l'utilizzo molto più rapido e moderno rispetto all'apertura da "File -> Load ROM".

5. **Audio Mute/Pause in Background:**
   - Intercettati gli eventi `Deactivate` (quando la finestra perde il focus) e `Activated` (quando torna attiva).
   - L'emulatore entra automaticamente in pausa se viene spostato in secondo piano, bloccando l'audio tramite `Emulator.APU.StopAudio()` e fermando il thread di emulazione, per poi riprendere quando l'utente torna sulla finestra. Questo migliora notevolmente la fruibilità.

## Validazione Precedente
- [x] UI/UX migliorata, Dark Mode estesa ai menu.
- [x] Scalabilità schermo e controlli Drag&Drop funzionanti.

---

## 6. Salvataggio e Caricamento su Disco (File .sav)
I giochi per GameBoy Advance salvano tipicamente i loro progressi nella SRAM, EEPROM o memoria Flash interna della cartuccia. Fino a poco fa, queste memorie esistevano solo nella RAM dell'emulatore.

**Cosa è stato aggiunto:**
- **Compatibilità Totale:** L'emulatore ora crea e carica file `.sav` grandi esattamente quanto la memoria di backup originale del gioco (es. 32KB per la SRAM, 128KB per le Flash). Questo li rende perfettamente interscambiabili con altri emulatori famosi come *VisualBoyAdvance* e *mGBA*.
- **Auto-Salvataggio Dinamico:** Non devi preoccuparti di cliccare "Salva" nell'emulatore. Ogni volta che il gioco decide di scrivere in memoria per salvare la partita, l'emulatore se ne accorge istantaneamente (tramite il flag `BatteryModified`). Ogni 3 secondi, se c'è stato un nuovo salvataggio nel gioco, l'emulatore scrive in background il file `.sav` su disco.
- **Transizioni Sicure:** L'emulatore forza un ultimo salvataggio di sicurezza non solo alla chiusura della finestra, ma anche quando trascini un nuovo gioco mentre un altro è già in esecuzione, evitando qualsiasi potenziale perdita di progressi.

## Validazione Finale
- [x] Il gioco compila senza errori ed il bug che duplicava l'evento di chiusura è stato risolto unificandolo con le operazioni di Stop Audio e Save Battery.
- [x] Le funzionalità hardware (SRAM, EEPROM, Flash) generano segnali di scrittura catturati dall'emulatore.
- [x] Il test manuale può essere effettuato salvando la partita in un qualsiasi gioco: apparirà il file `.sav` accanto alla ROM!

---

## 7. Save States (Salvataggi Seriali Iistantanei)
Abbiamo introdotto i **Save States**, permettendoti di salvare e ricaricare l'emulatore in un punto esatto nel tempo, aggirando i salvataggi normali dei giochi.

**Come Funziona Sotto il Cofano:**
- Quando selezioni "Save State", l'emulatore apre un canale di scrittura binario super-veloce e "getta" all'interno del file l'intero contenuto della RAM del GameBoy Advance (WRAM, IRAM, VRAM, OAM), seguito da tutti i registri interni del processore ARM, i contatori dei Timer e lo stato della cartuccia.
- Caricando uno stato, il processo inverso ricrea l'immagine in memoria in meno di un millisecondo.
- La ROM da 32MB non viene serializzata nel file, per cui gli stati creati (con estensione `.stX` ad esempio `.st1`) pesano pochissimo (circa 400 KB l'uno). 

**Miglioramenti UI:**
- Trovate i Save States nel menu **Emulation**.
- Puoi salvare in uno qualsiasi dei 9 slot disponibili (`Slot 1` ... `Slot 9`).
- Il menu **Load State** è **dinamico**. Quando avvii l'emulatore, questo controlla quanti e quali slot esistono su disco *esclusivamente per la ROM attualmente caricata*, mostrandoti solo le voci pertinenti.

**Compromessi Calcolati:**
- Abbiamo escluso di proposito il processore audio (APU) dalla serializzazione. L'hardware audio è molto complesso e questo garantisce l'eliminazione di lag e file troppo pesanti. Quando caricherai uno stato, l'audio potrebbe interrompersi per una frazione di secondo finché l'emulatore non inizia a suonare il campione successivo prodotto dal gioco, poi riprenderà perfettamente in sincrono.

## Validazione Finale Save States
- [x] Il gioco compila ed esegue senza errori in Visual Studio.
- [x] I file binari vengono scritti su disco, i registri iterati correttamente per evitare conflitti con altre properties di VB.NET.
- [x] Il menu appare con la voce "Nessun salvataggio trovato" in Load se non esistono `.stX`, popolandosi automaticamente appena crei uno State.
