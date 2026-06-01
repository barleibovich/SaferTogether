# SaferTogether Unity Client

Minimal Unity client for the existing SaferTogether gateway.

## Run

1. Open this `UnityClient` folder as a Unity project.
2. Start the existing gateway from `Gateway` with valid `SUPABASE_URL` and `SUPABASE_ANON_KEY`.
3. Press Play in Unity. The runtime UI is created automatically.

Default gateway URL is `http://localhost:5173`. Change it on the generated `SaferTogetherAuthController` component if needed.

## What It Does

- Includes a built-in character avatar composer for human, dragon, bear, elephant, devil, and angel avatars.
- Supports sex, skin, face shape, eyes, eye color, hair, hair color, shirts/jackets/sweatshirts, pants, shoes, accessories, and background.
- Signs up users with username, password, role, and composed character avatar id.
- Logs in users through the existing gateway.
- Loads the logged-in profile.
- Saves avatar edits with `PATCH /api/auth/profile`.
- Uses the bearer token returned by signup/login for Unity requests.

Complete character avatar ids are stored as:

`character:v2:<species>:<sex>:<skin>:<face>:<eyes>:<eyeColor>:<hair>:<hairColor>:<top>:<topColor>:<bottom>:<bottomColor>:<shoes>:<shoeColor>:<accessory>:<background>`

Older preset, builder, and `character:v1` ids are still accepted and mapped into the new editor.

## Embed In The Web App

Use Unity as a WebGL module inside the existing frontend:

1. In Unity, install WebGL Build Support if Hub asks for it.
2. Open `File > Build Settings`.
3. Choose `WebGL`, then `Switch Platform`.
4. In `Player Settings > Publishing Settings`, set `Compression Format` to `Disabled` for the simplest local run.
5. Run `SaferTogether > Build WebGL Avatar Editor`.
6. Start the gateway and open `http://localhost:5173/avatar-editor.html`.

The website loads the Unity build, sends the current logged-in profile into Unity, and Unity keeps calling the same gateway/backend.
