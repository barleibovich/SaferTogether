# Avatar Editor WebGL Build

Build the Unity avatar editor here from Unity:

`C:\SaferTogether\SaferTogetherUI\unity\avatar-editor`

Use the WebGL platform and set the build file name to `avatar-editor` so the web host can load:

- `Build/avatar-editor.loader.js`
- `Build/avatar-editor.data`
- `Build/avatar-editor.framework.js`
- `Build/avatar-editor.wasm`

For the simplest local run, set WebGL `Compression Format` to `Disabled` before building.

The editor saves the complete `character:v2` avatar format used by the web signup flow, logged-in web editor, backend validator, and Unity runtime UI.
