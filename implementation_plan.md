# Implementation Plan: SaveState Manager Form

## Proposed Changes

1. **New UI Component: SaveStateManagerForm**
   - Create `UI/SaveStateManagerForm.vb`. This form will contain a `ListBox` or `ListView` to display `.sav` files found in the same folder as the currently loaded ROM.
   - It will have buttons to "Refresh", "Delete", and "Backup" (or rename) the selected `.sav` files.

2. **Form1 Integration**
   - Add a `Save State Manager` menu item to the `Debug` menu (or a suitable menu) in `Form1.vb`.
   - Pass the currently active ROM directory to the `SaveStateManagerForm` when opened.

3. **vb-gba.vbproj**
   - Add `SaveStateManagerForm.vb` and its designer/resx to the VB project structure so it compiles correctly.

## Verification
- Run the application.
- Open the Save State Manager form.
- Verify it lists `.sav` files in the ROM directory.
- Verify deletion works correctly.
