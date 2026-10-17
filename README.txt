GODLINGS — playable Unity demo

Clone this repository and open its root folder in Unity Hub using Unity 6000.6.6f1.
Scene: Assets/Godlings/Scenes/Demo.unity
Read PLAY-DEMO.txt for controls and build commands.
Read HANDOFF-TO-DOMINIK.txt for the complete continuation prompt.
Read CREATIVE-COVENANT.txt before making any creative changes.
Read STORY.txt for the approved storyline and themes.

Art/Originals contains untouched supplied artwork. Art/Characters holds crops.
Assets/Godlings/Art contains runtime character, enemy palette, meteor, and shadow sprites.
Art/Palettes and Art/asset-inventory.json record provenance and file hashes.
Design holds the digitized character/weapon tables and confirmed level specification.
Unity source, scene, metadata, package lock, and project settings are versioned here.
Builds and caches are excluded. The release ZIP is an optional historical backup.

Dominik owns creative direction and all supplied artwork. No art license is granted.
Do not generate art or invent creative content. Ask Dominik about creative decisions.

URP MIGRATION — 17 October 2026
Rendering now uses Universal Render Pipeline 17.6.0 with its 2D renderer
(Unity 6000.6.6f1). Gamma color space, artwork, projection, palette, sorting,
and gameplay are preserved. No lighting or post-processing is added.
Assets/Godlings/Rendering contains the pipeline, 2D renderer, and explicit
unlit materials. SpriteUnlit uses Unity's URP sprite shader; VertexColorUnlit
is a small URP shader for the existing colored floor and warning meshes.
Graphics Settings and every Quality level reference the same URP asset.
DemoBuilder.Prepare calls RenderingSetup.EnsureConfigured, then serializes
both material references into the scene so builds retain their shaders.
Keep all rendering assets and their .meta files together when pulling changes.
The optional screenshot smoke check uses URP's SubmitRenderRequest API.
In-app purchasing was unused: its deprecated package was removed rather than
adding purchase functionality. Monetization remains undecided by Dominik.

Windows builds: this Mac currently has only Mac and WebGL support installed.
Add Windows Build Support (Mono) for Unity 6000.6.6f1 through Unity Hub, then
select Windows x64 in Build Profiles and include Assets/Godlings/Scenes/Demo.unity.
Distribute the whole build folder, not only its .exe. Test on an actual Windows PC.
Windows IL2CPP builds require a Windows editor plus the native C++ toolchain.
