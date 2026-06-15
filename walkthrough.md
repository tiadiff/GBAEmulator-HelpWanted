# Walkthrough: VB.GBA Emulator Advanced Settings

Sono stati implementati i **Settings Avanzati** che introducono un livello di controllo senza precedenti per chi vuole un'esperienza di emulazione ultra-personalizzabile. Tutte queste opzioni sono configurabili in tempo reale da `Opzioni -> Impostazioni...` e salvate automaticamente.

## 🎨 GBA LCD Color Correction
Lo schermo originale del GBA (modello AGS-001, quello non retroilluminato) offriva colori molto scuri e de-saturati. Per compensare, gli sviluppatori usavano palette estremamente vivaci. Giocando oggi su un monitor PC, i giochi possono sembrare troppo "accesi".
Abilitando **GBA LCD Color Correction** (nella tab Video), l'emulatore applica una Color Matrix personalizzata direttamente a livello di PPU. Questa funzione riduce la saturazione simulando l'autentica risposta del display LCD GBA originale!

> [!TIP]
> Provala con giochi famosi per la loro palette "neon" come Castlevania: Harmony of Dissonance per notare la drastica differenza.

## 👾 Hardware Sprite Limit Enforcer
Il vero GBA soffre di sfarfallio (flickering) in situazioni molto affollate (es. tanti nemici a schermo) perché l'hardware non riesce a elaborare o renderizzare un numero illimitato di sprite (limite hardware dei pixel OBJ per scanline).
- Se disabiliti questa opzione: Goditi un'esperienza "moderna", con sprite infiniti e assenza totale di flickering.
- Se abiliti questa opzione: L'emulatore torna all'accuratezza hardware, saltando il rendering dei pixel in eccesso per riga.

## 🎛️ Audio Channel Mixer (Muting Isolato)
Nella tab **Audio** trovi ora 6 interruttori dedicati per mutare indipendentemente i canali sonori del GBA:
- `Pulse 1` & `Pulse 2` (Onde quadre)
- `Wave` (Campionamenti personalizzati)
- `Noise` (Rumore bianco)
- `DMA A` & `DMA B` (Direct Sound per audio ad alta qualità e voci)

> [!NOTE]
> Utile per speedrunner o chiunque voglia estrarre tracce audio, capire come funziona un brano o ascoltare solo gli effetti sonori o le voci isolando la musica di sottofondo!

## ⏩ Fast-Forward (Avanzamento Rapido)
Giocare ai vecchi RPG non deve più significare combattere lentamente! 
Nella tab **Sistema** puoi scegliere il moltiplicatore (es. 2x, 4x o 10x) per il Fast-Forward. Vai poi nella tab **Controlli** per mappare un tasto dedicato (di default `Tab`).
Tenerlo premuto manipolerà il loop di sistema riducendo il tempo di sleep tra un frame e l'altro in tempo reale.

## 💾 Force Save Type
Talvolta ROM modificate, tradotte dai fan o giochi Homebrew ingannano il sistema di auto-rilevazione del tipo di salvataggio. Ora puoi ignorare l'autodetect ed impostare manualmente dalla tab **Sistema**:
- SRAM
- EEPROM
- FLASH64 / FLASH128

---
Tutto il codice è stato testato e compila alla perfezione. Puoi richiamare i nuovi setting aprendo `Opzioni -> Impostazioni...` nell'interfaccia principale. Buon divertimento!
