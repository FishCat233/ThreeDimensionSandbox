# ThreeDimensionSandbox

This project mainly for personal use, such as learning the Unity 3D system and attempting to apply certain functions of the engine within it.

## Requirements

- Unity `2022.3.62f3`
- Git LFS — binary assets (`*.fbx`, `*.png`, `*.pmx`, `*.bytes`, ...) are tracked by LFS, run `git lfs install` before cloning.
- [MMD4Mecanim](https://stereoarts.jp/) plugin — **not included in this repository**, see below.

### MMD4Mecanim

`Assets/MMD4Mecanim/` is ignored on purpose: the plugin ships ~450MB of native binaries (`*.a`, `*.dll`, `*.dylib`, `*.so`), which is too large for plain git and too large to be worth an LFS quota.

To open `Assets/Scenes/TestScene.unity` without missing-script / pink-material errors:

1. Download MMD4Mecanim from <https://stereoarts.jp/>.
2. Import the `.unitypackage` into the project.
3. Make sure the plugin lands at `Assets/MMD4Mecanim/` — the model's `.meta` files reference its script and shader GUIDs, so a different path or version will break them.

MMD models under `Assets/Arts/Models/` are imported from `.pmx` through MMD4Mecanim (`PMX2FBX`), which produces the sibling `*.fbx`, `*.xml` and `*.bytes` files. Those generated files are committed because re-generating them requires the plugin; the `*.pmx` source is kept too.
