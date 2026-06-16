# Walkthrough: OAM Limits e Bitmap Modes

## Risposta alla tua domanda sui Limiti Sprite:
Mi hai chiesto se la limitazione del numero massimo di sprite renderizzabili (1210 o 954 cicli) si applicasse anche agli sprite *verticalmente fuori dallo schermo* (es. sprite disegnati alla scanline 200, mentre il GBA sta renderizzando la 50).
La risposta è **assolutamente no**. L'hardware del GBA spende una manciata irrisoria di cicli nella primissima scansione della OAM per "scartare" al volo gli sprite che non intersecano orizzontalmente l'attuale scanline. I veri e propri cicli esosi che erodono il limite (`n*1` o `10+n*2`) vengono consumati unicamente dai pixel prelevati in memoria *per quella specifica scanline*.

## Bug Architetturale Corretto

Leggendo proprio il passaggio dei limiti ciclici OAM che hai postato, mi sono reso conto che l'emulatore possedeva una gestione del limite degli sprite **gravemente inesatta**:
In passato, l'emulatore limitava a brutto muso il numero di pixel disegnati fermandosi a quota "960" all'interno della `RenderSprites`. Dato che noi usiamo il Painter's Algorithm (i layer vengono disegnati dal basso verso l'alto, quindi le Priorità più basse 3-2-1 vengono sovrascritte da quelle alte 0), gli sprite con priorità massima venivano elaborati *per ultimi* in loop! 
Di conseguenza, se una scanline si riempiva, **venivano tagliati i layer ad alta priorità** (completamente l'inverso del GBA reale, in cui contava strettamente l'ordine di slot OAM da 0 a 127).

### 1. Nuova Scansione Preliminare (OAM Cycle Evaluation)
Ho introdotto una fase iniziale `EvaluateSpriteLimits` a inizio frame. Prima ancora di cominciare a disegnare il background, l'emulatore ora simula il passo di valutazione OAM riga per riga:
- Esegue un loop rigoroso da Sprite 0 a Sprite 127 (la priorità nativa in termini di esecuzione hardware).
- Somma cicli reali (costo fisso o costo dinamico affine di `10 + n*2`).
- Compila una matrice binaria bidimensionale `SpriteVisibleMask(127, 159)`.

### 2. Disegno con Ritaglio Accurato
Durante le 4 passate di rendering del Painter's Algorithm in `RenderSprites`, controlliamo istantaneamente la maschera: `If Not SpriteVisibleMask(i, yD) Then Continue For`.
Ora l'emulatore troncherà sempre in modo corretto gli sprite a fine memoria OAM (127..126..), indipendentemente dalla loro priorità visiva di Z-Index! Questa correzione era indispensabile per titoli pesanti sotto l'aspetto dell'action (sparatutto, platformer affollati) che sfruttano il clipping naturale.

## Bitmap Modes
Per quanto concerne i *Bitmap BG Modes* menzionati nel tuo testo (3, 4, 5): l'architettura implementata in precedenza in `RenderBitmapMode` era già perfettamente aderente a quanto citato. Modalità 3, frammentazione del Frame 1 in Mode 4/5 all'offset `0x0600A000` e dimensioni di 37.5 KBytes sono tutte matematicamente calcolate nei nostri mapping di memoria. Anche qui, non sono stati necessari interventi!
