# Figma-like Feel — Feature Checklist

Mark items with `[x]` for "please implement" or leave `[ ]` for "skip / later".
Add notes inline. I'll pick up whatever is checked in order top-to-bottom.

Legend:

- **Effort**: S (≤2h), M (≤1d), L (multi-day)
- **Depends**: features that must land first
- Verification: every item comes with new tests that hit the real code path

---

## Phase 1 — Cheap wins (Figma-like ergonomics)

- [ ]  **Grid snap** — toggle with `G`, size field in palette window (default 1u). Cursor snaps to nearest grid intersection before paint.
Effort: S. Depends: —.
- [x]  **Arrow-key nudge** — arrow keys move currently-selected placed objects by 1u; `Shift` = 10u; `Ctrl` = 0.1u.
Effort: S. Depends: —.
- [ ]  **Space+drag = pan camera** — mirror Figma; complements existing MMB pan.
Effort: S. Depends: —.
- [x]  **`V` = select mode** — deselect palette, restore Unity's move tool. Quick way out of paint mode.
Effort: S. Depends: —.
- [x]  **`Alt`+click in scene = duplicate last painted** without needing to re-open palette.
Effort: S. Depends: —.
- [ ]  **Palette search box** — filter entries by name substring at top of window.
Effort: S. Depends: —.
- [x]  **Recent / favorites row** — last 5 painted at top; pin via right-click.
Effort: S. Depends: search box (shares palette-window refactor).
- [x]  **Status bar** — cursor world coords, selected entry name, paint target path, grid on/off. Small footer strip in palette window.
Effort: S. Depends: —.
- [x]  **Drag-to-paint spacing** — while LMB held, paint a new object every N world units of cursor travel (configurable, default 1u).
Effort: S. Depends: —.

---

## Phase 2 — Smart placement (the biggest Figma feel jump)

- [ ]  **Object snap** — snap cursor to bounding-box edges/centers of nearby placed objects. Threshold configurable in pixels.
Effort: M. Depends: —.
- [ ]  **Alignment guides** — magenta dashed lines when preview aligns with any placed object's center/edge, exactly like Figma's smart guides.
Effort: M. Depends: object snap (shares bbox math).
- [x]  **Alt+drag existing object to clone** — grab an already-painted object with Alt held; drag creates a duplicate at cursor. Same UX as Figma.
Effort: M. Depends: —.
- [ ]  **Numeric HUD near cursor** — floating panel showing X / Y / rotation of the preview; editable inline.
Effort: M. Depends: —.
- [x]  **Undo stroke grouping** — one Ctrl+Z reverts the whole click-drag sweep, not each object separately.
Effort: M. Depends: drag-to-paint spacing.
- [x]  **Palette categories / folders** — collapsible sections in palette window per subfolder of the palette source. Keeps 67-entry palette scannable.
Effort: M. Depends: —.
- [ ]  **Hover preview on existing objects** — dashed outline + prefab-name tooltip when hovering scene objects in paint mode.
Effort: M. Depends: —.
- [x]  **Drag-out to rotate** — after a paint, hold RMB and drag to rotate the just-painted object around its origin (like Figma's rotation from bounding-box corner).
Effort: M. Depends: —.

---

## Phase 3 — Real workflow features

- [ ]  **Layer system** — proper Figma Frames-style layers:
    - Named layer roots with visibility + lock toggles
    - Active-layer picker (drop-down or breadcrumb)
    - Isolate mode: dim/hide all layers except active
    - "Show all" toggle
    This matches the layers you saw in the acoppes README video.
    Effort: L. Depends: —.
- [ ]  **Camera-locked preview overlay** — floating mini scene view that shows all layers even when one is isolated. The other half of the video's UX.
Effort: L. Depends: layer system.
- [ ]  **Palette browser panel** — grid of thumbnails with search, tags, drag-in-from-Project to add prefabs, in-place override tweaking. Turn the palette into a first-class asset library.
Effort: L. Depends: palette categories.
- [ ]  **Placement primitives:**
    - [ ]  Line — paint N objects between click and release
    - [ ]  Radial — N objects around a center
    - [ ]  Rectangle fill — stamp NxM grid
    - [ ]  Path — bezier path with objects along it
    Effort: L (per primitive). Depends: —.
- [x]  **Copy-properties across selection** — Figma's "copy properties" applied to multiple painted objects.
Effort: M. Depends: —.
- [ ]  **Marquee-select painted objects** in paint mode without leaving the tool. Applies actions to the whole selection.
Effort: M. Depends: —.

---

## Housekeeping (not Figma-feature but tool health)

- [x]  **Palette window UI Toolkit rewrite** — replace IMGUI with UI Toolkit. Better performance, more Figma-like styling, easier testing.
Effort: L. Depends: —. (One-time investment that speeds everything above.)
- [x]  **Preferences panel** — Preferences > Object Palette: default grid size, snap tolerance, hotkey step sizes, palette pinning, etc.
Effort: S. Depends: several Phase 1 features that introduce settings.
- [x]  **Undo integration audit** — verify every action (paint, erase, rotate-during-paint) creates one clean undo entry that fully reverses.
Effort: S. Depends: —.

---

## Recommended sequence

If you check nothing else, at least these give the biggest jump toward Figma feel per hour spent:

1. Grid snap + Arrow-key nudge + Space+drag pan (Phase 1)
2. Object snap + alignment guides (Phase 2)
3. Layer system (Phase 3) — solves your 3-depth-layer workflow

---

## What already works (won't reimplement)

- [x]  Palette window with grid of prefab thumbnails
- [x]  Click palette entry → preview follows cursor
- [x]  LMB to paint at cursor
- [x]  `E` toggles erase mode
- [x]  `[` / `]` rotate ±15°
- [x]  / `=` scale ±10%
- [x]  `0` reset rotation & scale
- [x]  Overrides on preview (Inspector) carry into every painted instance
- [x]  Paint target picker (persistent across sessions)
- [x]  Layer visibility toggles for sibling roots (basic version)
- [x]  Hotkeys work as global shortcuts (no need to click scene view)
- [x]  MMB reserved for camera pan
- [x]  35-test suite covering paint flow, overrides, hotkeys, event routing, multiply modifier, persistence