# Render pipeline materials

Backrock packs that include a `RenderPipelines` folder ship their materials
for the Built-in pipeline, URP and HDRP. The pack's own `Materials` folders
hold them on URP Lit; the pack's `RenderPipelines` folder holds them again as
`<Pack>_BuiltIn.unitypackage` (Standard), `<Pack>_URP.unitypackage` (URP Lit)
and `<Pack>_HDRP.unitypackage` (HDRP Lit), under the same names and GUIDs.

`Editor/BackrockPipelineMaterials.cs` keeps them on the pipeline your project
uses: a moment after a pack is imported, after scripts reload, and when the
project's render pipeline changes, it copies each shipped material's version
for that pipeline over it, in place, so the models, prefabs and scenes follow.
It only rewrites the materials those packages ship, wherever you have moved
them, and only ones still on Standard, URP Lit or HDRP Lit (or whose shader is
missing). A shipped material you have put on a shader of your own, and every
material of your own, is left alone. Edits to a shipped material last until
the project changes pipeline.

**Tools > Backrock > Render Pipeline Materials**: Use Built-in, URP or HDRP
Materials puts every pack's materials on that pipeline and keeps them there,
whatever the project uses; Match Active Pipeline turns the automatic matching
back on. Double-clicking one of the packages imports all of its materials,
and the automatic matching then puts them back on the project's pipeline
unless a set was chosen from the menu.

Packs that include this folder share one copy of it.
