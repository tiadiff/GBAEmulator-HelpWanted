# VB-GBA-HelpWanted 🎮

VB-GBA-HelpWanted is an experimental Game Boy Advance (GBA) emulator written entirely in **VB.NET**. 

This project has recently made massive progress in its development! The core components (CPU, Memory, Graphics) are highly functional, with recent fixes enabling complex 3D raycasting games like **GTA Advance** to run smoothly without crashing.

<img width="1910" height="1006" alt="Screenshot 2026-06-15 173153" src="https://github.com/user-attachments/assets/0e84429d-7c68-4e39-8b4d-1d993b645d2d" />

## 🤝 We Need Your Help! (Cercasi Contributori!)

This project is a work in progress and we are looking for passionate developers, retro-gaming enthusiasts, and VB.NET wizards to join the effort! 

Whether you're an expert in emulator development or simply want to learn more about the GBA architecture, your contributions are welcome. We still need help with:
- Perfecting PPU (Graphics) edge cases and ObjWindow clipping
- Improving audio (APU) DMA synchronization
- Further general debugging and refactoring

### 🌟 Recent Milestones Achieved:
- ✅ **Game Saves:** Full support for EEPROM, SRAM, and Flash memory backup (.sav files) with background auto-save.
- ✅ **Save States:** Functionality to save and load instantaneous emulator states (up to 9 slots).
- ✅ **UI & UX Improvements:** Drag & drop ROM loading, accurate Window Scaling (1x-4x), Pause/Resume, and smart audio mute on background.
- ✅ Fixed compilation errors related to array syntax formatting (VB.NET trailing commas).
- ✅ Fixed CPU Instruction Fetch alignment (resolved crashes to BIOS)
- ✅ Implemented fast HLE for Math SWIs (Div, DivArm, Sqrt) bypassing BIOS bugs
- ✅ Fixed CycleCount accumulator preventing infinite loops in heavy ARM games
- ✅ Corrected HBlank IRQ generation and VBlank DISPSTAT timings
- ✅ **Advanced PPU Features:** Complete Color Special Effects (Alpha Blending, Brightness increase/decrease) and Window clipping.
- ✅ **Full APU (Audio):** Complete implementation of PSG channels 1-4 (Square, Wave, Noise) and accurate DirectSound mixing, including isolated channel toggling.
- ✅ **Debugger Suite:** Added a comprehensive set of resizable debug windows (CPU Disassembler, APU Viewer, VRAM & OAM Viewer, Tilemap Viewer, IO Registers, etc.).
- ✅ **Advanced Emulator Settings:** Introduced "Hardcore" settings such as Hardware Sprite Limit enforcing, GBA LCD Color Correction matrix, Fast-Forwarding, and manual Save Type override.

## 🗺️ Development Roadmap (What's Missing)
To achieve a fully complete and accurate GBA emulator, the following features and systems still need to be implemented:
- **Cycle-Accurate Timings:** Implementing Memory Waitstates and CPU Prefetch Buffer simulation.
- **Accurate DMA:** Stalling the CPU correctly during DMA transfers rather than executing them instantly.
- **Game Saves & RTC:** Real Time Clock (RTC) for games like Pokémon (Hardware saves are already implemented!).
- **Input:** Gamepad/Controller support.

Check out our [CONTRIBUTING.md](CONTRIBUTING.md) to see how you can get involved.

## 🚀 Getting Started

### Prerequisites
- Visual Studio 2022 (or newer) with .NET desktop development workload
- Windows OS (for WinForms UI)

### Building
1. Clone the repository: `git clone https://github.com/tiadiff/VB.NET-GBAEmulator-HelpWanted.git`
2. Open `WinFormsApp1.sln` in Visual Studio
3. Build the solution (Ctrl+Shift+B)
4. Run the project (F5)

## 📁 Project Structure
- `GBACore.vb`: Main emulator core class
- `GBACore.CPU.*.vb`: ARM7TDMI CPU implementation
- `GBACore.Memory.vb`: Memory Map and Memory Access
- `GBACore.Graphics.vb`: PPU Implementation
- `Form1.vb`, `DebuggerForm.vb`, `MemoryViewerForm.vb`: WinForms UI and Debugging Tools
