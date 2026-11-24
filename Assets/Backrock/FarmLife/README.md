# Farm Life Lite

2 animated low-poly farm animals, 6 breed variants. Every animal is one
rigged, animated model with several breeds, each its own base colour map;
every breed is a prefab.

## Contents

| Animal | Breeds | Breed names | Clips | Triangles | Size (W x H x D) |
| --- | --- | --- | --- | --- | --- |
| Chicken | 3 | WhiteLeghorn, RhodeIslandRed, BuffOrpington | Idle, Walk, Fly, Peck | 1232 | 0.60 x 0.47 x 0.53 m |
| Goat | 3 | Saanen, Alpine, Toggenburg | Idle, Walk, Run, Eat, Death | 1784 | 0.42 x 1.13 x 1.49 m |

Each animal's folder holds its model (`.fbx`, mesh, skeleton and clips), its
material (`Materials/`), its Animator Controller (one state per clip, `Idle` first), one base colour map
per breed (1024 x 1024) and a `Prefabs` folder with one prefab per breed.

## Using them

Drag a prefab from any `Prefabs` folder into a scene and press Play: the
controller is assigned and `Idle` plays. Switch clips with
`Animator.Play("Walk")` or transitions of your own. Looping clips
(Eat, Fly, Idle, Peck, Run, Walk) import with Loop Time on; one-shots (Death) play once.

**Changing breed.** Every prefab carries an `AnimalCoat` component listing all
of that animal's breeds. Pick one in the Inspector, or from code:

```csharp
var coat = GetComponent<Backrock.FarmLife.AnimalCoat>();
coat.SetCoat(2);       // by index
coat.RandomCoat();     // a mixed flock from one prefab
```

The breed is written into the model's material, which ships for the
Built-in pipeline, URP and HDRP (see Render pipelines below).

## Scenes

`Scenes/Showcase.unity` has every animal in every breed; press Play and the
camera tours the yard while each animal steps through its clips.
`Scenes/<Animal>.unity` shows one animal's breeds side by side. The
`AnimalShowcase` component that drives them is for the demo only.

## The full pack

Farm Life Lite is two animals from Farm Life. The full pack adds the rooster,
chicks, ducks, geese, turkey and llama, with every breed of every animal.
Import it over this one: the files share their paths and GUIDs, so everything
here is updated in place and nothing is duplicated.

## Render pipelines

Every material ships for each pipeline, under the same name and GUID:
in the `Materials` folders on URP Lit, and in `RenderPipelines/` as `FarmLifeLite_BuiltIn.unitypackage`
(Standard), `FarmLifeLite_URP.unitypackage` (URP Lit) and `FarmLifeLite_HDRP.unitypackage`
(HDRP Lit).

A moment after the pack is imported, the editor script in
`Assets/Backrock/RenderPipelines` copies each material's version for the
render pipeline your project uses over it, and does so again if you switch
pipelines, so the models, prefabs and scenes render correctly in the Built-in
pipeline, URP and HDRP with nothing to convert. It only rewrites shipped
materials still on Standard, URP Lit or HDRP Lit, so one you have put on a
shader of your own is left alone. To keep every material on one pipeline
whatever the project uses, choose it under **Tools > Backrock > Render
Pipeline Materials**; Match Active Pipeline there turns the automatic
matching back on.

## Technical

- One mesh per animal, all quads (1232 to 1784 triangles), one material.
- Rig: Generic, 16 to 21 bones. The birds share one set of bone
  names and the four-legged animals another, so a clip plays on any animal of its kind.
- Units: metres, Y up, facing +Z, scale factor 1.
- Scripts live in the `Backrock.FarmLife` assembly and namespace.

Generated original content by Backrock Studios.
