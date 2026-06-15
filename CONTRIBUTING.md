# Contributing to VB-GBA-HelpWanted

First off, thank you for considering contributing to VB-GBA-HelpWanted! It's people like you that make open-source projects great.

## 🗺️ Development Roadmap & Areas to Help

We have made massive progress, implementing core CPU execution, PPU graphics, APU sound, save systems, and a complete debugger suite. However, to achieve a fully complete and accurate GBA emulator, we need help in the following areas:

1. **Cycle-Accurate Timings & Memory Waitstates**:
   - Simulation of sequential (S) and non-sequential (N) memory access cycles based on GBATEK.
   - Simulation of the CPU Prefetch Buffer.
2. **Accurate DMA Stalling**:
   - Implementing proper CPU stalling during DMA transfers instead of running them instantly.
3. **PPU (Graphics) Edge Cases**:
   - Fixing specific rendering edge cases, such as custom ObjWindow clipping boundaries and blending behavior.
4. **Real Time Clock (RTC)**:
   - Implementing hardware RTC emulation for games like Pokémon (Ruby/Sapphire/Emerald) which rely on real-world time.
5. **Input & Gamepad Support**:
   - Adding gamepad/controller support via XInput or DirectInput.
6. **Testing & Verification**:
   - Testing GBA test suites (e.g., armwrestler, mGBA tests) and identifying failing instructions, timings, or memory accesses.
7. **Performance & Refactoring**:
   - Optimizing execution loops, profiling VB.NET rendering overhead, and general codebase cleanup.

## How to Submit Changes

1. **Fork the repository**
2. **Create a new branch** (`git checkout -b feature/my-awesome-feature`)
3. **Commit your changes** (`git commit -m 'Add some feature'`)
4. **Push to the branch** (`git push origin feature/my-awesome-feature`)
5. **Open a Pull Request**

## Comunica con noi

Sentiti libero di aprire una **Issue** o una **Discussion** (se abilitata) nel repository per discutere di cosa vorresti implementare o se hai bisogno di aiuto per capire l'architettura.

