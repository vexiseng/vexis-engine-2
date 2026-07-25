VEXIS ENGINE 2 — STEP 1: RENDERING ARCHITECTURE

This package adds the renderer-independent foundation only.
It intentionally does NOT replace the existing software preview yet.

Added:
- src/Vexis.Rendering
- tests/Vexis.Rendering.Tests
- Graphics backend selection
- Graphics-device interface
- Renderer-independent camera math
- Render scene, mesh, material, light, frame, and viewport types
- Software graphics-device contract implementation
- Camera and backend-selection tests

Apply:
1. Extract this ZIP anywhere.
2. Open PowerShell in the root of your updated vexis-engine-2 repository.
3. Run:

   powershell -ExecutionPolicy Bypass -File "<extracted folder>\APPLY_STEP_1.ps1"

The script copies the files, updates Vexis.Engine2.slnx, builds, and runs tests.

Suggested commit:

git add .
git commit -m "feat: add renderer abstraction and camera foundation"
git push

After pushing, tell ChatGPT the repository is updated. Step 2 will add the actual Vortice Direct3D 11 device, swap chain, render target, depth buffer, clear, resize, present, and disposal foundation.

Validation note:
This package was structurally prepared against the repository version inspected on 2026-07-24. The current environment does not have the .NET SDK, so the included script performs the authoritative build and test on your machine before you commit.
