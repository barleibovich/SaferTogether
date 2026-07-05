using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaferTogether.UnityClient
{
    // Iron Dome dodge game. The player's avatar moves in four directions while incoming missiles
    // telegraph impact zones, fall at an angle, and explode on the shelter floor.
    public sealed class MissileMiniGame : MonoBehaviour
    {
        private const float GameSeconds = 60f;
        private const float MinX = 0.10f;
        private const float MaxX = 0.90f;
        private const float MinY = 0.20f;
        private const float MaxY = 0.78f;
        private const float StartX = 0.50f;
        private const float StartY = 0.28f;
        private const float PlayerSpeed = 0.58f;
        private const float ImpactRadius = 0.095f;
        private const float MissileWidth = 96f;
        private const float MissileHeight = 150f;
        private const float WallBottomY = 0.40f;
        private const float RuntimeAvatarTurnYaw = 90f;
        private const int RuntimeAvatarLayer = 31;

        private sealed class Missile
        {
            public RectTransform rect;
            public RectTransform trail;
            public RectTransform warning;
            public float x;
            public float y;
            public float targetX;
            public float targetY;
            public float velocityX;
            public float velocityY;
            public float speed;
            public float age;
        }

        private sealed class Effect
        {
            public RectTransform rect;
            public Image image;
            public float age;
            public float duration;
            public float startSize;
            public float endSize;
        }

        private Canvas canvas;
        private RectTransform field;
        private RectTransform warningLayer;
        private RectTransform missileLayer;
        private RectTransform effectLayer;
        private RectTransform playerLayer;
        private RectTransform controlLayer;
        private RectTransform timerFill;
        private RectTransform player;
        private RectTransform playerShadow;
        private Text hudText;
        private Text alertText;
        private Sprite missileSprite;
        private Action<MissionGameResult> onDone;
        private static Sprite circleSprite;
        private GameObject avatarRenderRoot;
        private Transform avatarRenderAvatarRoot;
        private Camera avatarRenderCamera;
        private RenderTexture avatarRenderTexture;
        private CharacterSpawner avatarRenderSpawner;
        private RawImage liveAvatarImage;
        private Texture sourceAvatarTexture;
        private string selectedAvatarId = "";

        private readonly List<Missile> missiles = new List<Missile>();
        private readonly List<Effect> effects = new List<Effect>();
        private float avatarX = StartX;
        private float avatarY = StartY;
        private float timeLeft;
        private float spawnTimer;
        private float tiltStrength;
        private float lastMoveX = 1f;
        private float avatarYaw;
        private float tiltNeutralGamma;
        private float tiltNeutralBeta;
        private bool tiltCalibrated;
        private float smoothTiltX;
        private float smoothTiltY;
        private float walkHoldUntil;
        private int hits;
        private int holdX;
        private int holdY;
        private bool running;
        private bool avatarRenderReady;

#if UNITY_WEBGL && !UNITY_EDITOR
        // current phone tilt from the browser (deviceorientation), in degrees
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern float SaferTogetherGetTiltGamma();

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern float SaferTogetherGetTiltBeta();
#endif

        public bool IsOpen => canvas != null;

        public void Open(Sprite avatarSprite, Action<MissionGameResult> done)
        {
            Open("", null, avatarSprite, done);
        }

        public void Open(string avatarId, Sprite avatarSprite, Action<MissionGameResult> done)
        {
            Open(avatarId, null, avatarSprite, done);
        }

        public void Open(string avatarId, Texture avatarTexture, Sprite avatarSprite, Action<MissionGameResult> done)
        {
            if (IsOpen)
            {
                return;
            }

            onDone = done;
            selectedAvatarId = string.IsNullOrEmpty(avatarId) ? "" : avatarId.Trim();
            sourceAvatarTexture = avatarTexture;
            missileSprite = Resources.Load<Sprite>("MissionGames/missile");
            hits = 0;
            tiltStrength = 0f;
            avatarX = StartX;
            avatarY = StartY;
            lastMoveX = 1f;
            avatarYaw = 0f;
            tiltCalibrated = false;
            smoothTiltX = 0f;
            smoothTiltY = 0f;
            walkHoldUntil = 0f;
            timeLeft = GameSeconds;
            spawnTimer = 0.35f;
            holdX = 0;
            holdY = 0;
            avatarRenderReady = false;
            missiles.Clear();
            effects.Clear();

            BuildUi(avatarSprite);
            running = true;
        }

        public void Close()
        {
            running = false;
            missiles.Clear();
            effects.Clear();
            CleanupRuntimeAvatar();
            sourceAvatarTexture = null;

            if (canvas != null)
            {
                Destroy(canvas.gameObject);
                canvas = null;
            }
        }

        private void BuildUi(Sprite avatarSprite)
        {
            canvas = MissionGameUi.CreateOverlay(transform, "Iron Dome Missile Game");

            field = MissionGameUi.Panel3(canvas.transform, "Missile Field", Vector2.zero, Vector2.one, new Color32(46, 49, 53, 255));
            BuildBattlefield();
            warningLayer = MissionGameUi.Stretch(field, "Warning Layer", Vector2.zero, Vector2.one);
            missileLayer = MissionGameUi.Stretch(field, "Missile Layer", Vector2.zero, Vector2.one);
            effectLayer = MissionGameUi.Stretch(field, "Impact Layer", Vector2.zero, Vector2.one);
            playerLayer = MissionGameUi.Stretch(field, "Player Layer", Vector2.zero, Vector2.one);
            controlLayer = MissionGameUi.Stretch(field, "Control Layer", Vector2.zero, Vector2.one);

            hudText = MissionGameUi.Label(field, new Vector2(0.04f, 0.92f), new Vector2(0.96f, 0.985f), "", 17, TextAnchor.MiddleCenter, Color.white);
            alertText = MissionGameUi.Label(field, new Vector2(0.12f, 0.84f), new Vector2(0.88f, 0.90f), MissionText.Rtl("הפיגעה מאזורי להתחמק כדי כיוונים בארבעה זוזו"), 14, TextAnchor.MiddleCenter, new Color32(255, 220, 120, 255));

            RectTransform timerTrack = MissionGameUi.Panel3(field, "Timer Track", new Vector2(0.06f, 0.895f), new Vector2(0.94f, 0.91f), new Color32(22, 34, 48, 220));
            timerFill = MissionGameUi.Panel3(timerTrack, "Timer Fill", Vector2.zero, Vector2.one, new Color32(42, 210, 135, 255));

            BuildPlayer(avatarSprite);
            BuildMovementControls();
        }

        private void BuildBattlefield()
        {
            Sprite wallSprite = LoadNewRoomSprite("wall");

            RectTransform backWall = MissionGameUi.Panel3(field, "Back Wall", new Vector2(0f, WallBottomY - 0.02f), new Vector2(1f, 1f), new Color32(206, 224, 228, 255));
            Image backWallImage = backWall.GetComponent<Image>();
            SetImageSprite(backWallImage, wallSprite, false);
            backWallImage.raycastTarget = false;

            RectTransform floor = MissionGameUi.Panel3(field, "Floor", new Vector2(0f, 0.12f), new Vector2(1f, 0.82f), new Color32(108, 117, 109, 255));
            Image floorImage = floor.GetComponent<Image>();
            SetImageSprite(floorImage, LoadNewRoomSprite("floor"), false);
            floorImage.raycastTarget = false;

            Image backFloorEdge = CreateImage(field, "Back Floor Edge", new Vector2(0f, 0.775f), new Vector2(1f, 0.783f), new Color32(35, 48, 48, 92));
            backFloorEdge.raycastTarget = false;

            Texture wallTexture = wallSprite != null ? wallSprite.texture : null;
            CreateSideWall(field, "Left Wall", wallTexture, new Vector2(0f, 0.04f), new Vector2(0.18f, 1f), false, 0.44f, new Color32(240, 242, 240, 255));
            CreateSideWall(field, "Right Wall", wallTexture, new Vector2(0.82f, 0.04f), new Vector2(1f, 1f), true, 0.44f, new Color32(222, 226, 223, 255));

            Image leftWallEdge = CreateImage(field, "Left Wall Edge", new Vector2(0.178f, 0.455f), new Vector2(0.184f, 1f), new Color32(45, 45, 45, 16));
            leftWallEdge.raycastTarget = false;

            Image rightWallEdge = CreateImage(field, "Right Wall Edge", new Vector2(0.816f, 0.455f), new Vector2(0.822f, 1f), new Color32(45, 45, 45, 16));
            rightWallEdge.raycastTarget = false;

            Image nearFloorShade = CreateImage(field, "Near Floor Shade", new Vector2(0f, 0.12f), new Vector2(1f, 0.20f), new Color32(7, 16, 21, 54));
            nearFloorShade.raycastTarget = false;
        }

        private void BuildPlayer(Sprite avatarSprite)
        {
            var shadowObject = new GameObject("Player Shadow", typeof(RectTransform), typeof(Image));
            shadowObject.transform.SetParent(playerLayer != null ? playerLayer : field, false);
            playerShadow = shadowObject.GetComponent<RectTransform>();
            playerShadow.pivot = new Vector2(0.5f, 0.5f);
            playerShadow.sizeDelta = new Vector2(74, 24);
            Image shadowImage = shadowObject.GetComponent<Image>();
            shadowImage.sprite = CircleSprite();
            shadowImage.color = new Color32(0, 0, 0, 78);
            shadowImage.raycastTarget = false;

            var playerObject = new GameObject("Player Avatar", typeof(RectTransform));
            playerObject.transform.SetParent(playerLayer != null ? playerLayer : field, false);
            player = playerObject.GetComponent<RectTransform>();
            player.pivot = new Vector2(0.5f, 0f);
            player.sizeDelta = new Vector2(86, 146);

            if (TryShowSourceAvatarTexture(player))
            {
                ApplyPlayerPosition(false);
                return;
            }

            if (TryShowRuntimeAvatar(player))
            {
                ApplyPlayerPosition(false);
                return;
            }

            Image playerImage = playerObject.AddComponent<Image>();
            playerImage.preserveAspect = true;
            playerImage.raycastTarget = false;

            if (avatarSprite != null)
            {
                playerImage.sprite = avatarSprite;
                playerImage.color = Color.white;
            }
            else
            {
                playerImage.enabled = false;
                BuildClassic2DAvatar(player);
            }

            ApplyPlayerPosition(false);
        }

        private static Image CreateImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static RoomSideWallGraphic CreateSideWall(Transform parent, string name, Texture texture, Vector2 anchorMin, Vector2 anchorMax, bool rightSide, float innerFloorY, Color color)
        {
            var wallObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RoomSideWallGraphic));
            wallObject.transform.SetParent(parent, false);
            RectTransform rect = wallObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = new Vector3(1.3f, 0.988f, 1f);

            RoomSideWallGraphic wall = wallObject.GetComponent<RoomSideWallGraphic>();
            wall.Configure(texture, rightSide, innerFloorY, color);
            wall.raycastTarget = false;
            return wall;
        }

        private static Sprite LoadNewRoomSprite(string id)
        {
            Texture2D texture = Resources.Load<Texture2D>("NewRoom/" + id);

            if (texture == null)
            {
                return null;
            }

            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static void SetImageSprite(Image image, Sprite sprite, bool preserveAspect)
        {
            if (image == null || sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = preserveAspect;
        }

        private void BuildMovementControls()
        {
            Transform parent = controlLayer != null ? controlLayer : field;
            RectTransform pad = MissionGameUi.Panel3(parent, "Move Pad", new Vector2(0.035f, 0.02f), new Vector2(0.32f, 0.17f), new Color32(8, 17, 30, 120));
        }

        private void CreateHoldButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, int dirX, int dirY)
        {
            Text text;
            Button button = MissionGameUi.TextButton(parent, anchorMin, anchorMax, label, new Color32(38, 58, 72, 235), 22, null, out text);
            text.color = new Color32(238, 246, 250, 255);

            EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();
            AddPointerTrigger(trigger, EventTriggerType.PointerDown, _ => SetHold(dirX, dirY));
            AddPointerTrigger(trigger, EventTriggerType.PointerUp, _ => ClearHold(dirX, dirY));
            AddPointerTrigger(trigger, EventTriggerType.PointerExit, _ => ClearHold(dirX, dirY));
        }

        private void SetHold(int dirX, int dirY)
        {
            holdX = dirX;
            holdY = dirY;
        }

        private void ClearHold(int dirX, int dirY)
        {
            if (holdX == dirX) holdX = 0;
            if (holdY == dirY) holdY = 0;
        }

        private static void AddPointerTrigger(EventTrigger trigger, EventTriggerType type, UnityAction<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }

        private void BuildClassic2DAvatar(RectTransform parent)
        {
            CreateAvatarPart(parent, "Head", new Vector2(0.31f, 0.68f), new Vector2(0.69f, 0.98f), new Color32(241, 186, 132, 255), true);
            CreateAvatarPart(parent, "Hair", new Vector2(0.27f, 0.82f), new Vector2(0.73f, 1.02f), new Color32(55, 36, 28, 255), true);
            CreateAvatarPart(parent, "Vest", new Vector2(0.19f, 0.34f), new Vector2(0.81f, 0.72f), new Color32(37, 72, 84, 255), false);
            CreateAvatarPart(parent, "Left Arm", new Vector2(0.05f, 0.38f), new Vector2(0.24f, 0.66f), new Color32(241, 186, 132, 255), false);
            CreateAvatarPart(parent, "Right Arm", new Vector2(0.76f, 0.38f), new Vector2(0.95f, 0.66f), new Color32(241, 186, 132, 255), false);
            CreateAvatarPart(parent, "Pants", new Vector2(0.22f, 0.16f), new Vector2(0.78f, 0.38f), new Color32(32, 44, 54, 255), false);
            CreateAvatarPart(parent, "Left Leg", new Vector2(0.28f, 0.00f), new Vector2(0.43f, 0.22f), new Color32(36, 39, 42, 255), false);
            CreateAvatarPart(parent, "Right Leg", new Vector2(0.57f, 0.00f), new Vector2(0.72f, 0.22f), new Color32(36, 39, 42, 255), false);
        }

        private static void CreateAvatarPart(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color, bool circle)
        {
            var part = new GameObject(name, typeof(RectTransform), typeof(Image));
            part.transform.SetParent(parent, false);
            RectTransform rect = part.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = part.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            if (circle)
            {
                image.sprite = CircleSprite();
                image.preserveAspect = true;
            }
        }

        private static Sprite CircleSprite()
        {
            if (circleSprite != null)
            {
                return circleSprite;
            }

            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float radius = (size - 1) * 0.5f;

            for (int y = 0; y < size; y += 1)
            {
                for (int x = 0; x < size; x += 1)
                {
                    float dx = x - radius;
                    float dy = y - radius;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                    byte alpha = distance <= 1f ? (byte)(Mathf.SmoothStep(1f, 0f, distance) * 255f) : (byte)0;
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return circleSprite;
        }

        private void Update()
        {
            if (!running)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            timeLeft -= dt;

            ReadMovement(dt);
            SpawnMissiles(dt);
            MoveMissiles(dt);
            UpdateEffects(dt);
            UpdateHud();

            if (timeLeft <= 0f)
            {
                Finish();
            }
        }

        private void ReadMovement(float dt)
        {
            Vector2 move = new Vector2(holdX, holdY);

#if UNITY_WEBGL && !UNITY_EDITOR
            // steer by phone rotation from the browser (Input.acceleration is 0 in WebGL).
            // the first reading becomes "center", so however the phone is held is neutral and
            // it only moves when you actually rotate away from that. 999 = no reading -> no move.
            float rawGamma = SaferTogetherGetTiltGamma();
            float rawBeta = SaferTogetherGetTiltBeta();
            if (Mathf.Abs(rawBeta) < 900f && Mathf.Abs(rawGamma) < 900f)
            {
                if (!tiltCalibrated)
                {
                    tiltNeutralGamma = rawGamma;
                    tiltNeutralBeta = rawBeta;
                    tiltCalibrated = true;
                }

                // how far the phone is tilted from the calibrated centre; ~22deg = full speed
                float targetX = Mathf.Clamp((rawGamma - tiltNeutralGamma) / 22f, -1f, 1f);  // tilt right -> right
                float targetY = Mathf.Clamp((tiltNeutralBeta - rawBeta) / 22f, -1f, 1f);     // tilt forward -> up
                if (Mathf.Abs(targetX) < 0.10f) targetX = 0f;
                if (Mathf.Abs(targetY) < 0.10f) targetY = 0f;

                // smooth the reading so noisy sensor jitter doesn't make the avatar twitch
                float smooth = 1f - Mathf.Exp(-dt * 12f);
                smoothTiltX = Mathf.Lerp(smoothTiltX, targetX, smooth);
                smoothTiltY = Mathf.Lerp(smoothTiltY, targetY, smooth);

                move.x += smoothTiltX;
                move.y += smoothTiltY;
                tiltStrength += (Mathf.Abs(smoothTiltX) + Mathf.Abs(smoothTiltY)) * dt;
            }
#else
            float tiltX = Input.acceleration.x;
            float tiltY = Input.acceleration.y;
            if (Mathf.Abs(tiltX) > 0.04f || Mathf.Abs(tiltY) > 0.06f)
            {
                move.x += tiltX * 1.35f;
                move.y += Mathf.Clamp(tiltY + 0.35f, -1f, 1f) * 0.65f;
                tiltStrength += (Mathf.Abs(tiltX) + Mathf.Abs(tiltY)) * dt;
            }
#endif

            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) move.x -= 1f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) move.x += 1f;
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) move.y += 1f;
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) move.y -= 1f;

            if (Input.GetMouseButton(0) && Screen.width > 0 && Screen.height > 0 && Input.mousePosition.y > Screen.height * 0.18f)
            {
                Vector2 target = ScreenToFieldPoint(Input.mousePosition);
                Vector2 delta = target - new Vector2(avatarX, avatarY);
                if (delta.sqrMagnitude > 0.0004f)
                {
                    move += delta.normalized;
                }
            }

            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            avatarX += move.x * PlayerSpeed * dt;
            avatarY += move.y * PlayerSpeed * dt;
            avatarX = Mathf.Clamp(avatarX, MinX, MaxX);
            avatarY = Mathf.Clamp(avatarY, MinY, MaxY);

            bool moving = move.sqrMagnitude > 0.001f;
            if (Mathf.Abs(move.x) > 0.03f)
            {
                lastMoveX = Mathf.Sign(move.x);
            }

            if (moving)
            {
                UpdatePlayerFacing(move);
                // hold the walk animation briefly so quick dodges still show a walk cycle
                walkHoldUntil = Time.unscaledTime + 0.18f;
            }

            bool walking = Time.unscaledTime < walkHoldUntil;
            ApplyPlayerPosition(walking);
        }

        private Vector2 ScreenToFieldPoint(Vector2 screenPoint)
        {
            if (field == null)
            {
                return new Vector2(avatarX, avatarY);
            }

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(field, screenPoint, null, out localPoint))
            {
                return new Vector2(avatarX, avatarY);
            }

            Rect rect = field.rect;
            return new Vector2(
                Mathf.Clamp01(Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x)),
                Mathf.Clamp01(Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y)));
        }

        private void ApplyPlayerPosition(bool moving)
        {
            if (player == null)
            {
                return;
            }

            player.anchorMin = player.anchorMax = new Vector2(avatarX, avatarY);
            float bob = moving ? Mathf.Sin(Time.unscaledTime * 13f) * 4f : 0f;
            player.anchoredPosition = new Vector2(0f, bob);
            float depthScale = Mathf.Lerp(1.0f, 0.72f, Mathf.InverseLerp(MinY, MaxY, avatarY));
            float horizontalScale = avatarRenderReady ? depthScale : (lastMoveX < 0f ? -depthScale : depthScale);
            player.localScale = new Vector3(horizontalScale, depthScale, 1f);

            if (playerShadow != null)
            {
                playerShadow.anchorMin = playerShadow.anchorMax = new Vector2(avatarX, avatarY - 0.015f);
                playerShadow.localScale = new Vector3(depthScale, depthScale, 1f);
            }

            RenderRuntimeAvatar(moving);
        }

        private void UpdatePlayerFacing(Vector2 move)
        {
            if (Mathf.Abs(move.x) >= Mathf.Abs(move.y) && Mathf.Abs(move.x) > 0.03f)
            {
                avatarYaw = move.x < 0f ? -RuntimeAvatarTurnYaw : RuntimeAvatarTurnYaw;
            }
            else if (Mathf.Abs(move.y) > 0.03f)
            {
                avatarYaw = move.y < 0f ? 180f : 0f;
            }
        }

        private bool TryShowRuntimeAvatar(RectTransform avatarContainer)
        {
            if (string.IsNullOrEmpty(selectedAvatarId))
            {
                return false;
            }

            try
            {
                PrepareRuntimeAvatarRenderRig();

                if (!ApplyRuntimeAvatarSelection())
                {
                    CleanupRuntimeAvatar();
                    return false;
                }

                SetLayerRecursively(avatarRenderAvatarRoot.gameObject, RuntimeAvatarLayer);

                if (!FrameRuntimeAvatar())
                {
                    CleanupRuntimeAvatar();
                    return false;
                }

                if (!RenderTextureHasVisiblePixels(avatarRenderTexture))
                {
                    CleanupRuntimeAvatar();
                    return false;
                }

                ShowRuntimeAvatarTexture(avatarContainer);
                avatarRenderReady = true;
                RenderRuntimeAvatar(false);
                return true;
            }
            catch (Exception error)
            {
                CleanupRuntimeAvatar();
                Debug.LogWarning("Could not render runtime avatar in missile game: " + error.Message, this);
                return false;
            }
        }

        private bool TryShowSourceAvatarTexture(RectTransform avatarContainer)
        {
            if (avatarContainer == null || sourceAvatarTexture == null)
            {
                return false;
            }

            RawImage image = avatarContainer.GetComponent<RawImage>();
            if (image == null)
            {
                image = avatarContainer.gameObject.AddComponent<RawImage>();
            }

            image.texture = sourceAvatarTexture;
            image.color = Color.white;
            image.raycastTarget = false;
            liveAvatarImage = image;
            avatarRenderReady = false;
            return true;
        }

        private void PrepareRuntimeAvatarRenderRig()
        {
            if (avatarRenderRoot != null)
            {
                return;
            }

            avatarRenderRoot = new GameObject("Missile Runtime Avatar Render Rig");
            avatarRenderRoot.transform.SetParent(transform, false);
            avatarRenderRoot.transform.position = new Vector3(0f, -320f, 0f);

            avatarRenderAvatarRoot = new GameObject("Avatar Preview Root").transform;
            avatarRenderAvatarRoot.SetParent(avatarRenderRoot.transform, false);

            var spawnerObject = new GameObject("Missile Avatar Spawner", typeof(CharacterSpawner));
            spawnerObject.transform.SetParent(avatarRenderRoot.transform, false);
            avatarRenderSpawner = spawnerObject.GetComponent<CharacterSpawner>();
            avatarRenderSpawner.mountPoint = avatarRenderAvatarRoot;

            avatarRenderTexture = new RenderTexture(384, 640, 24, RenderTextureFormat.ARGB32)
            {
                name = "Missile Runtime Avatar Texture",
                useMipMap = false,
                autoGenerateMips = false
            };
            avatarRenderTexture.Create();

            var cameraObject = new GameObject("Missile Avatar Camera", typeof(Camera));
            cameraObject.transform.SetParent(avatarRenderRoot.transform, false);
            avatarRenderCamera = cameraObject.GetComponent<Camera>();
            avatarRenderCamera.clearFlags = CameraClearFlags.SolidColor;
            avatarRenderCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            avatarRenderCamera.cullingMask = 1 << RuntimeAvatarLayer;
            avatarRenderCamera.orthographic = true;
            avatarRenderCamera.orthographicSize = 1.2f;
            avatarRenderCamera.nearClipPlane = 0.01f;
            avatarRenderCamera.farClipPlane = 20f;
            avatarRenderCamera.targetTexture = avatarRenderTexture;

            var lightObject = new GameObject("Missile Avatar Light", typeof(Light));
            lightObject.transform.SetParent(avatarRenderRoot.transform, false);
            lightObject.transform.localRotation = Quaternion.Euler(45f, -25f, 0f);
            Light avatarLight = lightObject.GetComponent<Light>();
            avatarLight.type = LightType.Directional;
            avatarLight.intensity = 1.25f;
            avatarLight.cullingMask = 1 << RuntimeAvatarLayer;
        }

        private bool ApplyRuntimeAvatarSelection()
        {
            if (avatarRenderSpawner == null)
            {
                return false;
            }

            if (avatarRenderSpawner.Show(selectedAvatarId) == null)
            {
                return false;
            }

            ApplyRuntimeAvatarFacing();
            return true;
        }

        private bool FrameRuntimeAvatar()
        {
            if (avatarRenderAvatarRoot == null || avatarRenderCamera == null || avatarRenderTexture == null)
            {
                return false;
            }

            if (!TryGetRendererBounds(avatarRenderAvatarRoot.gameObject, out Bounds bounds))
            {
                return false;
            }

            float aspect = (float)avatarRenderTexture.width / avatarRenderTexture.height;
            float avatarHeight = Mathf.Max(bounds.size.y, 0.01f);
            float heightBasedSize = avatarHeight * 0.56f;
            float widthBasedSize = Mathf.Max(bounds.size.x, 0.01f) / aspect * 0.55f;
            float orthographicSize = Mathf.Max(heightBasedSize, widthBasedSize);
            float bottomPadding = avatarHeight * 0.04f;
            float cameraY = bounds.min.y - bottomPadding + orthographicSize;

            avatarRenderCamera.transform.position = new Vector3(bounds.center.x, cameraY, bounds.min.z - 3f);
            avatarRenderCamera.transform.rotation = Quaternion.identity;
            avatarRenderCamera.orthographicSize = orthographicSize;
            avatarRenderCamera.farClipPlane = Mathf.Max(8f, bounds.size.z + 6f);
            avatarRenderCamera.Render();
            return true;
        }

        private void ShowRuntimeAvatarTexture(RectTransform avatarContainer)
        {
            if (avatarContainer == null || avatarRenderTexture == null)
            {
                return;
            }

            RawImage image = avatarContainer.GetComponent<RawImage>();
            if (image == null)
            {
                image = avatarContainer.gameObject.AddComponent<RawImage>();
            }

            image.texture = avatarRenderTexture;
            image.color = Color.white;
            image.raycastTarget = false;
            liveAvatarImage = image;
        }

        private void RenderRuntimeAvatar(bool walking)
        {
            if (!avatarRenderReady || avatarRenderSpawner == null || avatarRenderCamera == null || avatarRenderTexture == null)
            {
                return;
            }

            avatarRenderSpawner.SetWalking(walking);
            ApplyRuntimeAvatarFacing();
            SetLayerRecursively(avatarRenderAvatarRoot.gameObject, RuntimeAvatarLayer);

            if (!avatarRenderTexture.IsCreated())
            {
                avatarRenderTexture.Create();
            }

            if (liveAvatarImage != null && liveAvatarImage.texture != avatarRenderTexture)
            {
                liveAvatarImage.texture = avatarRenderTexture;
            }

            avatarRenderCamera.Render();
        }

        private void ApplyRuntimeAvatarFacing()
        {
            if (avatarRenderSpawner == null || avatarRenderSpawner.CurrentInstance == null)
            {
                return;
            }

            avatarRenderSpawner.CurrentInstance.transform.localRotation = Quaternion.Euler(0f, avatarYaw, 0f);
        }

        private static bool RenderTextureHasVisiblePixels(RenderTexture renderTexture)
        {
            if (renderTexture == null || !renderTexture.IsCreated())
            {
                return false;
            }

            RenderTexture previous = RenderTexture.active;
            var sample = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);

            try
            {
                RenderTexture.active = renderTexture;
                sample.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                sample.Apply();
                Color32[] pixels = sample.GetPixels32();

                for (int index = 0; index < pixels.Length; index += 16)
                {
                    if (pixels[index].a > 8)
                    {
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                RenderTexture.active = previous;
                Destroy(sample);
            }
        }

        private void CleanupRuntimeAvatar()
        {
            avatarRenderReady = false;

            if (liveAvatarImage != null)
            {
                liveAvatarImage.texture = null;
                liveAvatarImage = null;
            }

            avatarRenderSpawner = null;
            avatarRenderAvatarRoot = null;

            if (avatarRenderCamera != null)
            {
                avatarRenderCamera.targetTexture = null;
                avatarRenderCamera = null;
            }

            if (avatarRenderTexture != null)
            {
                avatarRenderTexture.Release();
                Destroy(avatarRenderTexture);
                avatarRenderTexture = null;
            }

            if (avatarRenderRoot != null)
            {
                Destroy(avatarRenderRoot);
                avatarRenderRoot = null;
            }
        }

        private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bounds = new Bounds(root.transform.position, Vector3.one);
            bool found = false;

            for (int i = 0; i < renderers.Length; i += 1)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            return found;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null)
            {
                return;
            }

            root.layer = layer;

            foreach (Transform child in root.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private void SpawnMissiles(float dt)
        {
            spawnTimer -= dt;

            if (spawnTimer > 0f)
            {
                return;
            }

            float progress = Mathf.Clamp01((GameSeconds - timeLeft) / GameSeconds);
            spawnTimer = Mathf.Lerp(1.05f, 0.42f, progress);
            float fallSpeed = Mathf.Lerp(0.34f, 0.62f, progress);

            Vector2 start = new Vector2(UnityEngine.Random.Range(0.05f, 0.95f), 1.08f);
            Vector2 target = new Vector2(UnityEngine.Random.Range(MinX, MaxX), UnityEngine.Random.Range(MinY, MaxY));
            Vector2 direction = (target - start).normalized;

            RectTransform warning = CreateWarning(target, progress);

            var missileObject = new GameObject("Incoming Missile", typeof(RectTransform), typeof(Image));
            missileObject.transform.SetParent(missileLayer != null ? missileLayer : field, false);
            RectTransform rect = missileObject.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(MissileWidth, MissileHeight);

            Image image = missileObject.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;

            if (missileSprite != null)
            {
                image.sprite = missileSprite;
                image.color = Color.white;
            }
            else
            {
                image.sprite = CircleSprite();
                image.color = new Color32(225, 70, 78, 255);
            }

            var trailObject = new GameObject("Missile Trail", typeof(RectTransform), typeof(Image));
            trailObject.transform.SetParent(missileLayer != null ? missileLayer : field, false);
            RectTransform trail = trailObject.GetComponent<RectTransform>();
            trail.pivot = new Vector2(0.5f, 0.5f);
            trail.sizeDelta = new Vector2(30, 120);
            Image trailImage = trailObject.GetComponent<Image>();
            trailImage.sprite = CircleSprite();
            trailImage.color = new Color32(255, 140, 45, 130);
            trailImage.raycastTarget = false;

            var missile = new Missile
            {
                rect = rect,
                trail = trail,
                warning = warning,
                x = start.x,
                y = start.y,
                targetX = target.x,
                targetY = target.y,
                velocityX = direction.x,
                velocityY = direction.y,
                speed = fallSpeed,
                age = 0f
            };

            ApplyMissileTransform(missile);
            missiles.Add(missile);
        }

        private RectTransform CreateWarning(Vector2 target, float progress)
        {
            var warningObject = new GameObject("Impact Warning", typeof(RectTransform), typeof(Image));
            warningObject.transform.SetParent(warningLayer != null ? warningLayer : field, false);
            RectTransform warning = warningObject.GetComponent<RectTransform>();
            warning.anchorMin = warning.anchorMax = target;
            warning.pivot = new Vector2(0.5f, 0.5f);
            warning.sizeDelta = Vector2.one * Mathf.Lerp(84f, 118f, progress);

            Image image = warningObject.GetComponent<Image>();
            image.sprite = CircleSprite();
            image.color = new Color32(255, 65, 45, 95);
            image.raycastTarget = false;
            return warning;
        }

        private void MoveMissiles(float dt)
        {
            for (int i = missiles.Count - 1; i >= 0; i -= 1)
            {
                Missile missile = missiles[i];
                missile.age += dt;
                missile.x += missile.velocityX * missile.speed * dt;
                missile.y += missile.velocityY * missile.speed * dt;

                ApplyMissileTransform(missile);
                UpdateWarning(missile);

                float dx = missile.x - missile.targetX;
                float dy = missile.y - missile.targetY;
                if (dx * dx + dy * dy <= 0.0025f || missile.y <= missile.targetY)
                {
                    Impact(missile);
                    missiles.RemoveAt(i);
                }
            }
        }

        private void ApplyMissileTransform(Missile missile)
        {
            Vector2 anchor = new Vector2(missile.x, missile.y);
            missile.rect.anchorMin = missile.rect.anchorMax = anchor;
            missile.rect.anchoredPosition = Vector2.zero;
            float angle = Mathf.Atan2(missile.velocityY, missile.velocityX) * Mathf.Rad2Deg;
            missile.rect.localRotation = Quaternion.Euler(0f, 0f, angle + 90f);

            if (missile.trail != null)
            {
                missile.trail.anchorMin = missile.trail.anchorMax = anchor - new Vector2(missile.velocityX, missile.velocityY) * 0.045f;
                missile.trail.localRotation = missile.rect.localRotation;
            }
        }

        private void UpdateWarning(Missile missile)
        {
            if (missile.warning == null)
            {
                return;
            }

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 12f) * 0.08f;
            missile.warning.localScale = new Vector3(pulse, pulse, 1f);
        }

        private void Impact(Missile missile)
        {
            bool hit = Vector2.Distance(new Vector2(avatarX, avatarY), new Vector2(missile.targetX, missile.targetY)) <= ImpactRadius;
            if (hit)
            {
                hits += 1;
                alertText.text = "Hit! Move out of the red impact circle.";
                alertText.color = MissionGameUi.Bad;
            }
            else
            {
                alertText.text = MissionText.Rtl("מעולה התחמקות");
                alertText.color = MissionGameUi.Good;
            }

            CreateExplosion(new Vector2(missile.targetX, missile.targetY), hit);

            if (missile.warning != null) Destroy(missile.warning.gameObject);
            if (missile.trail != null) Destroy(missile.trail.gameObject);
            if (missile.rect != null) Destroy(missile.rect.gameObject);
        }

        private void CreateExplosion(Vector2 anchor, bool hit)
        {
            var effectObject = new GameObject(hit ? "Impact Explosion" : "Near Miss Dust", typeof(RectTransform), typeof(Image));
            effectObject.transform.SetParent(effectLayer != null ? effectLayer : field, false);
            RectTransform rect = effectObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one * 26f;

            Image image = effectObject.GetComponent<Image>();
            image.sprite = CircleSprite();
            image.color = hit ? new Color32(255, 90, 50, 210) : new Color32(190, 190, 170, 145);
            image.raycastTarget = false;

            effects.Add(new Effect
            {
                rect = rect,
                image = image,
                duration = hit ? 0.55f : 0.42f,
                startSize = 26f,
                endSize = hit ? 180f : 110f
            });
        }

        private void UpdateEffects(float dt)
        {
            for (int i = effects.Count - 1; i >= 0; i -= 1)
            {
                Effect effect = effects[i];
                effect.age += dt;
                float t = Mathf.Clamp01(effect.age / effect.duration);
                float size = Mathf.Lerp(effect.startSize, effect.endSize, Mathf.SmoothStep(0f, 1f, t));
                effect.rect.sizeDelta = Vector2.one * size;

                Color color = effect.image.color;
                color.a = Mathf.Lerp(color.a, 0f, t);
                effect.image.color = color;

                if (t >= 1f)
                {
                    Destroy(effect.rect.gameObject);
                    effects.RemoveAt(i);
                }
            }
        }

        private void UpdateHud()
        {
            hudText.text = MissionText.Rtl(hits + " : פגיעות " + Mathf.CeilToInt(Mathf.Max(0f, timeLeft)) + " : זמן ברזל כיפת תרגיל");

            if (timerFill != null)
            {
                timerFill.anchorMin = Vector2.zero;
                timerFill.anchorMax = new Vector2(Mathf.Clamp01(timeLeft / GameSeconds), 1f);
                timerFill.offsetMin = Vector2.zero;
                timerFill.offsetMax = Vector2.zero;
            }
        }

        private void Finish()
        {
            running = false;

            var stage = new MissionStageResult
            {
                index = 0,
                label = "תרגיל כיפת ברזל",
                timeSeconds = GameSeconds,
                correct = hits == 0,
                wrongAttempts = hits,
                // same rotation measure as the other games, so the chart scales match
                rotation = MissionTilt.Take()
            };

            var result = new MissionGameResult
            {
                game = "missile",
                stages = new[] { stage },
                hits = hits,
                tiltStrength = Mathf.Round(tiltStrength * 100f) / 100f,
                totalSeconds = GameSeconds
            };

            Action<MissionGameResult> callback = onDone;
            Close();
            callback?.Invoke(result);
        }
    }
}
