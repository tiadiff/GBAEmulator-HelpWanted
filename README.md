# VB-GBA-HelpWanted 🎮

VB-GBA-HelpWanted is an experimental Game Boy Advance (GBA) emulator written entirely in **VB.NET**. 

This project is currently in its early stages of development. The core components (CPU, Memory, Graphics) are being actively worked on, but there is still a long way to go to achieve full compatibility and accurate emulation.

## 🤝 We Need Your Help! (Cercasi Contributori!)

This project is a work in progress and we are looking for passionate developers, retro-gaming enthusiasts, and VB.NET wizards to join the effort! 

Whether you're an expert in emulator development or simply want to learn more about the GBA architecture, your contributions are welcome. We need help with:
- Implementing missing ARM7TDMI CPU instructions (ARM & Thumb)
- Refining the PPU (Graphics) and memory mapping
- Improving timing and synchronization
- Implementing audio (APU)
- General debugging and refactoring

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
