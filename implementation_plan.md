# Goal Description
Inserimento di impostazioni **Avanzate** nell'emulatore. Il focus è spostato su funzionalità per sviluppatori, speedrunner e appassionati dell'accuratezza o del modding.

## Open Questions

> [!QUESTION]
> **Quali di questi Advanced Settings preferisci?**
> Dal momento che cercavi qualcosa di più avanzato, ecco una lista di idee "hardcore" per l'emulatore:
> 
> 1. **GBA LCD Color Correction (Video):** I giochi GBA erano programmati con colori molto accesi perché lo schermo originale (AGS-001) era poco luminoso. Sui monitor moderni risultano troppo saturi. Questa opzione applicherebbe un filtro matematico (Color Matrix) per desaturare e correggere i colori, simulando il vero schermo LCD del Game Boy Advance.
> 2. **Hardware Sprite Limit (Emulazione/Video):** Il GBA reale disegna un massimo di 10 sprite (OBJ) per linea, causando "sfarfallio" (flickering) nei giochi concitati. Aggiungiamo un toggle: se abilitato rispetta il limite hardware (accuratezza), se disabilitato permette sprite infiniti per linea eliminando i cali grafici tipici del GBA originale.
> 3. **Audio Channel Muting (Audio Debug):** 6 Checkbox indipendenti per mutare/isolare i singoli canali hardware (Pulse 1, Pulse 2, Wave, Noise, DMA A, DMA B). Estremamente utile per chi studia le colonne sonore o debugga.
> 4. **Fast-Forward Speed & Key (Sistema):** Definire la velocità del Fast-Forward (es. 2x, 4x, 10x, Uncapped) e assegnare un tasto dedicato sulla tastiera.
> 5. **Force Save Type (Memoria):** Di default l'emulatore rileva il tipo di salvataggio (SRAM, EEPROM, Flash). Questa opzione avanzata forza il tipo in caso di ROM modificate, homebrew o hack che ingannano l'auto-detect.

Sei d'accordo con queste proposte avanzate? C'è un ambito specifico (es. CPU, Memoria o Grafica) in cui vorresti spingerti ancora oltre?

## Proposed Changes

---

### [MODIFY] ConfigManager.vb
Aggiunta alla classe `AppConfig` di:
- `ColorCorrection As Boolean = False`
- `EnforceSpriteLimit As Boolean = True`
- `AudioChannelMask As Integer = &H3F` (Bitmask per i 6 canali)
- `FastForwardMultiplier As Integer = 0` (0 = Uncapped, 2 = 200%, ecc.)
- `ForceSaveType As Integer = 0` (0 = Auto, 1 = SRAM, 2 = EEPROM, 3 = FLASH64, 4 = FLASH128)

### [MODIFY] SettingsForm.vb
- Aggiunta di un nuovo tab **"Avanzate"** e integrazione delle opzioni nei tab esistenti:
  - Tab "Video": Checkbox "GBA LCD Color Correction" e "Enforce Hardware Sprite Limit".
  - Tab "Audio": Nuova sezione "Channel Mixer" con 6 Checkbox.
  - Tab "Sistema": Menu a tendina per il Fast-Forward Speed e per il Save Type.

### [MODIFY] Core Modules (GBACore.PPU, GBACore.APU)
- **APU:** Modificare il mix dell'audio moltiplicando il volume dei singoli canali in base al bit corrispondente in `Config.AudioChannelMask`.
- **PPU:** Introdurre il limite di sprite nel renderizzatore scanline (`RenderOBJ`) interrotto se superati i cicli limite. Aggiungere il mapping LUT per la Color Correction nel master output se l'opzione è attiva.
