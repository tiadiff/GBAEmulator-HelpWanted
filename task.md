# Miglioramento Qualità Audio GBA

- `[x]` Implementare `fsCycleAccumulator` in `GBACore.APU.vb`
- `[x]` Creare la funzione `StepFrameSequencer()` per gestire gli orologi a 512Hz
- `[x]` Aggiornare le classi PSG (`PulseChannel`, `NoiseChannel`) per supportare l'Envelope Step
- `[x]` Implementare il Filtro Passa-Basso IIR (Low-Pass Filter) su Left e Right in `GenerateSample()`
- `[x]` Ridurre il gain del Mixer (moltiplicatore) per evitare hard clipping a 16-bit
- `[ ]` Compilare e verificare l'assenza di distorsioni
