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

        // where the avatar stands at each object (x fraction of the room, left -> right)
        private const float DoorX = 0.15f;
        private const float WindowX = 0.415f;
        private const float BoardX = 0.64f;
        private const float RadioX = 0.865f;
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

        // keep only one controller alive so SendMessage always hits the one on screen
        private static MissionRoomController activeInstance;

        private MissionRoomPayload payload = new MissionRoomPayload();

        // one canvas, reused for both the waiting screen and the mission screen
        private Canvas rootCanvas;
        private RectTransform canvasRoot;

        // build the mission on the next Update
        private bool pendingMissionBuild;
        private string pendingMissionJson = "";

        // stuff that gets rebuilt every mission
        private RectTransform avatarRect;
        private RectTransform shadowRect;
        private Text statusText;
        private Button submitButton;
        private Button actionButton;
        private Text actionButtonLabel;
        private Image windowImage;
        private Image doorImage;
        private Image radioLight;
        private DoorLockMiniGame doorLock;
        private BoardExerciseMiniGame boardGame;
        private WindowCloseMiniGame windowGame;

        // walking. avatarX = left/right (room x-fraction), avatarDepth = forward/back
        // (0 = front near the camera, 1 = back against the wall)
        private float avatarX = 0.5f;
        private float avatarDepth = 0.45f;
        private float walkTargetX = -1f;   // where tap-to-walk is heading (negative = none)
        private float walkTargetDepth;
        private int uiMoveX;               // -1/0/+1 from the on-screen d-pad
        private int uiMoveY;               // -1/0/+1 from the on-screen d-pad
        private float faceDir = 1f;
        private float bobTime;

        // what's been done so far
        private string currentZone = "";
        private bool everReachedZone;
        private bool windowClosed;
        private bool radioOn;
        private bool radioWired;
        private bool doorClosed;
        private bool boardSolved;
        private bool missionComplete;
        private bool resultSubmitted;

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

            BuildWaitingScreen();
        }

        // every frame: build pending mission, then move the avatar around
        private void Update()
        {
            if (pendingMissionBuild)
            {
                pendingMissionBuild = false;
                ApplyPendingMission();
                return;
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
            ApplyAvatarPosition(moved, previousX, dt);
            UpdateZone();
        }

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

            Text title = CreateText(root, "Mission room", 34, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor);
            title.rectTransform.anchorMin = new Vector2(0.08f, 0.50f);
            title.rectTransform.anchorMax = new Vector2(0.92f, 0.60f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            Text waiting = CreateText(root, "Waiting for mission data...", 18, FontStyle.Normal, TextAnchor.MiddleCenter, MutedColor);
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
            RectTransform room = CreatePanel(root, "Room", new Vector2(0.02f, 0.30f), new Vector2(0.98f, 0.99f), Vector2.zero, Vector2.zero);
            room.GetComponent<Image>().color = new Color32(46, 49, 53, 255);

            CreateRoom(room);
            CreateControls(root);
            RefreshUI();
        }

        // reset all the flags/positions back to the start of a fresh mission
        private void ResetMissionState()
        {
            if (doorLock != null)
            {
                doorLock.Close();
            }

            if (boardGame != null)
            {
                boardGame.Close();
            }

            if (windowGame != null)
            {
                windowGame.Close();
            }

            avatarX = 0.5f;
            avatarDepth = 0.45f;
            walkTargetX = -1f;
            walkTargetDepth = 0f;
            uiMoveX = 0;
            uiMoveY = 0;
            faceDir = 1f;
            bobTime = 0f;
            currentZone = "";
            everReachedZone = false;
            windowClosed = false;
            radioOn = false;
            radioWired = false;
            doorClosed = false;
            boardSolved = false;
            missionComplete = false;
            resultSubmitted = false;

            avatarRect = null;
            shadowRect = null;
            statusText = null;
            submitButton = null;
            actionButton = null;
            actionButtonLabel = null;
            windowImage = null;
            doorImage = null;
            radioLight = null;
        }

        // lay out the wall, floor and all four objects + the avatar
        private void CreateRoom(RectTransform parent)
        {
            // make the wall part of the room
            RectTransform wall = CreatePanel(parent, "Wall", new Vector2(0f, WallBottomY), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            Image wallImage = wall.GetComponent<Image>();
            wallImage.color = WallFallbackColor;
            SetImageSprite(wallImage, LoadRoomSprite("back_wall"), false);
            wallImage.raycastTarget = false;

            // soft shade from the top for a bit of depth
            CreateSpriteImage(wall, "Wall Shade", GetVerticalShadeSprite(), Vector2.zero, Vector2.one, false);

            // draw the floor slightly over the wall
            RectTransform floor = CreatePanel(parent, "Floor", new Vector2(0f, 0f), new Vector2(1f, WallBottomY + 0.04f), Vector2.zero, Vector2.zero);
            Image floorImage = floor.GetComponent<Image>();
            floorImage.color = WallFallbackColor;
            SetImageSprite(floorImage, LoadRoomSprite("floor"), false);
            floorImage.raycastTarget = false;

            // put the four task objects on the wall

            // door, on the left, sitting on the wall/floor line
            bool doorTask = HasTask("door");
            Button doorButton = CreateSpriteButton(parent, "Door",
                LoadRoomSprite(doorTask ? "open_door" : "door"),
                new Vector2(0.0f, 0.40f), new Vector2(0.30f, 0.70f),
                doorTask ? (UnityAction)(() => WalkToZone("door")) : null, true);
            doorImage = doorButton.GetComponent<Image>();

            // window, center-left
            bool windowTask = HasTask("window");
            Button windowButton = CreateSpriteButton(parent, "Window",
                LoadRoomSprite(windowTask ? "open_window" : "close_window"),
                new Vector2(0.31f, 0.47f), new Vector2(0.52f, 0.70f),
                windowTask ? (UnityAction)(() => WalkToZone("window")) : null, true);
            windowImage = windowButton.GetComponent<Image>();

            // board, center-right (a bit smaller than the window)
            CreateSpriteButton(parent, "Board", LoadRoomSprite("board"),
                new Vector2(0.55f, 0.50f), new Vector2(0.73f, 0.70f),
                HasTask("board") ? (UnityAction)(() => WalkToZone("board")) : null, true);

            // radio, far right (just the radio unit, cropped out of the shelf image)
            CreateSpriteButton(parent, "Radio", LoadRoomSprite("radio"),
                new Vector2(0.73f, 0.44f), new Vector2(1.0f, 0.70f),
                HasTask("radio") ? (UnityAction)(() => WalkToZone("radio")) : null, true);

            CreateAvatar(parent);
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

        // make the d-pad, action button, submit button and status text
        private void CreateControls(RectTransform root)
        {
            // 4-way d-pad on the left (arrow keys / WASD do the same thing)
            Button up = CreateButton(root, "Move Up", "^", new Vector2(0.105f, 0.135f), new Vector2(0.195f, 0.205f), NoOp);
            AddHold(up.gameObject, 0, 1);

            Button down = CreateButton(root, "Move Down", "v", new Vector2(0.105f, 0.015f), new Vector2(0.195f, 0.085f), NoOp);
            AddHold(down.gameObject, 0, -1);

            Button left = CreateButton(root, "Move Left", "<", new Vector2(0.01f, 0.075f), new Vector2(0.10f, 0.145f), NoOp);
            AddHold(left.gameObject, -1, 0);

            Button right = CreateButton(root, "Move Right", ">", new Vector2(0.20f, 0.075f), new Vector2(0.29f, 0.145f), NoOp);
            AddHold(right.gameObject, 1, 0);

            actionButton = CreateButton(root, "Action", "Action", new Vector2(0.33f, 0.135f), new Vector2(0.65f, 0.225f), OnAction);
            actionButton.GetComponent<Image>().color = new Color32(41, 179, 106, 235);
            actionButtonLabel = actionButton.GetComponentInChildren<Text>();
            actionButton.gameObject.SetActive(false);

            submitButton = CreateButton(root, "Submit", "Submit", new Vector2(0.67f, 0.135f), new Vector2(0.98f, 0.225f), SubmitMission);
            submitButton.interactable = false;

            statusText = CreateText(root, "", 15, FontStyle.Bold, TextAnchor.MiddleCenter, MutedColor);
            statusText.rectTransform.anchorMin = new Vector2(0.04f, 0.255f);
            statusText.rectTransform.anchorMax = new Vector2(0.96f, 0.298f);
            statusText.rectTransform.offsetMin = Vector2.zero;
            statusText.rectTransform.offsetMax = Vector2.zero;
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
            var avatar = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
            avatar.transform.SetParent(parent, false);
            avatarRect = avatar.GetComponent<RectTransform>();
            avatarRect.pivot = new Vector2(0.5f, 0f);
            avatarRect.anchorMin = avatarRect.anchorMax = new Vector2(avatarX, feetY);
            avatarRect.sizeDelta = new Vector2(116, 196);
            avatarRect.anchoredPosition = Vector2.zero;

            Image image = avatar.GetComponent<Image>();
            image.raycastTarget = false;
            Sprite avatarSprite = SpriteFromDataUrl(payload.profile.avatarImage);

            if (avatarSprite != null)
            {
                image.sprite = avatarSprite;
                image.color = Color.white;
                image.preserveAspect = true;
                return;
            }

            // no avatar image? just show a circle with their first letter
            image.sprite = GetSoftCircleSprite();
            image.color = new Color32(122, 157, 147, 255);
            Text initial = CreateText(avatar.transform, InitialFor(payload.profile.username), 40, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            initial.raycastTarget = false;
            initial.rectTransform.anchorMin = Vector2.zero;
            initial.rectTransform.anchorMax = Vector2.one;
            initial.rectTransform.offsetMin = Vector2.zero;
            initial.rectTransform.offsetMax = Vector2.zero;
        }

        // ignore keyboard while a mini-game is open
        private bool KeyboardBlocked()
        {
            if (doorLock != null && doorLock.IsOpen)
            {
                return true;
            }

            if (boardGame != null && boardGame.IsOpen)
            {
                return true;
            }

            return windowGame != null && windowGame.IsOpen;
        }

        // combine the d-pad and the keyboard into one left/right value
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
            if (currentZone == "window" && HasTask("window") && !windowClosed)
            {
                // open the window mini-game, it calls back once it's closed
                GetWindowGame().Open(OnWindowClosed);
            }
            else if (currentZone == "radio" && HasTask("radio") && !radioWired)
            {
                radioOn = true;
                if (radioLight != null)
                {
                    radioLight.color = SafeColor;
                }

                // hand off to the web wire puzzle, it calls CompleteRadioWire when solved
                MissionResultBridge.OpenRadioWire();
            }
            else if (currentZone == "door" && HasTask("door") && !doorClosed)
            {
                // open the door lock mini-game, it calls back once all bolts are locked
                GetDoorLock().Open(OnDoorSecured);
            }
            else if (currentZone == "board" && HasTask("board") && !boardSolved)
            {
                GetBoardGame().Open(ExerciseQuestions(), ExerciseAnswers(), OnBoardSolved);
            }

            RefreshUI();
        }

        // get the door-lock component, making it the first time if needed
        private DoorLockMiniGame GetDoorLock()
        {
            if (doorLock == null)
            {
                doorLock = GetComponent<DoorLockMiniGame>() ?? gameObject.AddComponent<DoorLockMiniGame>();
            }

            return doorLock;
        }

        // get the board component, making it the first time if needed
        private BoardExerciseMiniGame GetBoardGame()
        {
            if (boardGame == null)
            {
                boardGame = GetComponent<BoardExerciseMiniGame>() ?? gameObject.AddComponent<BoardExerciseMiniGame>();
            }

            return boardGame;
        }

        // get the window component, making it the first time if needed
        private WindowCloseMiniGame GetWindowGame()
        {
            if (windowGame == null)
            {
                windowGame = GetComponent<WindowCloseMiniGame>() ?? gameObject.AddComponent<WindowCloseMiniGame>();
            }

            return windowGame;
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

        // called when the door lock game finishes (all bolts locked)
        private void OnDoorSecured()
        {
            if (doorClosed)
            {
                return;
            }

            doorClosed = true;
            SetImageSprite(doorImage, LoadRoomSprite("door"), true); // swap to the closed-door image
            MissionResultBridge.NotifyStageCompleted("door");
            RefreshUI();
        }

        // called when the window game finishes
        private void OnWindowClosed()
        {
            if (windowClosed)
            {
                return;
            }

            windowClosed = true;
            SetImageSprite(windowImage, LoadRoomSprite("close_window"), true);
            MissionResultBridge.NotifyStageCompleted("window");
            RefreshUI();
        }

        // called when all the board exercises are answered
        private void OnBoardSolved()
        {
            if (boardSolved)
            {
                return;
            }

            boardSolved = true;
            MissionResultBridge.NotifyStageCompleted("board");
            RefreshUI();
        }

        // the web page calls this once the radio wire puzzle is solved
        [Preserve]
        public void CompleteRadioWire()
        {
            if (radioWired)
            {
                return;
            }

            radioWired = true;
            radioOn = true;

            if (radioLight != null)
            {
                radioLight.color = SafeColor;
            }

            MissionResultBridge.NotifyStageCompleted("radio");
            RefreshUI();
        }

        // move/scale the avatar + shadow and do the little walking bob
        private void ApplyAvatarPosition(bool moved, float previousX, float dt)
        {
            // depth controls floor height and scale
            float feetY = Mathf.Lerp(FloorFrontY, FloorBackY, avatarDepth);
            float scale = Mathf.Lerp(FrontScale, BackScale, avatarDepth);

            avatarRect.anchorMin = avatarRect.anchorMax = new Vector2(avatarX, feetY);

            if (moved)
            {
                bobTime += dt * 9f;
                float delta = avatarX - previousX;

                if (Mathf.Abs(delta) > 0.00001f)
                {
                    faceDir = Mathf.Sign(delta);
                }
            }
            else
            {
                bobTime = 0f;
            }

            float bob = moved ? Mathf.Abs(Mathf.Sin(bobTime)) * 6f : 0f;
            avatarRect.anchoredPosition = new Vector2(0f, bob);
            avatarRect.localScale = new Vector3((faceDir >= 0f ? 1f : -1f) * scale, scale, 1f);

            if (shadowRect != null)
            {
                // keep the shadow on the floor
                shadowRect.anchorMin = shadowRect.anchorMax = new Vector2(avatarX, feetY - 0.005f);
                float shadowScale = scale * (1f - bob / 60f);
                shadowRect.localScale = new Vector3(shadowScale, shadowScale, 1f);
            }
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

            if (!string.IsNullOrEmpty(zone))
            {
                everReachedZone = true;
            }

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

            if (HasTask("door")) best = ConsiderZone(best, ref bestDistance, "door", DoorX);
            if (HasTask("window")) best = ConsiderZone(best, ref bestDistance, "window", WindowX);
            if (HasTask("board")) best = ConsiderZone(best, ref bestDistance, "board", BoardX);
            if (HasTask("radio")) best = ConsiderZone(best, ref bestDistance, "radio", RadioX);

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

            if (currentZone == "window" && HasTask("window") && !windowClosed)
            {
                showAction = true;
                actionLabel = "Close the window";
            }
            else if (currentZone == "radio" && HasTask("radio") && !radioWired)
            {
                showAction = true;
                actionLabel = "Wire the radio";
            }
            else if (currentZone == "door" && HasTask("door") && !doorClosed)
            {
                showAction = true;
                actionLabel = "Lock the door";
            }
            else if (currentZone == "board" && HasTask("board") && !boardSolved)
            {
                showAction = true;
                actionLabel = "Open the board";
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
                UpdateStatus("All tasks done!", true);
            }
            else if (missionComplete)
            {
                UpdateStatus("All tasks done. Tap Submit.", true);
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

            if (HasTask("window")) { any = true; if (!windowClosed) return false; }
            if (HasTask("radio")) { any = true; if (!radioWired) return false; }
            if (HasTask("door")) { any = true; if (!doorClosed) return false; }
            if (HasTask("board")) { any = true; if (!boardSolved) return false; }

            return any;
        }

        // a short hint for where the avatar is, or how many tasks are left
        private string ProgressHint()
        {
            if (currentZone == "window") return windowClosed ? "Window closed." : "Tap \"Close the window\".";
            if (currentZone == "radio") return radioWired ? "Radio wired." : "Tap \"Wire the radio\".";
            if (currentZone == "door") return doorClosed ? "Shelter door secured." : "Tap \"Lock the door\".";
            if (currentZone == "board") return boardSolved ? "Exercises done." : "Tap \"Open the board\".";

            int remaining = 0;
            if (HasTask("window") && !windowClosed) remaining += 1;
            if (HasTask("radio") && !radioWired) remaining += 1;
            if (HasTask("door") && !doorClosed) remaining += 1;
            if (HasTask("board") && !boardSolved) remaining += 1;

            return remaining > 0
                ? $"Walk to each lit object. {remaining} task(s) left."
                : "All tasks done. Tap Submit.";
        }

        // send the finished result back to the web page
        private void SubmitMission()
        {
            if (!missionComplete || resultSubmitted)
            {
                return;
            }

            MissionResultBridge.Submit("room", TasksSummary(), "completed", true, "", "");

            resultSubmitted = true;

            if (submitButton != null)
            {
                submitButton.interactable = false;
            }

            UpdateStatus("All tasks done!", true);
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
                rootCanvas = CreateCanvas(transform);
                canvasRoot = rootCanvas.GetComponent<RectTransform>();
            }

            return canvasRoot;
        }

        // clear the canvas children only
        private void ClearCanvas()
        {
            if (canvasRoot == null)
            {
                return;
            }

            for (int i = canvasRoot.childCount - 1; i >= 0; i -= 1)
            {
                Destroy(canvasRoot.GetChild(i).gameObject);
            }
        }

        // does nothing; placeholder click handler for the d-pad buttons
        private static void NoOp()
        {
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
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
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
                CreateText(buttonObject.transform, label, 16, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor);
            }

            return button;
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

        // make a button move the avatar the whole time it's held down
        private void AddHold(GameObject target, int dirX, int dirY)
        {
            EventTrigger trigger = target.AddComponent<EventTrigger>();
            AddTrigger(trigger, EventTriggerType.PointerDown, () => { uiMoveX = dirX; uiMoveY = dirY; });
            AddTrigger(trigger, EventTriggerType.PointerUp, () => ClearHold(dirX, dirY));
            AddTrigger(trigger, EventTriggerType.PointerExit, () => ClearHold(dirX, dirY));
        }

        // stop moving when you let go of a d-pad button
        private void ClearHold(int dirX, int dirY)
        {
            if (uiMoveX == dirX)
            {
                uiMoveX = 0;
            }

            if (uiMoveY == dirY)
            {
                uiMoveY = 0;
            }
        }

        // hook a callback onto an EventTrigger for a given event type
        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityAction action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
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
