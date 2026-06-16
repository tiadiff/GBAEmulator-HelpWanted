# Implement Accurate OAM Cycle Limits

- [x] Add `EvaluateSpriteLimits` phase at the beginning of `RenderFrame`.
- [x] Compute OAM evaluation limits based on scanlines, separating affine from normal sprites.
- [x] Use `SpriteVisibleMask(127, 159)` to strictly cull sprites before rendering them via Painter's Algorithm.
- [x] Remove the incorrect pixel-based `ObjPixelsRendered` limit that erroneously dropped high-priority sprites.
