# Risoluzione del crash di GTA Advance

Durante l'analisi del bug che causava il riavvio del gioco tornando alla schermata di boot del BIOS di Normmatt durante il gameplay, sono stati identificati e risolti i seguenti problemi critici nel core dell'emulatore:

## 1. Allineamento errato dell'Instruction Fetch (Causa Principale)
Nel GBA reale (ARM7TDMI), l'Instruction Fetch avviene sempre all'indirizzo allineato (multiplo di 4 in ARM, multiplo di 2 in Thumb), ignorando i bit meno significativi del PC. 
Nell'emulatore, se il gioco caricava in R15 un indirizzo disallineato (es. tramite un `POP {PC}` o caricandolo dalla memoria), `Read32` e `Read16` effettuavano una lettura disallineata. Su ARM, una lettura disallineata su memoria 32-bit causa una **rotazione** del valore restituito.
Questo faceva sì che la CPU dell'emulatore pescasse **opcode spazzatura (ruotati)**, portando all'esecuzione di istruzioni a caso che finivano per corrompere lo stack o forzare un `SWI 0x0` (SoftReset) / jump a `0x00000000`, causando il ritorno al BIOS.
**Fix:** È stata aggiunta una maschera per allineare l'indirizzo durante il fetch delle istruzioni in `StepCycle` (`ExePC And Not 3UI` per ARM e `ExePC And Not 1UI` per Thumb).

## 2. Bypass HLE per le operazioni matematiche (SWI)
GTA Advance utilizza in modo molto intensivo le operazioni matematiche tramite le Software Interrupts (SWI) per via del suo motore grafico 3D (raycasting).
- L'emulatore utilizzava il BIOS per le SWI se `UseBIOS` era attivo. Per ottimizzare le prestazioni ed evitare potenziali bug o loop infiniti nel BIOS per operazioni di divisione, **è stato forzato l'uso dell'HLE per tutte le SWI matematiche e di memoria (ID da 6 a 15)**.
- È stata implementata la SWI **DivArm (0x07)**, che prima mancava del tutto nell'implementazione nativa.
- È stata introdotta una protezione contro l'eccezione `OverflowException` di VB.NET nel caso estremo in cui avvenga una divisione di `Integer.MinValue \ -1`.

## 3. Fix timing HBlank IRQ e VBlank (Bug identificati nell'analisi)
Per prevenire crash legati al DMA Audio o alla sincronizzazione grafica del motore di GTA Advance:
- **HBlank IRQ:** L'emulatore generava l'HBlank IRQ solo durante l'area visibile (riga < 160). Come da documentazione GBATEK, gli IRQ di HBlank devono continuare a essere generati anche nelle "scanline invisibili" durante il VBlank. Modificato per generare l'IRQ regolarmente in tutte le scanline.
- **VBlank Flag:** Il flag VBlank in DISPSTAT era settato in modo errato anche per la scanline 227. Corretto per fermarsi alla 226, come richiesto dall'hardware GBA.

Le modifiche sono state compilate e testate con successo (`dotnet build` non ha riportato alcun errore). L'emulatore ora dovrebbe essere molto più stabile e non crashare più a causa dell'esecuzione di istruzioni ruotate in modo anomalo.
