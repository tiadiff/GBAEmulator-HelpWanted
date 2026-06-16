# LCD VRAM Mapping & Addressing Plan

Analizzando il documento del GBATEK sulla VRAM, l'emulatore attualmente alloca correttamente 96KB (`98304` byte) di VRAM, e mappa esattamente i 1024 byte di Palette RAM e OAM. 
Tuttavia, sono emersi **due bug critici** nel modo in cui la VRAM viene indirizzata in casi limite e nelle scritture a 8-bit.

## Problemi Identificati

1. **Scritture a 8-bit in VRAM (BG Mode 3, 4, 5)**
   - Attualmente, `GBACore.Memory.vb` ignora *qualsiasi* scrittura a 8-bit oltre l'indirizzo `0x06010000` (`a >= &H10000`), trattando tutto come "OBJ VRAM".
   - Tuttavia, nei BG Mode 3, 4 e 5, la porzione `0x06010000 - 0x06013FFF` fa parte del **Frame Buffer** (o della porzione estesa del Frame 0 in Mode 3), e le scritture a 8-bit qui *devono* essere accettate (e specchiate a 16-bit). La vera OBJ VRAM per questi mode inizia a `0x06014000`.

2. **Wraparound della OBJ VRAM fuori dai limiti (Sprite molto grandi)**
   - Quando uno Sprite molto largo/basso sforacchia i limiti della sua memoria (offset > 32KB), l'emulatore fa: `vramAddr = vramAddr Mod 98304`.
   - Questo errore logico fa in modo che lo Sprite "torni a zero" e legga grafica dalla **BG VRAM** (`0x06000000`), causando glitch grafici enormi.
   - Il GBATEK afferma esplicitamente che lo spazio `0x06018000+` specchia semplicemente `0x06010000`. Quindi lo spazio degli sprite è un buffer circolare di 32KB. Va usato `And &H7FFF` sull'offset per vincolarlo alla sua memoria, e non farlo mai scivolare nella BG VRAM.

## Proposed Changes

1. **GBACore.Memory.vb (`Write8`)**
   - Nel `Case &H6` (VRAM), determineremo il BG Mode attuale leggendo `IO(0) And 7`.
   - Modificheremo il check di ignoro: se `bgMode >= 3`, la soglia per l'OBJ VRAM diventerà `&H14000` (81920). Altrimenti resterà `&H10000` (65536).

2. **GBACore.Graphics.vb (`RenderSprites` e `BuildObjWindowPixels`)**
   - Rimuoveremo la vecchia riga `If vramAddr >= 98304 Then vramAddr = vramAddr Mod 98304`.
   - Modificheremo il calcolo per usare sempre il modulo sui 32KB:
     `Dim vramAddr = &H10000 + (((tOff * 32) + (ty * 8) + tx) And &H7FFF)` (per 8bpp)
     `Dim vramAddr = &H10000 + (((tOff * 32) + (ty * 4) + (tx \ 2)) And &H7FFF)` (per 4bpp)
   - Questo garantirà che qualunque tile-index sfori il limite, continui a ciclare e "pescare" unicamente all'interno della OBJ VRAM, rispettando il mirroring hardware GBA.

## Domande per te
Procedo con la correzione delle dinamiche di wraparound e delle scritture 8-bit in VRAM?
