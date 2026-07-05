using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace SaferTogether.UnityClient
{
    // group mission room where the player finishes room tasks
    [Preserve]
    public sealed class MissionRoomController : MonoBehaviour
    {
        private const string ControllerObjectName = "SaferTogether Mission Room Controller";

        private static readonly Color BackgroundColor = new Color32(221, 251, 255, 255);
        private static readonly Color TextColor = new Color32(7, 16, 21, 255);
        private static readonly Color MutedColor = new Color32(38, 56, 60, 255);
        private static readonly Color SafeColor = new Color32(8, 181, 101, 255);
        private static readonly Color DangerColor = new Color32(225, 64, 78, 255);
        private static readonly Color WallFallbackColor = new Color32(206, 224, 228, 255);

        // where the avatar stands at each object (x fraction of the room)
        // the emergency kit sits left of the door, so its zone x is the smaller value
        private const float DoorX = 0.22f;
        private const float KitX = 0.11f;
        private const float WindowX = 0.37f;
        private const float BoardX = 0.665f;
        private const float RadioX = 0.90f;
        private const float IronDomeX = 0.82f;
        private const float SafeX = 0.50f;

        private const float ZoneRadius = 0.085f;   // how close (in X fraction) counts as "at" an object
        private const float WalkSpeed = 0.42f;      // X fraction travelled per second
        private const float DepthSpeed = 0.55f;     // depth (forward/backward) travelled per second
        private const float MinX = 0.06f;
        private const float MaxX = 0.94f;
        private const float WallBottomY = 0.40f;     // wall fills above this height, floor below (room-relative)
        private const float FloorFrontY = 0.05f;     // avatar feet Y at the front of the room (depth 0)
        private const float FloorBackY = 0.37f;      // avatar feet Y at the back wall (depth 1)
        private const float FrontScale = 1.06f;      // avatar scale near the camera (depth 0)
        private const float BackScale = 0.80f;       // avatar scale at the back wall (depth 1)
        private const float RoomWorldWidth = 1.72f;  // room is wider than the visible camera viewport
        private const float CameraFollowSpeed = 7.5f;
        private const float RuntimeAvatarTurnYaw = 90f;
        private const int RoomRenderLayer = 29;
        private const int RuntimeAvatarLayer = 30;
        private const int RoomRenderTextureWidth = 1720;
        private const int RoomRenderTextureHeight = 720;
        private const float Room3DWidth = 12f;
        private const float Room3DDepth = 12f;
        private const float Room3DHeight = 4.2f;
        private const float Room3DFrontZ = -Room3DDepth * 0.5f;
        private const float Room3DBackZ = Room3DDepth * 0.5f;

        private static readonly Vector3 Floor3DPosition = new Vector3(0f, 0f, 0f);
        private static readonly Quaternion Floor3DRotation = Quaternion.Euler(0f, 0f, 0f);
        private static readonly Vector3 Floor3DScale = new Vector3(1f, 2f, 2f);
        private static readonly Vector3 BackWall3DPosition = new Vector3(0f, 2.1f, 6f);
        private static readonly Quaternion BackWall3DRotation = Quaternion.Euler(0f, 0f, 0f);
        private static readonly Vector3 BackWall3DScale = new Vector3(1f, 2f, 1f);
        private static readonly Vector3 RightWall3DPosition = new Vector3(6f, 2.1f, 6f);
        private static readonly Quaternion RightWall3DRotation = Quaternion.Euler(0f, 90f, 0f);
        private static readonly Vector3 RightWall3DScale = new Vector3(2f, 2f, 1f);
        private static readonly Vector3 LeftWall3DPosition = new Vector3(-6f, 2.1f, 6f);
        private static readonly Quaternion LeftWall3DRotation = Quaternion.Euler(0f, 90f, 0f);
        private static readonly Vector3 LeftWall3DScale = new Vector3(2f, 2f, 1f);
        private static readonly Vector3 Radio3DPosition = new Vector3(5f, 2f, -1f);
        private static readonly Quaternion Radio3DRotation = Quaternion.Euler(180f, 90f, 0f);
        private static readonly Vector3 Radio3DScale = new Vector3(1f, 2f, 1f);
        private static readonly Vector3 EmergencyKit3DPosition = new Vector3(-2.3f, 1.35f, -3f);
        private static readonly Quaternion EmergencyKit3DRotation = Quaternion.Euler(-100f, 190f, -90f);
        private static readonly Vector3 EmergencyKit3DScale = new Vector3(4f, 4f, 1f);
        private static readonly Vector3 IronDome3DPosition = new Vector3(3.84f, 1f, -2f);
        private static readonly Quaternion IronDome3DRotation = Quaternion.Euler(-90f, 0f, -100f);
        private static readonly Vector3 IronDome3DScale = Vector3.one;

        private enum Room3DFitMode
        {
            Stretch,
            UniformFit,
            UniformFill
        }

        // keep only one controller alive so SendMessage always hits the one on screen
        private static MissionRoomController activeInstance;

        private MissionRoomPayload payload = new MissionRoomPayload();

        // one canvas, reused for both the waiting screen and the mission screen
        private Canvas rootCanvas;
        private RectTransform canvasRoot;
        private Camera displayCamera;
        private RectTransform roomViewport;
        private RectTransform roomWorld;

        // build the mission on the next Update
        private bool pendingMissionBuild;
        private string pendingMissionJson = "";

        // stuff that gets rebuilt every mission
        private RectTransform avatarRect;
        private RectTransform shadowRect;
        private GameObject avatarRenderRoot;
        private Transform avatarRenderAvatarRoot;
        private Camera avatarRenderCamera;
        private RenderTexture avatarRenderTexture;
        private CharacterSpawner avatarRenderSpawner;
        private RawImage liveAvatarImage;
        private bool avatarRenderReady;
        private GameObject roomRenderRoot;
        private Camera roomRenderCamera;
        private RenderTexture roomRenderTexture;
        private readonly List<Material> roomRenderMaterials = new List<Material>();
        private readonly List<Mesh> roomRenderMeshes = new List<Mesh>();
        private Transform roomDoor3D;
        private Transform roomWindow3D;
        private Renderer roomRadioLightRenderer;
        private Light roomRadioLight;
        private GameObject roomDoorMarker3D;
        private GameObject roomBoardMarker3D;
        private GameObject roomRadioMarker3D;
        private Text statusText;
        private Button submitButton;
        private Button actionButton;
        private Text actionButtonLabel;
        private Button puzzleLauncherButton;
        private Button codeLauncherButton;
        private Button missileLauncherButton;
        private Button doorTapZone;
        private Button kitTapZone;
        private Button domeTapZone;
        private readonly List<KeyValuePair<RectTransform, Image>> pendingTapZoneFits = new List<KeyValuePair<RectTransform, Image>>();
        private RectTransform joystickRect;
        private RectTransform joystickKnobRect;
        private Image windowImage;
        private Image doorImage;
        private PuzzleMiniGame puzzleGame;
        private DoorCodeMiniGame codeGame;
        private MissileMiniGame missileGame;

        // walking. avatarX = left/right (room x-fraction), avatarDepth = forward/back
        // (0 = front near the camera, 1 = back against the wall)
        private float avatarX = 0.5f;
        private float avatarDepth = 0.45f;
        private float walkTargetX = -1f;   // where tap-to-walk is heading (negative = none)
        private float walkTargetDepth;
        private int uiMoveX;               // -1/0/+1 from the touch joystick
        private int uiMoveY;               // -1/0/+1 from the touch joystick
        private bool cameraInitialized;
        private float faceDir = 1f;
        private float avatarYaw = 0f;   // 3D avatar turn: +90 right, -90 left, 0 face-camera, 180 back-to-camera
        private float bobTime;

        // what's been done so far
        private string currentZone = "";
        private bool missionComplete;
        private bool resultSubmitted;

        // new mission games (puzzle / code / missile)
        private bool puzzleDone;
        private bool codeDone;
        private bool missileDone;
        private readonly System.Collections.Generic.List<MissionGameResult> gameResults = new System.Collections.Generic.List<MissionGameResult>();

        // runs first; keep only one controller alive
        private void Awake()
        {
            if (activeInstance != null && activeInstance != this)
            {
                // stop a second controller from taking messages
                Destroy(gameObject);
                return;
            }

            activeInstance = this;
            gameObject.name = ControllerObjectName;
        }

        // clean up the active-instance pointer when we get destroyed
        private void OnDestroy()
        {
            CleanupRoom3D();
            CleanupRuntimeAvatar();
            CleanupDisplayCamera();

            if (activeInstance == this)
            {
                activeInstance = null;
            }
        }

        // show the waiting screen until the mission data arrives
        private void Start()
        {
            if (activeInstance != this)
            {
                return;
            }

        #if UNITY_EDITOR
            ApplyMissionJson(
                "{\"activityId\":\"preview\"," +
                "\"groupId\":\"preview\"," +
                "\"mode\":\"preview\"," +
                "\"tasks\":[\"puzzle\",\"code\",\"missile\"]," +
                "\"exercises\":[{\"question\":\"1+1\",\"answer\":\"2\"}]," +
                "\"profile\":{\"username\":\"Test\",\"avatar\":\"\",\"avatarImage\":\"\"}}"
            );
            return;
        #else
            BuildWaitingScreen();
        #endif
        }

        // every frame: build pending mission, then move the avatar around
        private void Update()
        {
            // track phone rotation for every mini-game/stage (not just the missile game)
            MissionTilt.Sample();

        #if UNITY_EDITOR
            // Press L in Play mode to print the current layout of every room prop.
            if (Input.GetKeyDown(KeyCode.L))
            {
                DumpRoomLayout();
            }
        #endif

            if (pendingMissionBuild)
            {
                pendingMissionBuild = false;
                ApplyPendingMission();
                return;
            }

            // refit tap zones once the canvas has a real size (props are built off-frame)
            if (pendingTapZoneFits.Count > 0)
            {
                for (int i = 0; i < pendingTapZoneFits.Count; i += 1)
                {
                    FitTapZoneToSprite(pendingTapZoneFits[i].Key, pendingTapZoneFits[i].Value);
                }

                pendingTapZoneFits.Clear();
            }

            if (avatarRect == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            float previousX = avatarX;
            float previousDepth = avatarDepth;

            int moveX = ReadMoveX();
            int moveY = ReadMoveY();

            if (moveX != 0 || moveY != 0)
            {
                avatarX += moveX * WalkSpeed * dt;
                avatarDepth += moveY * DepthSpeed * dt;
                walkTargetX = -1f; // moving by hand cancels tap-to-walk
            }
            else if (walkTargetX >= 0f)
            {
                avatarX = Mathf.MoveTowards(avatarX, walkTargetX, WalkSpeed * dt);
                avatarDepth = Mathf.MoveTowards(avatarDepth, walkTargetDepth, DepthSpeed * dt);

                if (Mathf.Approximately(avatarX, walkTargetX) && Mathf.Approximately(avatarDepth, walkTargetDepth))
                {
                    walkTargetX = -1f;
                }
            }

            avatarX = Mathf.Clamp(avatarX, MinX, MaxX);
            avatarDepth = Mathf.Clamp01(avatarDepth);

            bool moved = Mathf.Abs(avatarX - previousX) > 0.00001f || Mathf.Abs(avatarDepth - previousDepth) > 0.00001f;
            ApplyAvatarPosition(moved, previousX, previousDepth, dt);
            UpdateZone();
        }

    #if UNITY_EDITOR
        // Editor helper: dump every room prop's current rectangle as the anchorMin/anchorMax
        // pair used by CreateNewImageRoom, plus its scale. While playing, tweak a prop's
        // RectTransform in the Inspector (or drag it), then either press L (Game view focused)
        // or right-click this component header -> "Dump Room Layout", and copy the Console lines.
        [ContextMenu("Dump Room Layout")]
        private void DumpRoomLayout()
        {
            if (roomWorld == null)
            {
                Debug.Log("Mission room layout: room is not built yet.");
                return;
            }

            float parentWidth = roomWorld.rect.width;
            float parentHeight = roomWorld.rect.height;
            if (Mathf.Abs(parentWidth) < 0.0001f || Mathf.Abs(parentHeight) < 0.0001f)
            {
                return;
            }

            var builder = new System.Text.StringBuilder();
            builder.AppendLine("=== Mission room layout (screen-independent; rotation & scale read separately) ===");

            for (int i = 0; i < roomWorld.childCount; i += 1)
            {
                RectTransform child = roomWorld.GetChild(i) as RectTransform;
                if (child == null)
                {
                    continue;
                }

                // Convert anchors + offsets into a pure normalized rect (0..1 of the room),
                // independent of rotation, scale and the current screen size.
                Vector2 anchorMin = child.anchorMin;
                Vector2 anchorMax = child.anchorMax;
                Vector2 offsetMin = child.offsetMin;
                Vector2 offsetMax = child.offsetMax;
                float minX = anchorMin.x + offsetMin.x / parentWidth;
                float maxX = anchorMax.x + offsetMax.x / parentWidth;
                float minY = anchorMin.y + offsetMin.y / parentHeight;
                float maxY = anchorMax.y + offsetMax.y / parentHeight;

                Vector3 rot = child.localEulerAngles;
                Vector3 scale = child.localScale;

                builder.AppendLine(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0,-20} new Vector2({1:0.000}f, {2:0.000}f), new Vector2({3:0.000}f, {4:0.000}f)   // rot ({5:0.0}, {6:0.0}, {7:0.0})  scale ({8:0.000}, {9:0.000})",
                    child.name, minX, minY, maxX, maxY, rot.x, rot.y, rot.z, scale.x, scale.y));
            }

            Debug.Log(builder.ToString());
        }
    #endif

        // the web page calls this to send us the mission once the room has loaded
        [Preserve]
        public void ApplyMissionJson(string json)
        {
            // do the actual rebuild on the next Update (clean frame)
            pendingMissionJson = json ?? "";
            pendingMissionBuild = true;
        }

        // parse the json and actually build the mission room
        private void ApplyPendingMission()
        {
            MissionTilt.Reset();

            try
            {
                payload = JsonUtility.FromJson<MissionRoomPayload>(pendingMissionJson) ?? new MissionRoomPayload();
            }
            catch (Exception error)
            {
                Debug.LogWarning("Could not parse mission payload: " + error.Message);
                payload = new MissionRoomPayload();
            }

            BuildMissionScreen();

            // tell the web page we got it so it stops resending
            MissionResultBridge.NotifyLoaded();
        }

        // just a title + "waiting for data" placeholder screen
        private void BuildWaitingScreen()
        {
            RectTransform root = EnsureCanvas();
            ClearCanvas();

            Text title = CreateText(root, MissionText.Rtl("חדר משימות"), 34, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor);
            title.rectTransform.anchorMin = new Vector2(0.08f, 0.50f);
            title.rectTransform.anchorMax = new Vector2(0.92f, 0.60f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            Text waiting = CreateText(root, MissionText.Rtl("ממתינים לנתוני המשימה..."), 18, FontStyle.Normal, TextAnchor.MiddleCenter, MutedColor);
            waiting.rectTransform.anchorMin = new Vector2(0.08f, 0.42f);
            waiting.rectTransform.anchorMax = new Vector2(0.92f, 0.50f);
            waiting.rectTransform.offsetMin = Vector2.zero;
            waiting.rectTransform.offsetMax = Vector2.zero;
        }

        // build the whole playable room: background, objects, controls
        private void BuildMissionScreen()
        {
            RectTransform root = EnsureCanvas();
            ClearCanvas();
            EnsureEventSystem();
            ResetMissionState();

            // use most of the canvas for the room
            RectTransform room = CreatePanel(root, "Room", new Vector2(0.01f, 0.24f), new Vector2(0.99f, 0.995f), Vector2.zero, Vector2.zero);
            room.GetComponent<Image>().color = new Color32(46, 49, 53, 255);

            CreateRoom(room);
            CreateControls(root);
            RefreshUI();
        }

        // reset all the flags/positions back to the start of a fresh mission
        private void ResetMissionState()
        {
            if (puzzleGame != null) puzzleGame.Close();
            if (codeGame != null) codeGame.Close();
            if (missileGame != null) missileGame.Close();

            puzzleDone = false;
            codeDone = false;
            missileDone = false;
            gameResults.Clear();

            avatarX = 0.5f;
            avatarDepth = 0.45f;
            walkTargetX = -1f;
            walkTargetDepth = 0f;
            uiMoveX = 0;
            uiMoveY = 0;
            cameraInitialized = false;
            faceDir = 1f;
            avatarYaw = 0f;
            bobTime = 0f;
            currentZone = "";
            missionComplete = false;
            resultSubmitted = false;

            avatarRect = null;
            shadowRect = null;
            statusText = null;
            submitButton = null;
            actionButton = null;
            actionButtonLabel = null;
            puzzleLauncherButton = null;
            codeLauncherButton = null;
            missileLauncherButton = null;
            joystickRect = null;
            joystickKnobRect = null;
            roomViewport = null;
            roomWorld = null;
            windowImage = null;
            doorImage = null;
            roomDoor3D = null;
            roomWindow3D = null;
            roomRadioLightRenderer = null;
            roomRadioLight = null;
            roomDoorMarker3D = null;
            roomBoardMarker3D = null;
            roomRadioMarker3D = null;
            doorTapZone = null;
            kitTapZone = null;
            domeTapZone = null;
            pendingTapZoneFits.Clear();
        }

        // lay out the wall, floor and all four objects + the avatar
        private void CreateRoom(RectTransform parent)
        {
            roomViewport = parent;
            roomViewport.GetComponent<Image>().raycastTarget = false;
            if (roomViewport.GetComponent<RectMask2D>() == null)
            {
                roomViewport.gameObject.AddComponent<RectMask2D>();
            }

            roomWorld = CreatePanel(roomViewport, "Room World", new Vector2(0f, 0f), new Vector2(RoomWorldWidth, 1f), Vector2.zero, Vector2.zero);
            Image worldImage = roomWorld.GetComponent<Image>();
            worldImage.color = new Color32(255, 255, 255, 0);
            worldImage.raycastTarget = false;

            CreateNewImageRoom(roomWorld);

            CreateAvatar(roomWorld);
            UpdateRoomCamera(0f);
        }

        // image-based room built from Assets/Resources/NewRoom; games are launched from the visible props
        private void CreateNewImageRoom(RectTransform parent)
        {
            Sprite wallSprite = LoadNewRoomSprite("wall");

            RectTransform backWall = CreatePanel(parent, "Back Wall", new Vector2(0f, WallBottomY - 0.02f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            Image backWallImage = backWall.GetComponent<Image>();
            backWallImage.color = Color.white;
            SetImageSprite(backWallImage, wallSprite, false);
            backWallImage.raycastTarget = false;

            RectTransform floor = CreatePanel(parent, "Floor", new Vector2(0f, 0f), new Vector2(1f, WallBottomY + 0.08f), Vector2.zero, Vector2.zero);
            Image floorImage = floor.GetComponent<Image>();
            floorImage.color = Color.white;
            SetImageSprite(floorImage, LoadNewRoomSprite("floor"), false);
            floorImage.raycastTarget = true;
            AddPointerTrigger(floor.gameObject.AddComponent<EventTrigger>(), EventTriggerType.PointerDown, data => WalkToRoomPoint(data as PointerEventData));

            Image backFloorEdge = CreateImage(parent, "Back Floor Edge", new Vector2(0f, WallBottomY + 0.055f), new Vector2(1f, WallBottomY + 0.062f), new Color32(35, 48, 48, 90));
            backFloorEdge.raycastTarget = false;

            Texture wallTexture = wallSprite != null ? wallSprite.texture : null;
            CreateSideWall(parent, "Left Wall", wallTexture, new Vector2(0f, 0.04f), new Vector2(0.18f, 1f), false, 0.44f, new Color32(240, 242, 240, 255));
            CreateSideWall(parent, "Right Wall", wallTexture, new Vector2(0.82f, 0.04f), new Vector2(1f, 1f), true, 0.44f, new Color32(222, 226, 223, 255));

            Image leftWallEdge = CreateImage(parent, "Left Wall Edge", new Vector2(0.178f, WallBottomY + 0.055f), new Vector2(0.184f, 1f), new Color32(45, 45, 45, 10));
            leftWallEdge.raycastTarget = false;

            Image rightWallEdge = CreateImage(parent, "Right Wall Edge", new Vector2(0.816f, WallBottomY + 0.055f), new Vector2(0.822f, 1f), new Color32(45, 45, 45, 10));
            rightWallEdge.raycastTarget = false;

            Image windowProp = CreateSpriteImage(parent, "Window", LoadNewRoomSprite("window"), new Vector2(0.344f, 0.620f), new Vector2(0.560f, 0.861f), true);
            windowProp.rectTransform.localScale = new Vector3(1.2f, 1f, 1f);
            Image boardImage = CreateSpriteImage(parent, "Board", LoadNewRoomSprite("board"), new Vector2(0.61826f, 0.632f), new Vector2(0.73f, 0.765f), true);
            boardImage.rectTransform.offsetMin = new Vector2(-2.4f, -8.7f);
            boardImage.rectTransform.offsetMax = new Vector2(36.6f, 22.3f);
            boardImage.rectTransform.localScale = new Vector3(1.125f, 1.0291f, 1f);
            CreateBoardChecklist(boardImage.rectTransform);
            RectTransform shelfRect = CreateSpriteImage(parent, "Shelf", LoadNewRoomSprite("shelf"), new Vector2(0.75f, 0.47f), new Vector2(0.93f, 0.62f), true).rectTransform;
            shelfRect.offsetMin = new Vector2(-258.3f, -15.6f);
            shelfRect.offsetMax = new Vector2(-258.3f, -15.6f);
            shelfRect.localScale = new Vector3(0.980f, 1.2139f, 1.4007f);

            RectTransform radioRect = CreateSpriteImage(parent, "Radio", LoadNewRoomSprite("radio"), new Vector2(0.80f, 0.50f), new Vector2(0.94f, 0.62f), true).rectTransform;
            radioRect.offsetMin = new Vector2(-279.6f, 29.1f);
            radioRect.offsetMax = new Vector2(-279.6f, 29.1f);
            radioRect.localScale = new Vector3(0.6756f, 0.8324f, 1f);
            RectTransform fireExtinguisherRect = CreateSpriteImage(parent, "Fire Extinguisher", LoadNewRoomSprite("Fire extinguisher"), new Vector2(0.720f, 0.426f), new Vector2(0.829f, 0.668f), true).rectTransform;
            fireExtinguisherRect.offsetMin = new Vector2(-8f, -22f);
            fireExtinguisherRect.offsetMax = new Vector2(-8f, -22f);
            fireExtinguisherRect.localScale = new Vector3(1.15f, 1.4833f, 1f);
            RectTransform waterRect = CreateSpriteImage(parent, "Water", LoadNewRoomSprite("water"), new Vector2(0.061f, 0.264f), new Vector2(0.161f, 0.414f), true).rectTransform;
            waterRect.offsetMin = new Vector2(12f, -17f);
            waterRect.offsetMax = new Vector2(12f, -17f);

            bool codeTask = HasTask("code");
            Image doorPropImage = CreateSpriteImage(parent, "Door", LoadNewRoomSprite(codeDone ? "closed_door" : "opened_door"),
                new Vector2(0.124f, 0.418f), new Vector2(0.304f, 0.694f), true);
            doorPropImage.rectTransform.offsetMin = new Vector2(5.9f, 8.6f);
            doorPropImage.rectTransform.offsetMax = new Vector2(5.9f, 8.6f);
            doorPropImage.rectTransform.localEulerAngles = new Vector3(0f, 60f, 0f);
            doorPropImage.rectTransform.localScale = new Vector3(1.2533f, 2.286f, 0.8364f);
            doorPropImage.rectTransform.anchoredPosition3D = new Vector3(
                doorPropImage.rectTransform.anchoredPosition3D.x,
                doorPropImage.rectTransform.anchoredPosition3D.y,
                9.6f);
            doorImage = doorPropImage;
            if (codeTask && !codeDone)
            {
                // tapping the door artwork itself opens the code game
                doorTapZone = CreatePropTapZone("Door Tap Zone", doorPropImage, () => OpenGame("code"));
            }

            Image kitImage = CreateSpriteImage(parent, "Emergency Kit", LoadNewRoomSprite("emergency kit"),
                new Vector2(0.052f, 0.265f), new Vector2(0.233f, 0.484f), true);

            kitImage.rectTransform.offsetMin = new Vector2(-37f, -77.1f);
            kitImage.rectTransform.offsetMax = new Vector2(-37f, -77.1f);

            kitImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, 10f);
            if (HasTask("puzzle") && !puzzleDone)
            {
                // the kit overlaps the door + left wall; keep it last so its tap zone sits on top
                kitImage.rectTransform.SetAsLastSibling();
                // tap zone hugs the kit artwork instead of a small box beside it
                kitTapZone = CreatePropTapZone("Emergency Kit Tap Zone", kitImage, () => OpenGame("puzzle"));
            }

            Image domeImage = CreateSpriteImage(parent, "Iron Dome", LoadNewRoomSprite("Iron_dome"),
                new Vector2(0.762f, -0.002f), new Vector2(0.962f, 0.732f), true);
            domeImage.rectTransform.offsetMin = new Vector2(-20f, -15f);
            domeImage.rectTransform.offsetMax = new Vector2(-20f, -15f);
            domeImage.rectTransform.localScale = new Vector3(1.2167f, 1.6667f, 1f);
            if (HasTask("missile") && !missileDone)
            {
                // tap zone hugs the iron dome artwork, not the tall bounding box around it
                domeTapZone = CreatePropTapZone("Iron Dome Tap Zone", domeImage, () => OpenGame("missile"));
            }
        }

        // chalk "safe room" checklist written on the green board sprite
        private void CreateBoardChecklist(RectTransform board)
        {
            if (board == null)
            {
                return;
            }

            // legacy uGUI Text renders LTR, so reverse each whole line (words + markers) so the
            // multi-word title and the numbered list all read right-to-left
            string content =
                MissionText.RtlLine("רשימת ציוד לממד:") + "\n" +
                MissionText.RtlLine("1( רדיו") + "\n" +
                MissionText.RtlLine("2( מים") + "\n" +
                MissionText.RtlLine("3( פלאפון") + "\n" +
                MissionText.RtlLine("4( פנס");

            Text list = CreateText(board, content, 14, FontStyle.Bold, TextAnchor.UpperRight, new Color32(245, 247, 240, 255));
            list.raycastTarget = false;
            list.lineSpacing = 1.05f;
            list.resizeTextForBestFit = true;
            list.resizeTextMinSize = 6;
            list.resizeTextMaxSize = 22;

            // keep the chalk on the green surface: the board sprite is letterboxed
            // (preserveAspect) into the centre of its rect, clear of the metal frame
            RectTransform rect = list.rectTransform;
            rect.anchorMin = new Vector2(0.30f, 0.16f);
            rect.anchorMax = new Vector2(0.70f, 0.84f);
            rect.offsetMin = new Vector2(-7.9f, 0f);
            rect.offsetMax = new Vector2(-7.9f, 0f);
        }

        // fallback room for editor/import states where the 3D room assets are not available yet
        private void CreateSpriteRoom(RectTransform parent)
        {
            // make the wall part of the room
            RectTransform wall = CreatePanel(parent, "Wall", new Vector2(0f, WallBottomY), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            Image wallImage = wall.GetComponent<Image>();
            wallImage.color = WallFallbackColor;
            SetImageSprite(wallImage, LoadRoomSprite("back_wall"), false);
            wallImage.raycastTarget = false;

            // soft shade from the top for a bit of depth
            CreateSpriteImage(wall, "Wall Shade", GetVerticalShadeSprite(), Vector2.zero, Vector2.one, false);
            CreateSpriteImage(wall, "Left Side Wall", LoadRoomSprite("left_wall"), new Vector2(0f, 0f), new Vector2(0.16f, 1f), false);
            CreateSpriteImage(wall, "Right Side Wall", LoadRoomSprite("right_wall"), new Vector2(0.84f, 0f), new Vector2(1f, 1f), false);

            // draw the floor slightly over the wall
            RectTransform floor = CreatePanel(parent, "Floor", new Vector2(0f, 0f), new Vector2(1f, WallBottomY + 0.04f), Vector2.zero, Vector2.zero);
            Image floorImage = floor.GetComponent<Image>();
            floorImage.color = WallFallbackColor;
            SetImageSprite(floorImage, LoadRoomSprite("floor"), false);
            floorImage.raycastTarget = true;
            AddPointerTrigger(floor.gameObject.AddComponent<EventTrigger>(), EventTriggerType.PointerDown, data => WalkToRoomPoint(data as PointerEventData));

            Image backFloorEdge = CreateImage(floor, "Back Floor Edge", new Vector2(0f, 0.86f), new Vector2(1f, 0.88f), new Color32(18, 40, 46, 72));
            backFloorEdge.raycastTarget = false;
            Image nearFloorShade = CreateImage(floor, "Near Floor Shade", new Vector2(0f, 0f), new Vector2(1f, 0.12f), new Color32(7, 16, 21, 58));
            nearFloorShade.raycastTarget = false;

            // put the four task objects on the wall

            // door, on the left, sitting on the wall/floor line
            bool codeTask = HasTask("code");
            Button doorButton = CreateSpriteButton(parent, "Door",
                LoadRoomSprite("door"),
                new Vector2(0.03f, 0.38f), new Vector2(0.19f, 0.72f),
                codeTask ? (UnityAction)(() => OpenGame("code")) : null, true);
            doorImage = doorButton.GetComponent<Image>();

            // emergency kit, left wall shelf
            if (HasTask("puzzle"))
            {
                Button kitButton = CreateButton(parent, "Emergency Kit", MissionText.Rtl("משחק פאזל"),
                    new Vector2(0.19f, 0.43f), new Vector2(0.36f, 0.53f),
                    () => OpenGame("puzzle"));
                kitButton.GetComponent<Image>().color = new Color32(205, 66, 72, 235);
            }

            // window, center-left (decorative in the new mission set)
            Button windowButton = CreateSpriteButton(parent, "Window",
                LoadRoomSprite("close_window"),
                new Vector2(0.28f, 0.48f), new Vector2(0.46f, 0.72f),
                null, true);
            windowImage = windowButton.GetComponent<Image>();

            // board, center-right (a bit smaller than the window)
            CreateSpriteButton(parent, "Board", LoadRoomSprite("board"),
                new Vector2(0.58f, 0.51f), new Vector2(0.75f, 0.72f),
                null, true);

            // radio, far right (just the radio unit, cropped out of the shelf image)
            CreateSpriteButton(parent, "Radio", LoadRoomSprite("radio"),
                new Vector2(0.82f, 0.45f), new Vector2(0.98f, 0.70f),
                null, true);

            if (HasTask("missile"))
            {
                Button domeButton = CreateButton(parent, "Iron Dome", MissionText.Rtl("הטילים משחק"),
                    new Vector2(0.72f, 0.33f), new Vector2(0.91f, 0.44f),
                    () => OpenGame("missile"));
                domeButton.GetComponent<Image>().color = new Color32(92, 130, 148, 235);
            }
        }

        // transparent hit boxes over the 3D render keep the existing movement/task logic intact
        private void Create3DInteractionLayer(RectTransform parent)
        {
            Image floorHit = CreateImage(parent, "3D Floor Tap Surface", new Vector2(0f, 0f), new Vector2(1f, WallBottomY + 0.04f), new Color32(255, 255, 255, 1));
            floorHit.raycastTarget = true;
            AddPointerTrigger(floorHit.gameObject.AddComponent<EventTrigger>(), EventTriggerType.PointerDown, data => WalkToRoomPoint(data as PointerEventData));

            bool codeTask = HasTask("code");
            CreateSpriteButton(parent, "Door Hit Area", null,
                new Vector2(0.03f, 0.38f), new Vector2(0.19f, 0.72f),
                codeTask ? (UnityAction)(() => OpenGame("code")) : null, false);

            CreateSpriteButton(parent, "Emergency Kit Hit Area", null,
                new Vector2(0.195f, 0.43f), new Vector2(0.235f, 0.51f),
                HasTask("puzzle") ? (UnityAction)(() => OpenGame("puzzle")) : null, false);

            CreateSpriteButton(parent, "Iron Dome Hit Area", null,
                new Vector2(0.70f, 0.28f), new Vector2(0.93f, 0.50f),
                HasTask("missile") ? (UnityAction)(() => OpenGame("missile")) : null, false);
        }

        // build a render-to-texture room from Assets/Resources/Room3D
        private bool TryCreateRoom3D(RectTransform parent)
        {
            if (LoadRoom3DModel("floor") == null || LoadRoom3DModel("wall") == null)
            {
                return false;
            }

            try
            {
                CleanupRoom3D();

                roomRenderRoot = new GameObject("Mission Room 3D Render Root");
                roomRenderRoot.transform.SetParent(transform, false);
                roomRenderRoot.transform.localPosition = new Vector3(0f, -500f, 0f);

                roomRenderTexture = new RenderTexture(RoomRenderTextureWidth, RoomRenderTextureHeight, 24, RenderTextureFormat.ARGB32)
                {
                    name = "Mission Room 3D Texture",
                    useMipMap = false,
                    autoGenerateMips = false
                };
                roomRenderTexture.Create();

                RawImage roomImage = CreateRawImage(parent, "3D Room Render", roomRenderTexture, Vector2.zero, Vector2.one);
                roomImage.raycastTarget = false;

                var sceneObject = new GameObject("Mission Room 3D Geometry");
                sceneObject.transform.SetParent(roomRenderRoot.transform, false);
                BuildRoom3DGeometry(sceneObject.transform);
                SetLayerRecursively(sceneObject, RoomRenderLayer);

                BuildRoom3DCamera();
                if (roomRenderCamera != null)
                {
                    roomRenderCamera.Render();
                }

                return true;
            }
            catch (Exception error)
            {
                Debug.LogWarning("Could not build 3D mission room: " + error.Message, this);
                CleanupRoom3D();
                return false;
            }
        }

        // place the floor, three walls and mission objects in the 3D render scene
        private void BuildRoom3DGeometry(Transform scene)
        {
            Material floorMaterial = CreateRoom3DMaterial("floor", Color.white, false, true, true);
            Material wallMaterial = CreateRoom3DMaterial("wall", Color.white, false, true, true);
            Material doorMaterial = CreateRoom3DMaterial("door", new Color32(160, 106, 72, 255));
            Material windowMaterial = CreateRoom3DMaterial("Window", new Color32(168, 215, 228, 255));
            Material boardMaterial = CreateRoom3DMaterial("Board", new Color32(66, 95, 84, 255));
            Material radioMaterial = CreateRoom3DMaterial("radio", new Color32(56, 65, 69, 255));

            CreateRoom3DShellSurface(scene, "Floor 3D", Floor3DPosition, Floor3DRotation, Floor3DScale, floorMaterial,
                new Vector3(-Room3DWidth * 0.5f, 0f, -Room3DDepth * 0.25f),
                new Vector3(-Room3DWidth * 0.5f, 0f, Room3DDepth * 0.25f),
                new Vector3(Room3DWidth * 0.5f, 0f, Room3DDepth * 0.25f),
                new Vector3(Room3DWidth * 0.5f, 0f, -Room3DDepth * 0.25f));

            CreateRoom3DShellSurface(scene, "Back Wall 3D", BackWall3DPosition, BackWall3DRotation, BackWall3DScale, wallMaterial,
                new Vector3(-Room3DWidth * 0.5f, -Room3DHeight * 0.25f, 0f),
                new Vector3(-Room3DWidth * 0.5f, Room3DHeight * 0.25f, 0f),
                new Vector3(Room3DWidth * 0.5f, Room3DHeight * 0.25f, 0f),
                new Vector3(Room3DWidth * 0.5f, -Room3DHeight * 0.25f, 0f));

            CreateRoom3DShellSurface(scene, "Left Wall 3D", LeftWall3DPosition, LeftWall3DRotation, LeftWall3DScale, wallMaterial,
                new Vector3(0f, -Room3DHeight * 0.25f, 0f),
                new Vector3(0f, Room3DHeight * 0.25f, 0f),
                new Vector3(Room3DDepth * 0.5f, Room3DHeight * 0.25f, 0f),
                new Vector3(Room3DDepth * 0.5f, -Room3DHeight * 0.25f, 0f));

            CreateRoom3DShellSurface(scene, "Right Wall 3D", RightWall3DPosition, RightWall3DRotation, RightWall3DScale, wallMaterial,
                new Vector3(0f, -Room3DHeight * 0.25f, 0f),
                new Vector3(0f, Room3DHeight * 0.25f, 0f),
                new Vector3(Room3DDepth * 0.5f, Room3DHeight * 0.25f, 0f),
                new Vector3(Room3DDepth * 0.5f, -Room3DHeight * 0.25f, 0f));

            bool doorTask = HasTask("code");
            GameObject door = PlaceRoom3DModel(scene, "door", "Door 3D",
                BackWallPoint(DoorX, 1.1f, 0.08f), Quaternion.Euler(0f, doorTask ? -16f : 0f, 0f),
                new Vector3(1.25f, 2.25f, 0.22f), Room3DFitMode.UniformFit,
                new Vector3(0.5f, 0f, 0.5f), doorMaterial);
            roomDoor3D = door != null ? door.transform : null;

            GameObject window = PlaceRoom3DModel(scene, "Window", "Window 3D",
                BackWallPoint(WindowX, 1.85f, 0.10f), Quaternion.identity,
                new Vector3(1.55f, 1.15f, 0.20f), Room3DFitMode.UniformFit,
                new Vector3(0.5f, 0.5f, 0.5f), windowMaterial);
            roomWindow3D = window != null ? window.transform : null;

            PlaceRoom3DModel(scene, "Board", "Board 3D",
                BackWallPoint(BoardX, 1.78f, 0.09f), Quaternion.identity,
                new Vector3(1.85f, 1.15f, 0.16f), Room3DFitMode.UniformFit,
                new Vector3(0.5f, 0.5f, 0.5f), boardMaterial);

            PlaceRoom3DModel(scene, "radio", "Radio 3D",
                Radio3DPosition, Radio3DRotation, Radio3DScale, radioMaterial);

            PlaceRoom3DModel(scene, "chair", "Chair 3D",
                Room3DPoint(0.18f, 0.42f, 0f), Quaternion.Euler(0f, 58f, 0f),
                new Vector3(0.9f, 1.05f, 0.9f), Room3DFitMode.UniformFit,
                new Vector3(0.5f, 0f, 0.5f), CreateRoom3DMaterial("chair", new Color32(128, 88, 62, 255)));

            PlaceRoom3DModel(scene, "emergency kit", "Emergency Kit 3D",
                EmergencyKit3DPosition, EmergencyKit3DRotation, EmergencyKit3DScale,
                CreateRoom3DMaterial("emergency kit", new Color32(204, 66, 72, 255)));

            PlaceRoom3DModel(scene, "Fire extinguisher", "Fire Extinguisher 3D",
                BackWallPoint(0.23f, 0.7f, 0.22f), Quaternion.identity,
                new Vector3(0.42f, 1.12f, 0.42f), Room3DFitMode.UniformFit,
                new Vector3(0.5f, 0f, 0.5f), CreateRoom3DMaterial("Fire extinguisher", new Color32(205, 38, 45, 255)));

            PlaceRoom3DModel(scene, "Iron dome", "Iron Dome 3D",
                IronDome3DPosition, IronDome3DRotation, IronDome3DScale,
                CreateRoom3DMaterial("Iron dome", new Color32(118, 139, 138, 255)));

            CreateTaskMarkers(scene);
            CreateRadioIndicator(scene);
        }

        // camera and lights that render only the 3D room layer
        private void BuildRoom3DCamera()
        {
            var cameraObject = new GameObject("Mission Room 3D Camera", typeof(Camera));
            cameraObject.transform.SetParent(roomRenderRoot.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 2.25f, Room3DFrontZ - 2.35f);
            cameraObject.transform.LookAt(roomRenderRoot.transform.TransformPoint(new Vector3(0f, 1.35f, Room3DBackZ - 1.2f)), Vector3.up);

            roomRenderCamera = cameraObject.GetComponent<Camera>();
            roomRenderCamera.clearFlags = CameraClearFlags.SolidColor;
            roomRenderCamera.backgroundColor = new Color32(205, 239, 245, 255);
            roomRenderCamera.cullingMask = 1 << RoomRenderLayer;
            roomRenderCamera.fieldOfView = 54f;
            roomRenderCamera.nearClipPlane = 0.02f;
            roomRenderCamera.farClipPlane = 40f;
            roomRenderCamera.targetTexture = roomRenderTexture;

            var keyLightObject = new GameObject("Mission Room 3D Key Light", typeof(Light));
            keyLightObject.transform.SetParent(roomRenderRoot.transform, false);
            keyLightObject.transform.localRotation = Quaternion.Euler(48f, -32f, 0f);
            Light keyLight = keyLightObject.GetComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.15f;
            keyLight.cullingMask = 1 << RoomRenderLayer;

            var fillLightObject = new GameObject("Mission Room 3D Fill Light", typeof(Light));
            fillLightObject.transform.SetParent(roomRenderRoot.transform, false);
            fillLightObject.transform.localPosition = new Vector3(0f, 2.4f, -0.8f);
            Light fillLight = fillLightObject.GetComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.range = 11f;
            fillLight.intensity = 1.7f;
            fillLight.color = new Color32(218, 247, 255, 255);
            fillLight.cullingMask = 1 << RoomRenderLayer;
        }

        private GameObject CreateRoom3DShellSurface(
            Transform parent,
            string name,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            Material material,
            Vector3 bottomLeft,
            Vector3 topLeft,
            Vector3 topRight,
            Vector3 bottomRight)
        {
            var surfaceObject = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            surfaceObject.transform.SetParent(parent, false);
            surfaceObject.transform.localPosition = localPosition;
            surfaceObject.transform.localRotation = localRotation;
            surfaceObject.transform.localScale = localScale;

            var mesh = new Mesh
            {
                name = name + " Mesh",
                vertices = new[] { bottomLeft, topLeft, topRight, bottomRight },
                uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right },
                triangles = new[] { 0, 1, 2, 0, 2, 3, 2, 1, 0, 3, 2, 0 }
            };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            MeshFilter filter = surfaceObject.GetComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            roomRenderMeshes.Add(mesh);

            MeshRenderer renderer = surfaceObject.GetComponent<MeshRenderer>();
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            SetLayerRecursively(surfaceObject, RoomRenderLayer);
            return surfaceObject;
        }

        private GameObject PlaceRoom3DModel(
            Transform parent,
            string folder,
            string name,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            Material material)
        {
            return PlaceRoom3DModel(parent, folder, name, localPosition, localRotation, localScale,
                Vector3.one, Room3DFitMode.Stretch, Vector3.zero, material, false);
        }

        private GameObject PlaceRoom3DModel(
            Transform parent,
            string folder,
            string name,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 targetSize,
            Room3DFitMode fitMode,
            Vector3 pivot,
            Material material)
        {
            return PlaceRoom3DModel(parent, folder, name, localPosition, localRotation, Vector3.one,
                targetSize, fitMode, pivot, material, true);
        }

        private GameObject PlaceRoom3DModel(
            Transform parent,
            string folder,
            string name,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            Vector3 targetSize,
            Room3DFitMode fitMode,
            Vector3 pivot,
            Material material,
            bool fitToBounds)
        {
            GameObject prefab = LoadRoom3DModel(folder);
            if (prefab == null)
            {
                return null;
            }

            var slot = new GameObject(name);
            slot.transform.SetParent(parent, false);
            slot.transform.localPosition = localPosition;
            slot.transform.localRotation = Quaternion.identity;
            slot.transform.localScale = localScale;

            GameObject instance = Instantiate(prefab, slot.transform);
            instance.name = folder + " Model";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            if (material != null)
            {
                ApplyRoom3DMaterial(instance, material);
            }

            if (fitToBounds)
            {
                FitRoom3DModel(instance.transform, targetSize, fitMode, pivot);
            }

            slot.transform.localRotation = localRotation;
            SetLayerRecursively(slot, RoomRenderLayer);
            return slot;
        }

        private void CreateTaskMarkers(Transform scene)
        {
            Color glowColor = new Color32(48, 219, 152, 255);

            if (HasTask("code") && !codeDone) roomDoorMarker3D = AddTaskMarker(scene, BackWallPoint(DoorX, 2.45f, 0.22f), 0.18f, glowColor);
            if (HasTask("puzzle") && !puzzleDone) roomBoardMarker3D = AddTaskMarker(scene, EmergencyKit3DPosition + new Vector3(0f, 0.55f, 0f), 0.16f, glowColor);
            if (HasTask("missile") && !missileDone) roomRadioMarker3D = AddTaskMarker(scene, IronDome3DPosition + new Vector3(0f, 0.45f, 0f), 0.18f, glowColor);
        }

        private GameObject AddTaskMarker(Transform parent, Vector3 localPosition, float size, Color color)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "Task Glow";
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = localPosition;
            marker.transform.localScale = Vector3.one * size;

            Collider collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateRoom3DGlowMaterial("Task Glow Material", color, 1.8f);
            }

            SetLayerRecursively(marker, RoomRenderLayer);
            return marker;
        }

        private void CreateRadioIndicator(Transform parent)
        {
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "Radio Status Light 3D";
            indicator.transform.SetParent(parent, false);
            indicator.transform.localPosition = Radio3DPosition + new Vector3(0f, 0.35f, 0f);
            indicator.transform.localScale = Vector3.one * 0.12f;

            Collider collider = indicator.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            roomRadioLightRenderer = indicator.GetComponent<Renderer>();
            if (roomRadioLightRenderer != null)
            {
                roomRadioLightRenderer.sharedMaterial = CreateRoom3DGlowMaterial("Radio Status Light Material", Color.gray, 0.2f);
            }

            var lightObject = new GameObject("Radio Status Point Light", typeof(Light));
            lightObject.transform.SetParent(indicator.transform, false);
            roomRadioLight = lightObject.GetComponent<Light>();
            roomRadioLight.type = LightType.Point;
            roomRadioLight.range = 1.25f;
            roomRadioLight.cullingMask = 1 << RoomRenderLayer;

            SetLayerRecursively(indicator, RoomRenderLayer);
            SetRadio3DOn(false);
        }

        private static GameObject LoadRoom3DModel(string folder)
        {
            string root = "Room3D/" + folder + "/";
            return Resources.Load<GameObject>(root + "base")
                ?? Resources.Load<GameObject>(root + "base_basic_shaded")
                ?? Resources.Load<GameObject>(root + "base_basic_pbr");
        }

        private Material CreateRoom3DMaterial(string folder, Color fallbackColor, bool tintTexture = false, bool preferShaded = false, bool unlit = false)
        {
            Shader shader = unlit
                ? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Unlit/Color")
                : Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
            var material = new Material(shader)
            {
                name = "Mission Room " + folder + " Material"
            };

            Texture2D shaded = Resources.Load<Texture2D>("Room3D/" + folder + "/shaded");
            Texture2D diffuse = Resources.Load<Texture2D>("Room3D/" + folder + "/texture_diffuse");
            Texture2D texture = preferShaded ? shaded ?? diffuse : diffuse ?? shaded;

            SetRoom3DMaterialColor(material, texture != null && !tintTexture ? Color.white : fallbackColor, 0f);
            if (texture != null)
            {
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.18f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.02f);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);

            roomRenderMaterials.Add(material);
            return material;
        }

        private Material CreateRoom3DGlowMaterial(string name, Color color, float emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = name
            };

            SetRoom3DMaterialColor(material, color, emission);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);

            roomRenderMaterials.Add(material);
            return material;
        }

        private static void SetRoom3DMaterialColor(Material material, Color color, float emission)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
            }
        }

        private static void ApplyRoom3DMaterial(GameObject root, Material material)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i += 1)
            {
                Renderer renderer = renderers[i];
                Material[] materials = renderer.sharedMaterials;

                if (materials == null || materials.Length == 0)
                {
                    renderer.sharedMaterial = material;
                    continue;
                }

                for (int j = 0; j < materials.Length; j += 1)
                {
                    materials[j] = material;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static void FitRoom3DModel(Transform instance, Vector3 targetSize, Room3DFitMode fitMode, Vector3 pivot)
        {
            if (!TryGetLocalRendererBounds(instance.gameObject, out Bounds bounds))
            {
                return;
            }

            Vector3 ratios = new Vector3(
                AxisScale(targetSize.x, bounds.size.x),
                AxisScale(targetSize.y, bounds.size.y),
                AxisScale(targetSize.z, bounds.size.z));

            Vector3 scale = ratios;
            if (fitMode != Room3DFitMode.Stretch)
            {
                float uniform = fitMode == Room3DFitMode.UniformFill
                    ? Mathf.Max(ratios.x, Mathf.Max(ratios.y, ratios.z))
                    : Mathf.Min(ratios.x, Mathf.Min(ratios.y, ratios.z));
                scale = Vector3.one * uniform;
            }

            instance.localScale = scale;

            Vector3 scaledMin = Vector3.Scale(bounds.min, scale);
            Vector3 scaledMax = Vector3.Scale(bounds.max, scale);
            Vector3 anchor = new Vector3(
                Mathf.Lerp(scaledMin.x, scaledMax.x, pivot.x),
                Mathf.Lerp(scaledMin.y, scaledMax.y, pivot.y),
                Mathf.Lerp(scaledMin.z, scaledMax.z, pivot.z));
            instance.localPosition = -anchor;
        }

        private static float AxisScale(float target, float source)
        {
            if (target <= 0.0001f || source <= 0.0001f)
            {
                return 1f;
            }

            return target / source;
        }

        private static bool TryGetLocalRendererBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bounds = new Bounds(Vector3.zero, Vector3.one);
            bool found = false;

            Matrix4x4 worldToLocal = root.transform.worldToLocalMatrix;
            for (int i = 0; i < renderers.Length; i += 1)
            {
                Bounds worldBounds = renderers[i].bounds;
                Vector3 min = worldBounds.min;
                Vector3 max = worldBounds.max;

                for (int corner = 0; corner < 8; corner += 1)
                {
                    Vector3 worldCorner = new Vector3(
                        (corner & 1) == 0 ? min.x : max.x,
                        (corner & 2) == 0 ? min.y : max.y,
                        (corner & 4) == 0 ? min.z : max.z);
                    Vector3 localCorner = worldToLocal.MultiplyPoint3x4(worldCorner);

                    if (!found)
                    {
                        bounds = new Bounds(localCorner, Vector3.zero);
                        found = true;
                    }
                    else
                    {
                        bounds.Encapsulate(localCorner);
                    }
                }
            }

            return found;
        }

        private static Vector3 Room3DPoint(float x, float depth, float height)
        {
            return new Vector3(
                Mathf.Lerp(-Room3DWidth * 0.5f, Room3DWidth * 0.5f, Mathf.Clamp01(x)),
                height,
                Mathf.Lerp(Room3DFrontZ, Room3DBackZ, Mathf.Clamp01(depth)));
        }

        private static Vector3 BackWallPoint(float x, float height, float forwardOffset)
        {
            return new Vector3(
                Mathf.Lerp(-Room3DWidth * 0.5f, Room3DWidth * 0.5f, Mathf.Clamp01(x)),
                height,
                Room3DBackZ - forwardOffset);
        }

        // did the admin pick this task for the room?
        private bool HasTask(string task)
        {
            if (payload.tasks == null)
            {
                return false;
            }

            for (int i = 0; i < payload.tasks.Length; i += 1)
            {
                if (payload.tasks[i] == task)
                {
                    return true;
                }
            }

            return false;
        }

        // make the movement stick, action button, submit button and status text
        private void CreateControls(RectTransform root)
        {
            // touch joystick on the left; arrow keys / WASD still work on desktop
            CreateMovementJoystick(root);

            actionButton = CreateButton(root, "Action", MissionText.Rtl("פעולה"), new Vector2(0.33f, 0.135f), new Vector2(0.65f, 0.225f), OnAction);
            actionButton.GetComponent<Image>().color = new Color32(41, 179, 106, 235);
            actionButtonLabel = actionButton.GetComponentInChildren<Text>();
            actionButton.gameObject.SetActive(false);

            submitButton = CreateButton(root, "Submit", MissionText.Rtl("סיום"), new Vector2(0.67f, 0.135f), new Vector2(0.98f, 0.225f), SubmitMission);
            submitButton.interactable = false;

            statusText = CreateText(root, "", 15, FontStyle.Bold, TextAnchor.MiddleCenter, MutedColor);
            statusText.rectTransform.anchorMin = new Vector2(0.04f, 0.255f);
            statusText.rectTransform.anchorMax = new Vector2(0.96f, 0.298f);
            statusText.rectTransform.offsetMin = Vector2.zero;
            statusText.rectTransform.offsetMax = Vector2.zero;
            statusText.resizeTextForBestFit = true;
            statusText.resizeTextMinSize = 10;
            statusText.resizeTextMaxSize = 15;
            statusText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        // game-style movement control, replacing the old four text-arrow buttons
        private void CreateMovementJoystick(RectTransform root)
        {
            var stickObject = new GameObject("Move Joystick", typeof(RectTransform), typeof(Image));
            stickObject.transform.SetParent(root, false);
            joystickRect = stickObject.GetComponent<RectTransform>();
            joystickRect.anchorMin = joystickRect.anchorMax = new Vector2(0.15f, 0.125f);
            joystickRect.sizeDelta = new Vector2(118, 118);
            joystickRect.anchoredPosition = Vector2.zero;

            Image stickImage = stickObject.GetComponent<Image>();
            stickImage.sprite = GetSoftCircleSprite();
            stickImage.color = new Color32(11, 69, 81, 96);
            stickImage.raycastTarget = true;

            Image innerGlow = CreateSpriteImage(joystickRect, "Joystick Inner Glow", GetSoftCircleSprite(),
                new Vector2(0.22f, 0.22f), new Vector2(0.78f, 0.78f), true);
            innerGlow.color = new Color32(50, 214, 151, 52);

            var knobObject = new GameObject("Joystick Knob", typeof(RectTransform), typeof(Image));
            knobObject.transform.SetParent(joystickRect, false);
            joystickKnobRect = knobObject.GetComponent<RectTransform>();
            joystickKnobRect.anchorMin = joystickKnobRect.anchorMax = new Vector2(0.5f, 0.5f);
            joystickKnobRect.sizeDelta = new Vector2(46, 46);
            joystickKnobRect.anchoredPosition = Vector2.zero;

            Image knobImage = knobObject.GetComponent<Image>();
            knobImage.sprite = GetSoftCircleSprite();
            knobImage.color = new Color32(41, 179, 106, 222);
            knobImage.raycastTarget = false;

            EventTrigger trigger = stickObject.AddComponent<EventTrigger>();
            AddPointerTrigger(trigger, EventTriggerType.PointerDown, data => UpdateJoystick(data as PointerEventData));
            AddPointerTrigger(trigger, EventTriggerType.Drag, data => UpdateJoystick(data as PointerEventData));
            AddPointerTrigger(trigger, EventTriggerType.PointerUp, _ => ResetJoystick());
            AddPointerTrigger(trigger, EventTriggerType.EndDrag, _ => ResetJoystick());
        }

        // map the finger position on the joystick pad into the existing movement values
        private void UpdateJoystick(PointerEventData eventData)
        {
            if (eventData == null || joystickRect == null)
            {
                return;
            }

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickRect, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                return;
            }

            float radius = Mathf.Min(joystickRect.rect.width, joystickRect.rect.height) * 0.5f;
            if (radius <= 0.0001f)
            {
                return;
            }

            Vector2 direction = Vector2.ClampMagnitude(localPoint / radius, 1f);
            const float DeadZone = 0.22f;

            uiMoveX = Mathf.Abs(direction.x) > DeadZone ? (direction.x > 0f ? 1 : -1) : 0;
            uiMoveY = Mathf.Abs(direction.y) > DeadZone ? (direction.y > 0f ? 1 : -1) : 0;

            if (joystickKnobRect != null)
            {
                joystickKnobRect.anchoredPosition = direction * (radius * 0.46f);
                float activeScale = uiMoveX != 0 || uiMoveY != 0 ? 1.08f : 1f;
                joystickKnobRect.localScale = new Vector3(activeScale, activeScale, 1f);
            }
        }

        // stop touch movement and return the joystick to center
        private void ResetJoystick()
        {
            uiMoveX = 0;
            uiMoveY = 0;

            if (joystickKnobRect != null)
            {
                joystickKnobRect.anchoredPosition = Vector2.zero;
                joystickKnobRect.localScale = Vector3.one;
            }
        }

        // spawn the player avatar and its shadow
        private void CreateAvatar(RectTransform parent)
        {
            float feetY = Mathf.Lerp(FloorFrontY, FloorBackY, avatarDepth);

            // soft shadow on the ground (made first so it's behind the avatar)
            var shadow = new GameObject("Avatar Shadow", typeof(RectTransform), typeof(Image));
            shadow.transform.SetParent(parent, false);
            shadowRect = shadow.GetComponent<RectTransform>();
            shadowRect.anchorMin = shadowRect.anchorMax = new Vector2(avatarX, feetY - 0.005f);
            shadowRect.sizeDelta = new Vector2(100, 24);
            Image shadowImage = shadow.GetComponent<Image>();
            shadowImage.raycastTarget = false;
            shadowImage.sprite = GetSoftCircleSprite();
            shadowImage.color = new Color32(7, 16, 21, 90);

            // avatar stands on the floor: pivot at the feet
            var avatar = new GameObject("Avatar", typeof(RectTransform));
            avatar.transform.SetParent(parent, false);
            avatarRect = avatar.GetComponent<RectTransform>();
            avatarRect.pivot = new Vector2(0.5f, 0f);
            avatarRect.anchorMin = avatarRect.anchorMax = new Vector2(avatarX, feetY);
            avatarRect.sizeDelta = new Vector2(116, 196);
            avatarRect.anchoredPosition = Vector2.zero;

            if (TryShowRuntimeAvatar(avatarRect))
            {
                return;
            }

            Image image = avatar.AddComponent<Image>();
            image.raycastTarget = false;
            Sprite avatarSprite = SpriteFromDataUrl(payload.profile.avatarImage);

            if (avatarSprite != null)
            {
                image.sprite = avatarSprite;
                image.color = Color.white;
                image.preserveAspect = true;
                return;
            }

            // no 3D avatar or saved image? just show a circle with their first letter
            image.sprite = GetSoftCircleSprite();
            image.color = new Color32(122, 157, 147, 255);
            Text initial = CreateText(avatar.transform, InitialFor(payload.profile.username), 40, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            initial.raycastTarget = false;
            initial.rectTransform.anchorMin = Vector2.zero;
            initial.rectTransform.anchorMax = Vector2.one;
            initial.rectTransform.offsetMin = Vector2.zero;
            initial.rectTransform.offsetMax = Vector2.zero;
        }

        // render the saved pack character into the 2D mission-room UI
        private bool TryShowRuntimeAvatar(RectTransform avatarContainer)
        {
            try
            {
                PrepareRuntimeAvatarRenderRig();
                ApplyRuntimeAvatarSelection();
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
                Debug.LogWarning("Could not render runtime avatar in mission room: " + error.Message, this);
                return false;
            }
        }

        // create the hidden camera setup that renders the 3D character into the room UI
        private void PrepareRuntimeAvatarRenderRig()
        {
            if (avatarRenderRoot != null)
            {
                return;
            }

            avatarRenderRoot = new GameObject("Mission Runtime Avatar Render Rig");
            avatarRenderRoot.transform.SetParent(transform, false);
            avatarRenderRoot.transform.position = new Vector3(0f, -250f, 0f);

            avatarRenderAvatarRoot = new GameObject("Avatar Preview Root").transform;
            avatarRenderAvatarRoot.SetParent(avatarRenderRoot.transform, false);

            var spawnerObject = new GameObject("Mission Avatar Spawner", typeof(CharacterSpawner));
            spawnerObject.transform.SetParent(avatarRenderRoot.transform, false);
            avatarRenderSpawner = spawnerObject.GetComponent<CharacterSpawner>();
            avatarRenderSpawner.mountPoint = avatarRenderAvatarRoot;

            avatarRenderTexture = new RenderTexture(384, 640, 24, RenderTextureFormat.ARGB32)
            {
                name = "Mission Runtime Avatar Texture",
                useMipMap = false,
                autoGenerateMips = false
            };
            avatarRenderTexture.Create();

            var cameraObject = new GameObject("Mission Avatar Camera", typeof(Camera));
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

            var lightObject = new GameObject("Mission Avatar Light", typeof(Light));
            lightObject.transform.SetParent(avatarRenderRoot.transform, false);
            lightObject.transform.localRotation = Quaternion.Euler(45f, -25f, 0f);
            Light avatarLight = lightObject.GetComponent<Light>();
            avatarLight.type = LightType.Directional;
            avatarLight.intensity = 1.25f;
            avatarLight.cullingMask = 1 << RuntimeAvatarLayer;
        }

        // spawn the saved pack character into the hidden render rig
        private void ApplyRuntimeAvatarSelection()
        {
            if (avatarRenderSpawner == null)
            {
                return;
            }

            avatarRenderSpawner.Show(payload.profile.avatar);
            ApplyRuntimeAvatarFacing();
        }

        // frame the avatar full-body with the feet near the bottom of the portrait texture
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

        // show the render texture in place of the fallback avatar art
        private void ShowRuntimeAvatarTexture(RectTransform avatarContainer)
        {
            if (avatarContainer == null || avatarRenderTexture == null)
            {
                return;
            }

            Image fallbackImage = avatarContainer.GetComponent<Image>();
            if (fallbackImage != null)
            {
                Destroy(fallbackImage);
            }

            for (int i = avatarContainer.childCount - 1; i >= 0; i -= 1)
            {
                Destroy(avatarContainer.GetChild(i).gameObject);
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

        // update the hidden animator and camera so the mission avatar can walk
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

        // dispose the render-to-texture 3D room between missions
        private void CleanupRoom3D()
        {
            roomDoor3D = null;
            roomWindow3D = null;
            roomRadioLightRenderer = null;
            roomRadioLight = null;
            roomDoorMarker3D = null;
            roomBoardMarker3D = null;
            roomRadioMarker3D = null;

            if (roomRenderCamera != null)
            {
                roomRenderCamera.targetTexture = null;
                roomRenderCamera = null;
            }

            if (roomRenderTexture != null)
            {
                roomRenderTexture.Release();
                Destroy(roomRenderTexture);
                roomRenderTexture = null;
            }

            if (roomRenderRoot != null)
            {
                Destroy(roomRenderRoot);
                roomRenderRoot = null;
            }

            for (int i = 0; i < roomRenderMaterials.Count; i += 1)
            {
                if (roomRenderMaterials[i] != null)
                {
                    Destroy(roomRenderMaterials[i]);
                }
            }

            roomRenderMaterials.Clear();

            for (int i = 0; i < roomRenderMeshes.Count; i += 1)
            {
                if (roomRenderMeshes[i] != null)
                {
                    Destroy(roomRenderMeshes[i]);
                }
            }

            roomRenderMeshes.Clear();
        }

        // dispose the hidden runtime avatar render rig between missions
        private void CleanupRuntimeAvatar()
        {
            avatarRenderReady = false;
            liveAvatarImage = null;
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

        // collect visible mesh bounds for render-camera framing
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

        // cameras isolate by layer, so move all avatar renderers under the same layer
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

        // ignore keyboard while a mini-game is open
        private bool KeyboardBlocked()
        {
            if (puzzleGame != null && puzzleGame.IsOpen)
            {
                return true;
            }

            if (codeGame != null && codeGame.IsOpen)
            {
                return true;
            }

            return missileGame != null && missileGame.IsOpen;
        }

        // combine the touch joystick and the keyboard into one left/right value
        private int ReadMoveX()
        {
            int x = uiMoveX;

            if (!KeyboardBlocked())
            {
                if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) x -= 1;
                if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) x += 1;
            }

            return Mathf.Clamp(x, -1, 1);
        }

        // same but for forward/back
        private int ReadMoveY()
        {
            int y = uiMoveY;

            if (!KeyboardBlocked())
            {
                if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) y += 1;
                if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) y -= 1;
            }

            return Mathf.Clamp(y, -1, 1);
        }

        // walk the avatar over to one of the objects
        private void WalkToZone(string zone)
        {
            uiMoveX = 0;
            uiMoveY = 0;
            walkTargetX = StationX(zone);
            // wall tasks are at the back, safe zone is on the floor
            walkTargetDepth = zone == "safe-zone" ? 0.30f : 0.92f;
        }

        // do whatever action is available where the avatar is standing
        private void OnAction()
        {
            OpenGame(currentZone);
            RefreshUI();
        }

        // open the mini-game for a prop ("puzzle" | "code" | "missile"); ignores anything else
        private void OpenGame(string id)
        {
            if (id == "puzzle" && HasTask("puzzle") && !puzzleDone)
            {
                GetPuzzle().Open(result => OnGameFinished("puzzle", result));
            }
            else if (id == "code" && HasTask("code") && !codeDone)
            {
                GetCode().Open(result => OnGameFinished("code", result));
            }
            else if (id == "missile" && HasTask("missile") && !missileDone)
            {
                MissionRoomProfile profile = payload.profile;
                GetMissile().Open(
                    profile != null ? profile.avatar : "",
                    avatarRenderReady ? avatarRenderTexture : null,
                    SpriteFromDataUrl(profile != null ? profile.avatarImage : ""),
                    result => OnGameFinished("missile", result));
            }
        }

        // called by each game when it finishes; stores the result and updates the room
        private void OnGameFinished(string id, MissionGameResult result)
        {
            if (result != null)
            {
                gameResults.Add(result);
            }

            if (id == "puzzle") puzzleDone = true;
            else if (id == "code") codeDone = true;
            else if (id == "missile") missileDone = true;

            HideLauncher(id);
            DisableTapZone(id);

            if (id == "puzzle") ClearRoom3DMarker(ref roomBoardMarker3D);
            else if (id == "code")
            {
                SetDoor3DClosed();
                ClearRoom3DMarker(ref roomDoorMarker3D);
            }
            else if (id == "missile") ClearRoom3DMarker(ref roomRadioMarker3D);

            MissionResultBridge.NotifyStageCompleted(id);
            RefreshUI();
        }

        private void HideLauncher(string id)
        {
            Button button = null;
            if (id == "puzzle") button = puzzleLauncherButton;
            else if (id == "code") button = codeLauncherButton;
            else if (id == "missile") button = missileLauncherButton;

            if (button != null)
            {
                button.gameObject.SetActive(false);
                button.onClick.RemoveAllListeners();
            }
        }

        // turn off the prop tap zone for a finished game so the closed prop is no longer tappable
        private void DisableTapZone(string id)
        {
            Button zone = null;
            if (id == "puzzle") { zone = kitTapZone; kitTapZone = null; }
            else if (id == "code") { zone = doorTapZone; doorTapZone = null; }
            else if (id == "missile") { zone = domeTapZone; domeTapZone = null; }

            if (zone != null)
            {
                zone.onClick.RemoveAllListeners();
                zone.gameObject.SetActive(false);
            }
        }

        private PuzzleMiniGame GetPuzzle()
        {
            if (puzzleGame == null)
            {
                puzzleGame = GetComponent<PuzzleMiniGame>() ?? gameObject.AddComponent<PuzzleMiniGame>();
            }

            return puzzleGame;
        }

        private DoorCodeMiniGame GetCode()
        {
            if (codeGame == null)
            {
                codeGame = GetComponent<DoorCodeMiniGame>() ?? gameObject.AddComponent<DoorCodeMiniGame>();
            }

            return codeGame;
        }

        private MissileMiniGame GetMissile()
        {
            if (missileGame == null)
            {
                missileGame = GetComponent<MissileMiniGame>() ?? gameObject.AddComponent<MissileMiniGame>();
            }

            return missileGame;
        }

        // a row of big launcher buttons across the top, one per selected game. tapping the door
        // prop also opens the code game; these buttons make every game reachable + clearly labelled.
        private void BuildGameLaunchers(RectTransform root)
        {
            var ids = new System.Collections.Generic.List<string>();
            var labels = new System.Collections.Generic.List<string>();

            if (HasTask("puzzle")) { ids.Add("puzzle"); labels.Add(MissionText.Rtl("פאזל משחק")); }
            if (HasTask("code")) { ids.Add("code"); labels.Add(MissionText.Rtl("הדלת משחק")); }
            if (HasTask("missile")) { ids.Add("missile"); labels.Add(MissionText.Rtl("הטילים משחק")); }

            if (ids.Count == 0)
            {
                return;
            }

            float width = 1f / ids.Count;

            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                float x0 = i * width;
                Button button = CreateButton(root, id + " Launcher", labels[i],
                    new Vector2(x0 + 0.01f, 0.915f), new Vector2(x0 + width - 0.01f, 0.99f),
                    () => OpenGame(id));
                button.GetComponent<Image>().color = new Color32(18, 110, 180, 240);
            }
        }

        // pull just the questions out of the exercises
        private string[] ExerciseQuestions()
        {
            if (payload.exercises == null)
            {
                return new string[0];
            }

            var list = new string[payload.exercises.Length];
            for (int i = 0; i < payload.exercises.Length; i += 1)
            {
                list[i] = payload.exercises[i] != null ? payload.exercises[i].question : "";
            }

            return list;
        }

        // pull just the answers out of the exercises
        private string[] ExerciseAnswers()
        {
            if (payload.exercises == null)
            {
                return new string[0];
            }

            var list = new string[payload.exercises.Length];
            for (int i = 0; i < payload.exercises.Length; i += 1)
            {
                list[i] = payload.exercises[i] != null ? payload.exercises[i].answer : "";
            }

            return list;
        }

        private void SetDoor3DClosed()
        {
            if (doorImage != null)
            {
                SetImageSprite(doorImage, LoadNewRoomSprite("closed_door"), true);
            }

            if (roomDoor3D != null)
            {
                roomDoor3D.localRotation = Quaternion.identity;
            }
        }

        private void SetRadio3DOn(bool on)
        {
            Color color = on ? SafeColor : new Color32(58, 74, 78, 255);
            float emission = on ? 2f : 0.12f;

            if (roomRadioLightRenderer != null)
            {
                SetRoom3DMaterialColor(roomRadioLightRenderer.sharedMaterial, color, emission);
            }

            if (roomRadioLight != null)
            {
                roomRadioLight.enabled = on;
                roomRadioLight.color = color;
                roomRadioLight.intensity = on ? 1.6f : 0f;
            }
        }

        private void ClearRoom3DMarker(ref GameObject marker)
        {
            if (marker != null)
            {
                Destroy(marker);
                marker = null;
            }
        }

        // move/scale the avatar + shadow and do the little walking bob
        private void ApplyAvatarPosition(bool moved, float previousX, float previousDepth, float dt)
        {
            // depth controls floor height and scale
            float feetY = Mathf.Lerp(FloorFrontY, FloorBackY, avatarDepth);
            float scale = Mathf.Lerp(FrontScale, BackScale, avatarDepth);

            avatarRect.anchorMin = avatarRect.anchorMax = new Vector2(avatarX, feetY);

            if (moved)
            {
                bobTime += dt * 9f;
                float deltaX = avatarX - previousX;
                float deltaDepth = avatarDepth - previousDepth;

                // turn the avatar to face the way it's heading; pick the dominant axis
                // (normalized by each axis' speed so it tracks the joystick's intent on diagonals)
                bool horizontal = Mathf.Abs(deltaX) / WalkSpeed >= Mathf.Abs(deltaDepth) / DepthSpeed;
                if (horizontal && Mathf.Abs(deltaX) > 0.00001f)
                {
                    // walking left -> turn left, walking right -> turn right
                    faceDir = Mathf.Sign(deltaX);
                    avatarYaw = faceDir < 0f ? -RuntimeAvatarTurnYaw : RuntimeAvatarTurnYaw;
                }
                else if (!horizontal && Mathf.Abs(deltaDepth) > 0.00001f)
                {
                    // toward the front of the room (the camera) -> back to camera; toward the back -> face the camera
                    avatarYaw = deltaDepth < 0f ? 180f : 0f;
                }
            }
            else
            {
                bobTime = 0f;
            }

            float bob = moved ? Mathf.Abs(Mathf.Sin(bobTime)) * 6f : 0f;
            avatarRect.anchoredPosition = new Vector2(0f, bob);
            float horizontalScale = avatarRenderReady ? scale : (faceDir >= 0f ? 1f : -1f) * scale;
            avatarRect.localScale = new Vector3(horizontalScale, scale, 1f);

            if (shadowRect != null)
            {
                // keep the shadow on the floor
                shadowRect.anchorMin = shadowRect.anchorMax = new Vector2(avatarX, feetY - 0.005f);
                float shadowScale = scale * (1f - bob / 60f);
                shadowRect.localScale = new Vector3(shadowScale, shadowScale, 1f);
            }

            UpdateRoomCamera(dt);
            RenderRuntimeAvatar(moved);
        }

        // pan the oversized room world inside its clipped viewport, like a simple camera
        private void UpdateRoomCamera(float dt)
        {
            if (roomViewport == null || roomWorld == null)
            {
                return;
            }

            float viewportWidth = roomViewport.rect.width;
            float worldWidth = roomWorld.rect.width;

            if (viewportWidth <= 1f || worldWidth <= viewportWidth + 1f)
            {
                Vector2 fixedMin = roomWorld.offsetMin;
                Vector2 fixedMax = roomWorld.offsetMax;
                fixedMin.x = 0f;
                fixedMax.x = 0f;
                roomWorld.offsetMin = fixedMin;
                roomWorld.offsetMax = fixedMax;
                cameraInitialized = true;
                return;
            }

            float focusX = Mathf.Clamp01(avatarX) * worldWidth;
            float targetOffset = Mathf.Clamp((viewportWidth * 0.5f) - focusX, viewportWidth - worldWidth, 0f);
            float currentOffset = roomWorld.offsetMin.x;

            if (!cameraInitialized || dt <= 0f)
            {
                currentOffset = targetOffset;
                cameraInitialized = true;
            }
            else
            {
                currentOffset = Mathf.Lerp(currentOffset, targetOffset, Mathf.Clamp01(dt * CameraFollowSpeed));
            }

            Vector2 min = roomWorld.offsetMin;
            Vector2 max = roomWorld.offsetMax;
            min.x = currentOffset;
            max.x = currentOffset;
            roomWorld.offsetMin = min;
            roomWorld.offsetMax = max;
        }

        // tapping the floor now walks the avatar to that spot in the larger room
        private void WalkToRoomPoint(PointerEventData eventData)
        {
            if (eventData == null || roomWorld == null || KeyboardBlocked())
            {
                return;
            }

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(roomWorld, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                return;
            }

            Rect rect = roomWorld.rect;
            float targetX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
            float targetY = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);

            uiMoveX = 0;
            uiMoveY = 0;
            walkTargetX = Mathf.Clamp(targetX, MinX, MaxX);
            walkTargetDepth = Mathf.Clamp01(Mathf.InverseLerp(FloorFrontY, FloorBackY, targetY));
        }

        // recheck which object the avatar is near and refresh the buttons
        private void UpdateZone()
        {
            string zone = NearestZone();

            if (zone == currentZone)
            {
                return;
            }

            currentZone = zone;
            RefreshUI();
        }

        // figure out which object the avatar is closest to (if any)
        private string NearestZone()
        {
            // only count chosen objects near the back wall
            if (avatarDepth < 0.5f)
            {
                return "";
            }

            string best = "";
            float bestDistance = ZoneRadius;

            if (HasTask("code")) best = ConsiderZone(best, ref bestDistance, "code", DoorX);
            if (HasTask("puzzle")) best = ConsiderZone(best, ref bestDistance, "puzzle", KitX);
            if (HasTask("missile")) best = ConsiderZone(best, ref bestDistance, "missile", IronDomeX);

            return best;
        }

        // helper: keep this zone if it's the closest one so far
        private string ConsiderZone(string best, ref float bestDistance, string zone, float zoneX)
        {
            float distance = Mathf.Abs(avatarX - zoneX);

            if (distance <= bestDistance)
            {
                bestDistance = distance;
                return zone;
            }

            return best;
        }

        // the x spot where you stand to use a given object
        private float StationX(string zone)
        {
            if (zone == "code") return DoorX;
            if (zone == "puzzle") return KitX;
            if (zone == "missile") return IronDomeX;
            if (zone == "door") return DoorX;
            if (zone == "safe-zone") return SafeX;
            if (zone == "window") return WindowX;
            if (zone == "board") return BoardX;
            if (zone == "radio") return RadioX;
            return avatarX;
        }

        // update buttons and status text
        private void RefreshUI()
        {
            bool showAction = false;
            string actionLabel = "";

            if (currentZone == "puzzle" && HasTask("puzzle") && !puzzleDone)
            {
                showAction = true;
                actionLabel = MissionText.Rtl("משחק פאזל");
            }
            else if (currentZone == "code" && HasTask("code") && !codeDone)
            {
                showAction = true;
                actionLabel = MissionText.Rtl("הדלת משחק");
            }
            else if (currentZone == "missile" && HasTask("missile") && !missileDone)
            {
                showAction = true;
                actionLabel = MissionText.Rtl("הטילים משחק");
            }

            if (actionButton != null)
            {
                actionButton.gameObject.SetActive(showAction);

                if (showAction && actionButtonLabel != null)
                {
                    actionButtonLabel.text = actionLabel;
                }
            }

            missionComplete = IsAllComplete();

            if (submitButton != null)
            {
                submitButton.interactable = missionComplete && !resultSubmitted;
            }

            if (resultSubmitted)
            {
                UpdateStatus(MissionText.Rtl("כל המשימות הסתיימו!"), true);
            }
            else if (missionComplete)
            {
                UpdateStatus(MissionText.Rtl("כל המשימות הסתיימו. לחצו שליחה."), true);
            }
            else
            {
                UpdateStatus(ProgressHint(), false);
            }
        }

        // are all the chosen tasks done?
        private bool IsAllComplete()
        {
            bool any = false;

            if (HasTask("puzzle")) { any = true; if (!puzzleDone) return false; }
            if (HasTask("code")) { any = true; if (!codeDone) return false; }
            if (HasTask("missile")) { any = true; if (!missileDone) return false; }

            return any;
        }

        // a short hint for where the avatar is, or how many tasks are left
        private string ProgressHint()
        {
            int remaining = 0;
            if (HasTask("puzzle") && !puzzleDone) remaining += 1;
            if (HasTask("code") && !codeDone) remaining += 1;
            if (HasTask("missile") && !missileDone) remaining += 1;

            if (remaining <= 0)
            {
                return MissionText.Rtl("כל המשחקים הסתיימו. לחצו שליחה.");
            }

            if (remaining == 1)
            {
                return MissionText.Rtl("בחרו משחק כדי להתחיל. נותר משחק אחד.");
            }

            return MissionText.Rtl("משחקים " +remaining + " נותרו");
        }

        // send the finished result back to the web page
        private void SubmitMission()
        {
            if (!missionComplete || resultSubmitted)
            {
                return;
            }

            MissionResultBridge.SubmitGames("room", TasksSummary(), gameResults.ToArray());

            resultSubmitted = true;

            if (submitButton != null)
            {
                submitButton.interactable = false;
            }

            UpdateStatus(MissionText.Rtl("כל המשימות הסתיימו!"), true);
        }

        // join the task list into one comma string for the result
        private string TasksSummary()
        {
            return payload.tasks == null ? "" : string.Join(",", payload.tasks);
        }

        // set the status line text + color (green if good, red if not)
        private void UpdateStatus(string message, bool good)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = message;
            statusText.color = good ? SafeColor : DangerColor;
        }

        // make the one reusable canvas the first time we need it
        private RectTransform EnsureCanvas()
        {
            if (rootCanvas == null)
            {
                EnsureDisplayCamera();
                rootCanvas = CreateCanvas(transform);
                canvasRoot = rootCanvas.GetComponent<RectTransform>();
            }

            return canvasRoot;
        }

        private void EnsureDisplayCamera()
        {
            if (displayCamera != null)
            {
                return;
            }

            Camera existingCamera = Camera.main;
            if (existingCamera != null && existingCamera.targetTexture == null)
            {
                return;
            }

            var cameraObject = new GameObject("Mission Room Display Camera", typeof(Camera));
            cameraObject.transform.SetParent(transform, false);
            displayCamera = cameraObject.GetComponent<Camera>();
            displayCamera.clearFlags = CameraClearFlags.SolidColor;
            displayCamera.backgroundColor = BackgroundColor;
            displayCamera.cullingMask = 0;
            displayCamera.depth = -100f;
        }

        private void CleanupDisplayCamera()
        {
            if (displayCamera != null)
            {
                Destroy(displayCamera.gameObject);
                displayCamera = null;
            }
        }

        // clear the canvas children only
        private void ClearCanvas()
        {
            if (canvasRoot == null)
            {
                return;
            }

            CleanupRoom3D();
            CleanupRuntimeAvatar();

            for (int i = canvasRoot.childCount - 1; i >= 0; i -= 1)
            {
                Destroy(canvasRoot.GetChild(i).gameObject);
            }
        }

        // spin up a screen-space canvas that scales with the screen
        private static Canvas CreateCanvas(Transform parent)
        {
            var canvasObject = new GameObject("Mission Room Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.transform.SetParent(parent, false);

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390, 720);
            scaler.matchWidthOrHeight = 0.5f;

            Image background = canvasObject.AddComponent<Image>();
            background.color = BackgroundColor;

            return canvas;
        }

        // make a plain rectangle panel with the given anchors
        private static RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            panel.GetComponent<Image>().color = new Color32(255, 255, 255, 28);
            return rect;
        }

        // make a uGUI text element with these settings
        private static Text CreateText(Transform parent, string value, int size, FontStyle style, TextAnchor alignment, Color color)
        {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.text = string.IsNullOrEmpty(value) ? "" : value;
            text.font = MissionFonts.UiFont;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8, 4);
            rect.offsetMax = new Vector2(-8, -4);
            return text;
        }

        // make a colored button with a text label and click handler
        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, UnityAction onClick)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(4, 4);
            rect.offsetMax = new Vector2(-4, -4);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color32(122, 157, 147, 235);

            Button button = buttonObject.GetComponent<Button>();

            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            if (!string.IsNullOrEmpty(label))
            {
                Text text = CreateText(buttonObject.transform, label, 16, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor);
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 9;
                text.resizeTextMaxSize = 16;
                text.verticalOverflow = VerticalWrapMode.Overflow;
            }

            return button;
        }

        private static Button CreateRoomLauncherButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, UnityAction onClick, Color color)
        {
            Button button = CreateButton(parent, name, label, anchorMin, anchorMax, onClick);
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }

            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.color = Color.white;
                text.fontSize = 14;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 8;
                text.resizeTextMaxSize = 14;
                text.verticalOverflow = VerticalWrapMode.Overflow;

                Outline outline = text.gameObject.GetComponent<Outline>() ?? text.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color32(0, 0, 0, 160);
                outline.effectDistance = new Vector2(1f, -1f);
            }

            return button;
        }

        // make a transparent tap zone that hugs a prop's visible (letterboxed) artwork, so each
        // game activates by tapping the door / kit / iron dome image itself, not the looser box
        // around it. The zone is a child of the prop image and gets inset to the drawn sprite.
        private Button CreatePropTapZone(string name, Image propImage, UnityAction onClick)
        {
            var zoneObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            zoneObject.transform.SetParent(propImage.rectTransform, false);
            RectTransform rect = zoneObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = zoneObject.GetComponent<Image>();
            image.color = new Color32(255, 255, 255, 0); // invisible, but still receives taps
            image.raycastTarget = true;

            // the artwork underneath must not grab taps in its empty letterbox margins
            propImage.raycastTarget = false;

            Button button = zoneObject.GetComponent<Button>();

            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            // fit now, and again next frame once the canvas has a real size
            FitTapZoneToSprite(rect, propImage);
            pendingTapZoneFits.Add(new KeyValuePair<RectTransform, Image>(rect, propImage));
            return button;
        }

        // inset a stretched child so it covers only the drawn sprite of a preserveAspect image
        private static void FitTapZoneToSprite(RectTransform zone, Image propImage)
        {
            if (zone == null || propImage == null || propImage.sprite == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            Rect parentRect = propImage.rectTransform.rect;
            float rectWidth = parentRect.width;
            float rectHeight = parentRect.height;
            if (rectWidth <= 1f || rectHeight <= 1f)
            {
                return; // layout not ready yet; the next-frame pass will fit it
            }

            Rect spriteRect = propImage.sprite.rect;
            if (spriteRect.width <= 0f || spriteRect.height <= 0f)
            {
                return;
            }

            float spriteAspect = spriteRect.width / spriteRect.height;
            float rectAspect = rectWidth / rectHeight;

            float drawnWidth = rectWidth;
            float drawnHeight = rectHeight;
            if (spriteAspect > rectAspect)
            {
                drawnHeight = rectWidth / spriteAspect; // letterboxed top and bottom
            }
            else
            {
                drawnWidth = rectHeight * spriteAspect; // pillarboxed left and right
            }

            float insetX = Mathf.Max(0f, (rectWidth - drawnWidth) * 0.5f);
            float insetY = Mathf.Max(0f, (rectHeight - drawnHeight) * 0.5f);

            zone.anchorMin = Vector2.zero;
            zone.anchorMax = Vector2.one;
            zone.offsetMin = new Vector2(insetX, insetY);
            zone.offsetMax = new Vector2(-insetX, -insetY);
        }

        // make an image that just shows a sprite (no clicking)
        private static Image CreateSpriteImage(Transform parent, string name, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, bool preserveAspect)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = imageObject.GetComponent<Image>();
            image.raycastTarget = false;

            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
                image.preserveAspect = preserveAspect;
            }
            else
            {
                image.color = WallFallbackColor;
            }

            return image;
        }

        // make a raw image for render textures
        private static RawImage CreateRawImage(Transform parent, string name, Texture texture, Vector2 anchorMin, Vector2 anchorMax)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            RawImage image = imageObject.GetComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        // make a tappable sprite (the room objects you can click)
        private static Button CreateSpriteButton(Transform parent, string name, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, UnityAction onClick, bool preserveAspect)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = buttonObject.GetComponent<Image>();

            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
                image.preserveAspect = preserveAspect;
            }
            else
            {
                // tiny fill keeps the empty button clickable
                image.color = new Color32(255, 255, 255, 4);
            }

            Button button = buttonObject.GetComponent<Button>();

            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            return button;
        }

        // make a plain solid-color image
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

        // hook a callback that needs pointer coordinates onto an EventTrigger
        private static void AddPointerTrigger(EventTrigger trigger, EventTriggerType type, UnityAction<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }

        // load a room png from Resources and crop it to its content
        private static Sprite LoadRoomSprite(string id)
        {
            Texture2D texture = Resources.Load<Texture2D>("Room/" + id);

            if (texture == null)
            {
                return null;
            }

            return Sprite.Create(texture, ContentRect(id, texture), new Vector2(0.5f, 0.5f), 100f);
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

        // crop room sprites to the useful part
        private static Rect ContentRect(string id, Texture2D texture)
        {
            int srcW, srcH, x, yTop, cw, ch;
            switch (id)
            {
                case "radio":        srcW = 433; srcH = 319; x = 100; yTop = 137; cw = 73;  ch = 80;  break; // just the radio, not the whole shelf
                case "open_door":    srcW = 585; srcH = 427; x = 90;  yTop = 6;   cw = 310; ch = 398; break;
                case "door":         srcW = 190; srcH = 315; x = 10;  yTop = 6;   cw = 164; ch = 298; break;
                case "open_window":  srcW = 558; srcH = 447; x = 46;  yTop = 46;  cw = 462; ch = 344; break;
                case "close_window": srcW = 390; srcH = 306; x = 54;  yTop = 32;  cw = 284; ch = 250; break;
                case "board":        srcW = 577; srcH = 433; x = 14;  yTop = 14;  cw = 548; ch = 400; break;
                default:
                    return new Rect(0, 0, texture.width, texture.height);
            }

            float sx = (float)texture.width / srcW;
            float sy = (float)texture.height / srcH;

            float rx = x * sx;
            float rw = cw * sx;
            float rh = ch * sy;
            float ry = texture.height - (yTop + ch) * sy; // flip y (top-left -> bottom-left)

            rx = Mathf.Clamp(rx, 0f, texture.width);
            ry = Mathf.Clamp(ry, 0f, texture.height);
            rw = Mathf.Clamp(rw, 1f, texture.width - rx);
            rh = Mathf.Clamp(rh, 1f, texture.height - ry);

            return new Rect(rx, ry, rw, rh);
        }

        // swap an image's sprite (and reset its color/aspect)
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

        // turn a base64 data url into a cropped sprite
        private static Sprite SpriteFromDataUrl(string dataUrl)
        {
            string value = Clean(dataUrl);
            int commaIndex = value.IndexOf(",", StringComparison.Ordinal);

            if (commaIndex < 0)
            {
                return null;
            }

            try
            {
                byte[] bytes = Convert.FromBase64String(value.Substring(commaIndex + 1));
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                if (!texture.LoadImage(bytes))
                {
                    return null;
                }

                // cut out the saved avatar background
                Rect contentRect = KeyOutBackgroundAndTrim(texture);
                return Sprite.Create(texture, contentRect, new Vector2(0.5f, 0.5f), 100f);
            }
            catch
            {
                return null;
            }
        }

        // remove the flat edge background and crop the avatar
        private static Rect KeyOutBackgroundAndTrim(Texture2D texture)
        {
            int width = texture.width;
            int height = texture.height;
            Rect fullRect = new Rect(0, 0, width, height);

            if (width < 4 || height < 4)
            {
                return fullRect;
            }

            Color32[] pixels;

            try
            {
                pixels = texture.GetPixels32();
            }
            catch
            {
                return fullRect; // texture isn't readable
            }

            Color32 c0 = pixels[0];
            Color32 c1 = pixels[width - 1];
            Color32 c2 = pixels[(height - 1) * width];
            Color32 c3 = pixels[height * width - 1];

            bool hasSolidBackground =
                c0.a > 250 && c1.a > 250 && c2.a > 250 && c3.a > 250 &&
                SimilarColor(c0, c1, 40) && SimilarColor(c0, c2, 40) && SimilarColor(c0, c3, 40);

            if (hasSolidBackground)
            {
                int rBg = (c0.r + c1.r + c2.r + c3.r) / 4;
                int gBg = (c0.g + c1.g + c2.g + c3.g) / 4;
                int bBg = (c0.b + c1.b + c2.b + c3.b) / 4;
                // use a tight match so clothes stay visible
                const int threshold = 16;
                int thresholdSq = threshold * threshold;

                var queued = new bool[pixels.Length];
                var stack = new Stack<int>(width * 2);

                for (int x = 0; x < width; x++)
                {
                    EnqueueIfBackground(pixels, queued, stack, x, rBg, gBg, bBg, thresholdSq);
                    EnqueueIfBackground(pixels, queued, stack, (height - 1) * width + x, rBg, gBg, bBg, thresholdSq);
                }

                for (int y = 0; y < height; y++)
                {
                    EnqueueIfBackground(pixels, queued, stack, y * width, rBg, gBg, bBg, thresholdSq);
                    EnqueueIfBackground(pixels, queued, stack, y * width + width - 1, rBg, gBg, bBg, thresholdSq);
                }

                while (stack.Count > 0)
                {
                    int index = stack.Pop();
                    pixels[index].a = 0;

                    int x = index % width;
                    int y = index / width;

                    if (x > 0) EnqueueIfBackground(pixels, queued, stack, index - 1, rBg, gBg, bBg, thresholdSq);
                    if (x < width - 1) EnqueueIfBackground(pixels, queued, stack, index + 1, rBg, gBg, bBg, thresholdSq);
                    if (y > 0) EnqueueIfBackground(pixels, queued, stack, index - width, rBg, gBg, bBg, thresholdSq);
                    if (y < height - 1) EnqueueIfBackground(pixels, queued, stack, index + width, rBg, gBg, bBg, thresholdSq);
                }
            }

            // brighten the visible pixels a bit so the clothes stand out against the room
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a <= 16)
                {
                    continue;
                }

                pixels[i].r = (byte)Mathf.Min(255f, pixels[i].r * 1.15f);
                pixels[i].g = (byte)Mathf.Min(255f, pixels[i].g * 1.15f);
                pixels[i].b = (byte)Mathf.Min(255f, pixels[i].b * 1.15f);
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            // tight box around the visible (non-transparent) pixels
            int minX = width, minY = height, maxX = -1, maxY = -1;

            for (int y = 0; y < height; y++)
            {
                int row = y * width;

                for (int x = 0; x < width; x++)
                {
                    if (pixels[row + x].a > 16)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (maxX < minX || maxY < minY)
            {
                return fullRect; // nothing visible, just use the whole image
            }

            int pad = Mathf.RoundToInt(Mathf.Min(width, height) * 0.04f);
            minX = Mathf.Max(0, minX - pad);
            minY = Mathf.Max(0, minY - pad);
            maxX = Mathf.Min(width - 1, maxX + pad);
            maxY = Mathf.Min(height - 1, maxY + pad);

            return new Rect(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);
        }

        // flood-fill helper: queue this pixel if it's close enough to the bg color
        private static void EnqueueIfBackground(Color32[] pixels, bool[] queued, Stack<int> stack, int index, int rBg, int gBg, int bBg, int thresholdSq)
        {
            if (queued[index])
            {
                return;
            }

            Color32 pixel = pixels[index];
            int dr = pixel.r - rBg;
            int dg = pixel.g - gBg;
            int db = pixel.b - bBg;

            if (dr * dr + dg * dg + db * db <= thresholdSq)
            {
                queued[index] = true;
                stack.Push(index);
            }
        }

        // true if two colors are within tolerance on each channel
        private static bool SimilarColor(Color32 a, Color32 b, int tolerance)
        {
            return Mathf.Abs(a.r - b.r) <= tolerance
                && Mathf.Abs(a.g - b.g) <= tolerance
                && Mathf.Abs(a.b - b.b) <= tolerance;
        }

        // cached soft circle sprite
        private static Sprite softCircleSprite;

        // get the cached soft circle, drawing it the first time
        private static Sprite GetSoftCircleSprite()
        {
            if (softCircleSprite != null)
            {
                return softCircleSprite;
            }

            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size * 0.5f;
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) - radius;
                    float dy = (y + 0.5f) - radius;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                    float alpha = Mathf.Clamp01(1f - distance);
                    alpha = Mathf.SmoothStep(0f, 1f, alpha);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            softCircleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return softCircleSprite;
        }

        // cached wall shade sprite
        private static Sprite verticalShadeSprite;

        // get the cached wall gradient, drawing it the first time
        private static Sprite GetVerticalShadeSprite()
        {
            if (verticalShadeSprite != null)
            {
                return verticalShadeSprite;
            }

            const int height = 128;
            var texture = new Texture2D(2, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[2 * height];

            for (int y = 0; y < height; y++)
            {
                float t = y / (float)(height - 1); // 0 at the bottom, 1 at the top
                byte alpha = (byte)Mathf.Lerp(6f, 78f, t);
                var shade = new Color32(7, 16, 21, alpha);
                pixels[y * 2] = shade;
                pixels[y * 2 + 1] = shade;
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            verticalShadeSprite = Sprite.Create(texture, new Rect(0, 0, 2, height), new Vector2(0.5f, 0.5f), 100f);
            return verticalShadeSprite;
        }

        // make sure there's an EventSystem so the UI gets clicks
        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // trim a string, or give back "" if it's null/empty
        private static string Clean(string value)
        {
            return string.IsNullOrEmpty(value) ? "" : value.Trim();
        }

        // grab the first letter of a username (or "?")
        private static string InitialFor(string username)
        {
            string clean = Clean(username);
            return clean.Length == 0 ? "?" : clean.Substring(0, 1).ToUpperInvariant();
        }
    }

    // draws the side walls as angled room surfaces instead of full rectangular UI images.
    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class RoomSideWallGraphic : MaskableGraphic
    {
        private Texture texture;
        private bool rightSide;
        private float innerFloorY = 0.45f;

        public override Texture mainTexture
        {
            get { return texture != null ? texture : Texture2D.whiteTexture; }
        }

        protected override void OnEnable()
        {
            EnsureCanvasRenderer();
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            EnsureCanvasRenderer();
            base.OnDisable();
        }

        public void Configure(Texture wallTexture, bool isRightSide, float innerFloor, Color tint)
        {
            EnsureCanvasRenderer();
            texture = wallTexture;
            rightSide = isRightSide;
            innerFloorY = Mathf.Clamp01(innerFloor);
            color = tint;
            SetMaterialDirty();
            SetVerticesDirty();
        }

        private void EnsureCanvasRenderer()
        {
            if (GetComponent<CanvasRenderer>() == null)
            {
                gameObject.AddComponent<CanvasRenderer>();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = GetPixelAdjustedRect();
            float innerY = Mathf.Lerp(rect.yMin, rect.yMax, innerFloorY);

            Vector2 innerTop;
            Vector2 outerTop;
            Vector2 outerBottom;
            Vector2 innerBottom;

            if (rightSide)
            {
                innerTop = new Vector2(rect.xMin, rect.yMax);
                outerTop = new Vector2(rect.xMax, rect.yMax);
                outerBottom = new Vector2(rect.xMax, rect.yMin);
                innerBottom = new Vector2(rect.xMin, innerY);
            }
            else
            {
                innerTop = new Vector2(rect.xMax, rect.yMax);
                outerTop = new Vector2(rect.xMin, rect.yMax);
                outerBottom = new Vector2(rect.xMin, rect.yMin);
                innerBottom = new Vector2(rect.xMax, innerY);
            }

            Color32 vertexColor = color;

            // Sample only a narrow vertical slice of the wall texture instead of the
            // full 0..1 width. The side wall is a thin strip, so stretching the whole
            // 2048px texture across it minifies ~17x and the mipmaps collapse it to a
            // flat average colour (the "grey panel"). A narrow slice keeps the texel
            // density close to the back wall so the brick detail stays visible.
            const float USpan = 0.28f;
            float uLow = 0.5f - USpan * 0.5f;
            float uHigh = 0.5f + USpan * 0.5f;
            float innerU = rightSide ? uLow : uHigh;
            float outerU = rightSide ? uHigh : uLow;

            vh.AddVert(innerTop, vertexColor, new Vector2(innerU, 1f));
            vh.AddVert(outerTop, vertexColor, new Vector2(outerU, 1f));
            vh.AddVert(outerBottom, vertexColor, new Vector2(outerU, 0f));
            vh.AddVert(innerBottom, vertexColor, new Vector2(innerU, 0f));
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
    }

    // all the mission data the web page sends us
    [Serializable]
    public class MissionRoomPayload
    {
        public string activityId;
        public string groupId;
        public string mode;
        public string[] tasks;
        public MissionExercise[] exercises;
        public MissionRoomProfile profile = new MissionRoomProfile();
    }

    // one board exercise: a question + its answer
    [Serializable]
    public class MissionExercise
    {
        public string question;
        public string answer;
    }

    // the player's avatar pic + name
    [Serializable]
    public class MissionRoomProfile
    {
        public string avatar;
        public string avatarImage;
        public string username;
    }
}
