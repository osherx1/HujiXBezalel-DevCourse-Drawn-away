# Line Drawing with Physics (Love Balls-style)

This project adds a simple line-drawing mechanic using LineRenderer + 2D colliders so the drawn strokes interact with physics, like in Love Balls.

## What you get
- `Line.cs`: A component that renders a line as you draw and generates either an EdgeCollider2D (thin, 1D) or a PolygonCollider2D (thick, with width) so physics objects can sit on the stroke.
- `LineManager.cs`: Listens for mouse/touch input, spawns `Line` objects, and feeds points as you drag.

## Unity setup (2D)
1. Import the two scripts into `Assets/Scripts/`:
   - `Line.cs`
   - `LineManager.cs`
2. Create an empty GameObject in your scene named `LineManager` and attach `LineManager` script.
3. (Optional) Create a Material for the LineRenderer:
   - Right-click in Project > Create > Material, name it `Line_Mat`.
   - Shader: `Sprites/Default` (simple, works in 2D), set color as you like.
   - Assign it to `LineManager > lineMaterial` field.
4. Configure `LineManager` in Inspector:
   - `lineWidth`: visual width of the stroke (also used for collider thickness if polygon mode is on).
   - `minDistance`: minimum distance between recorded points (helps performance and smoothness).
   - `usePolygonCollider`: ON = solid stroke with thickness. OFF = thin edge only.
   - (Optional) `physicsMaterial2D`: set friction/bounciness for the stroke.
5. Ensure you have a 2D camera and physics world (e.g., `Main Camera` with Orthographic projection).
6. Press Play and draw with the mouse (or touch on mobile).

## Notes
- PolygonCollider2D: gives the stroke actual thickness so rigidbodies can rest on it. More expensive than EdgeCollider2D, but better for Love Balls-style gameplay.
- EdgeCollider2D: cheaper, but no thickness. Objects can slip through if your lineWidth is just visual.
- Sorting: You can control render order via `Line` component's `sortingLayerName` and `sortingOrder`.
- Finalize: `Line.FinalizeLine(makeDynamic)` can switch the Rigidbody2D from static to dynamic after drawing if you want the line to fall.

## Performance tips
- Keep `minDistance` reasonably high (e.g., 0.05–0.1 world units) to reduce points.
- For long draws, consider smoothing or decimating points after the stroke completes.
- Pool line GameObjects if you create/destroy many in a session.

## Extending
- Add stroke lifetime: destroy the line after N seconds.
- Add color picker and different materials.
- Add eraser tool (raycast for colliders created by lines and destroy them).
- Save/load strokes by serializing the point lists.

## Troubleshooting
- If objects pass through the line, try:
  - Using PolygonCollider2D mode.
  - Increasing line width.
  - Increasing Rigidbody2D velocity iterations and collision detection (Continuous) on moving objects.
- If drawing flickers or looks offset, check your camera’s Z and the line’s Z positions; we always write points at Z=0.
- If nothing draws, make sure `Main Camera` exists and `EventSystem` UI doesn’t block input.
