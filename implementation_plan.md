# GBA Emulator Performance and Rendering Fixes

This plan addresses the performance and memory issues caused by WinForms `Timer` and continuous `Bitmap` instantiations.

## Proposed Changes

### 1. `Form1.vb`
- **Replace WinForms `Timer`**: Remove `GameLoop` timer. Introduce a background `Thread` (`EmulationThread`) that will execute the emulator loop.
- **Implement 60 FPS Syncing**: Inside the background thread, use a `Stopwatch` to measure frame time and sleep if the emulation finishes a frame in less than 16.6ms.
- **Single Bitmap Rendering**: Create a single `Bitmap` object (`DisplayBitmap`) at startup and assign it to `ScreenBox.Image`.
- **Thread-safe UI Updates**: Inside the emulation thread, after `StepCycle()` signals a `V-Blank`, we will call `BeginInvoke` on the form to run `LockBits`, update the `DisplayBitmap` pixels using `Marshal.Copy`, and call `ScreenBox.Invalidate()`. This ensures thread safety and zero GC pressure.

### 2. `GBACore.Graphics.vb`
- **Preallocate Buffers**: Move `pixels()`, `winMask()`, and `objWinPixels()` from local variables to class-level `Private` arrays. This avoids creating thousands of arrays every second, completely fixing the Garbage Collector issue.
- **Update `RenderFrameFast`**: Change the method signature to `Public Sub RenderFrame(outPixels() As Integer)`. Instead of creating and locking a `Bitmap`, it will render the frame directly into the provided `outPixels` array.
- **Refactor Helper Methods**: Update `BuildWindowMask` and `BuildObjWindowPixels` to use the preallocated arrays.

### 3. `GBACore.vb`
- Initialize the new internal arrays in `ResetCore()` if necessary, ensuring no leftover garbage pixels are drawn on reset.

## User Review Required

> [!IMPORTANT]
> - Moving the emulation loop to a background thread will significantly improve performance but requires locking mechanisms if we add UI features that pause/modify emulator state concurrently. Is it okay to use `Me.BeginInvoke` for UI synchronization?
> - The rendering logic for `TileBG` and `Sprites` appears to be correct based on standard GBA specs (mapping and PaletteRAM extraction). If you still encounter black screens after these optimizations, it might be due to CPU/Memory timing bugs rather than the rendering code itself. Shall I proceed with these changes?
