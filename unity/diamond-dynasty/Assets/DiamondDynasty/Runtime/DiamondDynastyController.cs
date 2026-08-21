using UnityEngine;
using UnityEngine.InputSystem;

namespace DiamondDynasty
{
    /// <summary>Unity lifecycle, input, and pixel presentation for the at-bat slice.</summary>
    public sealed class DiamondDynastyController : MonoBehaviour
    {
        [SerializeField] private AtBatConfig config;

        private AtBatRules rules;
        private AtBatState state;
        private SpriteRenderer batterRenderer;
        private SpriteRenderer pitcherRenderer;
        private SpriteRenderer ballRenderer;
        private SpriteRenderer[] strikeZoneLines;
        private SpriteRenderer[] aimLines;
        private Sprite[] batterSprites;
        private Sprite[] pitcherSprites;
        private Sprite[] ballSprites;
        private Camera presentationCamera;
        private int viewportWidth = -1;
        private int viewportHeight = -1;
        private float resolvedAt;
        private float readyAt;
        private float swingStartedAt = -1f;
        private PlayerRole menuSelection = PlayerRole.Batter;
        private GUIStyle hudStyle;
        private GUIStyle messageStyle;
        private GUIStyle menuTitleStyle;
        private GUIStyle menuButtonStyle;

        /// <summary>Initializes the rules engine and native-pixel scene.</summary>
        public void Awake()
        {
            if (config == null) config = Resources.Load<AtBatConfig>("DiamondDynasty/AtBatConfig");
            if (config == null) config = AtBatConfig.CreatePrototypeDefaults();
            rules = new AtBatRules(config, new UnityAtBatRandom());
            state = rules.CreateState();
            ConfigureApplication();
            BuildPresentation();
        }

        /// <summary>Processes input and advances deterministic gameplay.</summary>
        public void Update()
        {
            ConfigureViewport();
            HandleInput();
            var now = Time.unscaledTime;
            if (state.Role == PlayerRole.Batter && state.Phase == AtBatPhase.Ready && now - readyAt >= config.BatterAutoPitchDelaySeconds)
            {
                rules.BeginCpuPitch(state, now);
            }
            if (state.Role == PlayerRole.Pitcher && state.Phase == AtBatPhase.Charging)
            {
                rules.UpdatePitchCharge(state, now);
            }
            if (state.Role == PlayerRole.Pitcher && state.Phase == AtBatPhase.Pitching)
            {
                if (rules.TryResolveCpuBatter(state, now))
                {
                    swingStartedAt = now;
                    resolvedAt = now;
                }
            }
            if (state.Phase == AtBatPhase.Pitching && rules.PitchProgress(state.Pitch, now) >= 1f)
            {
                if (rules.ResolveTakenPitch(state, now)) resolvedAt = now;
            }
            if (state.Phase == AtBatPhase.Resolved && now - resolvedAt >= config.ResultHoldSeconds)
            {
                rules.ReadyNextPitch(state);
                swingStartedAt = -1f;
                readyAt = now;
            }
            RenderState(now);
        }

        private void ConfigureApplication()
        {
            Application.targetFrameRate = 60;
            QualitySettings.antiAliasing = 0;
            Screen.SetResolution(config.NativeWidth * 3, config.NativeHeight * 3, false);
            presentationCamera = Camera.main;
            if (presentationCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                presentationCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }
            presentationCamera.orthographic = true;
            presentationCamera.orthographicSize = config.NativeHeight * 0.5f;
            presentationCamera.transform.position = new Vector3(0f, 0f, -10f);
            presentationCamera.clearFlags = CameraClearFlags.SolidColor;
            presentationCamera.backgroundColor = new Color32(7, 19, 40, 255);
            ConfigureViewport();
        }

        private void ConfigureViewport()
        {
            if (presentationCamera == null || Screen.width <= 0 || Screen.height <= 0) return;
            if (viewportWidth == Screen.width && viewportHeight == Screen.height) return;
            viewportWidth = Screen.width;
            viewportHeight = Screen.height;

            var targetAspect = (float)config.NativeWidth / config.NativeHeight;
            var windowAspect = (float)viewportWidth / viewportHeight;
            if (windowAspect > targetAspect)
            {
                var normalizedWidth = targetAspect / windowAspect;
                presentationCamera.rect = new Rect((1f - normalizedWidth) * 0.5f, 0f, normalizedWidth, 1f);
            }
            else
            {
                var normalizedHeight = windowAspect / targetAspect;
                presentationCamera.rect = new Rect(0f, (1f - normalizedHeight) * 0.5f, 1f, normalizedHeight);
            }
        }

        private void BuildPresentation()
        {
            var stadium = Resources.Load<Sprite>("DiamondDynasty/Art/stadium");
            CreateRenderer("Stadium", stadium, ToWorld(128f, 112f, 0f), 0);
            batterSprites = LoadSpriteFrames("batter", 9);
            pitcherSprites = LoadSpriteFrames("pitcher", 7);
            ballSprites = CreateBallSprites();
            batterRenderer = CreateRenderer("Batter", batterSprites[0], ToWorld(80f, 150f, -1f), 20);
            pitcherRenderer = CreateRenderer("Pitcher", pitcherSprites[0], ToWorld(128f, 94f, -1f), 10);
            ballRenderer = CreateRenderer("Ball", ballSprites[0], ToWorld(128f, 111f, -2f), 30);
            ballRenderer.enabled = false;
            strikeZoneLines = CreateOutline("Strike Zone", new Color(1f, 0.96f, 0.81f, 0.45f), 31);
            aimLines = CreateOutline("Contact Cursor", Color.white, 32);
        }

        private void HandleInput()
        {
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            if (state.Phase == AtBatPhase.RoleSelection)
            {
                HandleRoleSelectionInput(keyboard, gamepad);
                return;
            }

            if (WasBackPressed(keyboard, gamepad))
            {
                if (state.Phase == AtBatPhase.Charging)
                {
                    rules.CancelPitchCharge(state);
                }
                else if (state.Phase != AtBatPhase.Pitching)
                {
                    ReturnToRoleSelection();
                }
                return;
            }

            if (state.Role == PlayerRole.Batter) HandleBatterInput(keyboard, gamepad);
            else if (state.Role == PlayerRole.Pitcher) HandlePitcherInput(keyboard, gamepad);
        }

        private void HandleRoleSelectionInput(Keyboard keyboard, Gamepad gamepad)
        {
            var moveLeft = keyboard != null && (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame);
            var moveRight = keyboard != null && (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame);
            if (gamepad != null)
            {
                moveLeft |= gamepad.dpad.left.wasPressedThisFrame;
                moveRight |= gamepad.dpad.right.wasPressedThisFrame;
            }
            if (moveLeft) menuSelection = PlayerRole.Batter;
            if (moveRight) menuSelection = PlayerRole.Pitcher;

            var confirm = keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
            if (gamepad != null) confirm |= gamepad.buttonSouth.wasPressedThisFrame;
            if (!confirm || !rules.SelectRole(state, menuSelection)) return;
            readyAt = Time.unscaledTime;
            swingStartedAt = -1f;
        }

        private void HandleBatterInput(Keyboard keyboard, Gamepad gamepad)
        {
            if (state.Phase != AtBatPhase.Ready && state.Phase != AtBatPhase.Pitching) return;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame) rules.MoveAim(state, -config.AimStep, 0f);
                if (keyboard.rightArrowKey.wasPressedThisFrame) rules.MoveAim(state, config.AimStep, 0f);
                if (keyboard.upArrowKey.wasPressedThisFrame) rules.MoveAim(state, 0f, -config.AimStep);
                if (keyboard.downArrowKey.wasPressedThisFrame) rules.MoveAim(state, 0f, config.AimStep);
                if (state.Phase == AtBatPhase.Pitching && (keyboard.spaceKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)) Swing();
            }
            if (gamepad == null) return;
            if (gamepad.dpad.left.wasPressedThisFrame) rules.MoveAim(state, -config.AimStep, 0f);
            if (gamepad.dpad.right.wasPressedThisFrame) rules.MoveAim(state, config.AimStep, 0f);
            if (gamepad.dpad.up.wasPressedThisFrame) rules.MoveAim(state, 0f, -config.AimStep);
            if (gamepad.dpad.down.wasPressedThisFrame) rules.MoveAim(state, 0f, config.AimStep);
            if (state.Phase == AtBatPhase.Pitching && gamepad.buttonSouth.wasPressedThisFrame) Swing();
        }

        private void HandlePitcherInput(Keyboard keyboard, Gamepad gamepad)
        {
            if (state.Phase == AtBatPhase.PitchSetup)
            {
                if (keyboard != null)
                {
                    if (keyboard.qKey.wasPressedThisFrame) rules.CyclePitch(state, -1);
                    if (keyboard.eKey.wasPressedThisFrame) rules.CyclePitch(state, 1);
                    if (keyboard.digit1Key.wasPressedThisFrame) rules.SelectPitch(state, 0);
                    if (keyboard.digit2Key.wasPressedThisFrame) rules.SelectPitch(state, 1);
                    if (keyboard.digit3Key.wasPressedThisFrame) rules.SelectPitch(state, 2);
                    if (keyboard.leftArrowKey.wasPressedThisFrame) rules.MovePitchTarget(state, -config.PitchTargetStep, 0f);
                    if (keyboard.rightArrowKey.wasPressedThisFrame) rules.MovePitchTarget(state, config.PitchTargetStep, 0f);
                    if (keyboard.upArrowKey.wasPressedThisFrame) rules.MovePitchTarget(state, 0f, -config.PitchTargetStep);
                    if (keyboard.downArrowKey.wasPressedThisFrame) rules.MovePitchTarget(state, 0f, config.PitchTargetStep);
                }
                if (gamepad != null)
                {
                    if (gamepad.leftShoulder.wasPressedThisFrame) rules.CyclePitch(state, -1);
                    if (gamepad.rightShoulder.wasPressedThisFrame) rules.CyclePitch(state, 1);
                    if (gamepad.dpad.left.wasPressedThisFrame) rules.MovePitchTarget(state, -config.PitchTargetStep, 0f);
                    if (gamepad.dpad.right.wasPressedThisFrame) rules.MovePitchTarget(state, config.PitchTargetStep, 0f);
                    if (gamepad.dpad.up.wasPressedThisFrame) rules.MovePitchTarget(state, 0f, -config.PitchTargetStep);
                    if (gamepad.dpad.down.wasPressedThisFrame) rules.MovePitchTarget(state, 0f, config.PitchTargetStep);
                }
                if (WasPitchButtonPressed(keyboard, gamepad)) rules.BeginPitchCharge(state, Time.unscaledTime);
                return;
            }

            if (state.Phase != AtBatPhase.Charging) return;
            if (WasPitchButtonReleased(keyboard, gamepad) && !IsPitchButtonHeld(keyboard, gamepad))
            {
                rules.ReleasePitch(state, Time.unscaledTime);
            }
        }

        private void Swing()
        {
            if (state.Role != PlayerRole.Batter || state.Phase != AtBatPhase.Pitching) return;
            swingStartedAt = Time.unscaledTime;
            if (rules.ResolveSwing(state, swingStartedAt)) resolvedAt = swingStartedAt;
        }

        private static bool WasPitchButtonPressed(Keyboard keyboard, Gamepad gamepad)
        {
            var pressed = keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
            if (gamepad != null) pressed |= gamepad.buttonSouth.wasPressedThisFrame;
            return pressed;
        }

        private static bool WasPitchButtonReleased(Keyboard keyboard, Gamepad gamepad)
        {
            var released = keyboard != null && (keyboard.enterKey.wasReleasedThisFrame || keyboard.spaceKey.wasReleasedThisFrame);
            if (gamepad != null) released |= gamepad.buttonSouth.wasReleasedThisFrame;
            return released;
        }

        private static bool IsPitchButtonHeld(Keyboard keyboard, Gamepad gamepad)
        {
            var held = keyboard != null && (keyboard.enterKey.isPressed || keyboard.spaceKey.isPressed);
            if (gamepad != null) held |= gamepad.buttonSouth.isPressed;
            return held;
        }

        private static bool WasBackPressed(Keyboard keyboard, Gamepad gamepad)
        {
            var pressed = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
            if (gamepad != null) pressed |= gamepad.buttonEast.wasPressedThisFrame;
            return pressed;
        }

        private void ReturnToRoleSelection()
        {
            state = rules.CreateState();
            menuSelection = PlayerRole.Batter;
            readyAt = 0f;
            swingStartedAt = -1f;
            ballRenderer.enabled = false;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) rules.CancelPitchCharge(state);
        }

        private void OnDisable()
        {
            if (rules != null && state != null) rules.CancelPitchCharge(state);
        }

        private void RenderState(float now)
        {
            var pitcherFrame = state.Phase == AtBatPhase.Pitching
                ? Mathf.Min(pitcherSprites.Length - 1, Mathf.FloorToInt(rules.PitchProgress(state.Pitch, now) * pitcherSprites.Length))
                : 0;
            pitcherRenderer.sprite = pitcherSprites[pitcherFrame];

            var batterFrame = 0;
            if (swingStartedAt >= 0f)
            {
                batterFrame = Mathf.Min(batterSprites.Length - 1, Mathf.FloorToInt((now - swingStartedAt) / config.BatterFrameSeconds));
            }
            batterRenderer.sprite = batterSprites[batterFrame];

            if (state.Phase == AtBatPhase.Pitching)
            {
                var sample = rules.SamplePitch(state.Pitch, now);
                ballRenderer.enabled = true;
                ballRenderer.sprite = ballSprites[Mathf.Clamp(sample.Radius - 1, 0, ballSprites.Length - 1)];
                ballRenderer.transform.position = ToWorld(Mathf.Round(sample.X), Mathf.Round(sample.Y), -2f);
                ballRenderer.transform.localScale = Vector3.one;
            }
            else ballRenderer.enabled = false;

            SetOutlineEnabled(strikeZoneLines, state.Phase != AtBatPhase.RoleSelection);
            if (state.Phase != AtBatPhase.RoleSelection)
            {
                PositionOutline(strikeZoneLines, config.StrikeLeft, config.StrikeTop, config.StrikeRight, config.StrikeBottom);
            }

            var showAim = state.ShowBatterCursor || (state.ShowPitcherDetails &&
                (state.Phase == AtBatPhase.PitchSetup || state.Phase == AtBatPhase.Charging || state.Phase == AtBatPhase.Pitching));
            SetOutlineEnabled(aimLines, showAim);
            if (!showAim) return;

            var aimX = state.Role == PlayerRole.Pitcher ? state.PitchTargetX : state.AimX;
            var aimY = state.Role == PlayerRole.Pitcher ? state.PitchTargetY : state.AimY;
            var aimColor = state.Role == PlayerRole.Pitcher ? new Color32(255, 199, 74, 255) : new Color32(82, 224, 208, 255);
            SetOutlineColor(aimLines, aimColor);
            PositionOutline(aimLines, aimX - 6f, aimY - 6f, aimX + 6f, aimY + 6f);
        }

        private void OnGUI()
        {
            var scale = Mathf.Min(Screen.width / (float)config.NativeWidth, Screen.height / (float)config.NativeHeight);
            var offsetX = (Screen.width - config.NativeWidth * scale) * 0.5f;
            var offsetY = (Screen.height - config.NativeHeight * scale) * 0.5f;
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
            EnsureGuiStyles();

            if (state.Phase == AtBatPhase.RoleSelection)
            {
                DrawRoleSelection();
                return;
            }

            GUI.color = new Color32(7, 19, 40, 255);
            GUI.DrawTexture(new Rect(0f, 0f, 256f, 31f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(6f, 3f, 70f, 12f), string.Format("HARBOR  {0:00}", state.Runs), hudStyle);
            GUI.Label(new Rect(92f, 3f, 50f, 12f), "INN " + state.Inning, hudStyle);
            GUI.Label(new Rect(140f, 3f, 42f, 12f), "H " + state.Hits, hudStyle);
            GUI.Label(new Rect(180f, 3f, 60f, 12f), "OUT " + state.Outs, hudStyle);
            GUI.Label(new Rect(6f, 16f, 80f, 12f), string.Format("B {0}  S {1}", state.Balls, state.Strikes), hudStyle);

            if (state.ShowPitcherDetails) DrawPitcherControls();

            GUI.color = new Color(0.03f, 0.08f, 0.16f, 0.88f);
            GUI.DrawTexture(new Rect(18f, 198f, 220f, 20f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(20f, 200f, 216f, 16f), state.Result, messageStyle);
        }

        private void EnsureGuiStyles()
        {
            if (hudStyle != null) return;
            hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 7, fontStyle = FontStyle.Bold };
            hudStyle.normal.textColor = new Color32(255, 246, 207, 255);
            messageStyle = new GUIStyle(hudStyle) { alignment = TextAnchor.MiddleCenter };
            menuTitleStyle = new GUIStyle(messageStyle) { fontSize = 13 };
            menuButtonStyle = new GUIStyle(messageStyle) { fontSize = 10 };
        }

        private void DrawRoleSelection()
        {
            GUI.color = new Color(0.02f, 0.06f, 0.13f, 0.94f);
            GUI.DrawTexture(new Rect(24f, 42f, 208f, 142f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(36f, 54f, 184f, 24f), "CHOOSE YOUR SIDE", menuTitleStyle);
            DrawRoleOption(new Rect(38f, 96f, 82f, 42f), PlayerRole.Batter, "BATTER", "READ & SWING");
            DrawRoleOption(new Rect(136f, 96f, 82f, 42f), PlayerRole.Pitcher, "PITCHER", "AIM & POWER");
            GUI.Label(new Rect(34f, 151f, 188f, 18f), "MOVE: ←→ / D-PAD   ENTER/SPACE/SOUTH", messageStyle);
        }

        private void DrawRoleOption(Rect rect, PlayerRole role, string title, string subtitle)
        {
            var selected = menuSelection == role;
            GUI.color = selected ? new Color32(235, 171, 52, 255) : new Color32(32, 66, 91, 255);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = selected ? new Color32(7, 19, 40, 255) : Color.white;
            GUI.Label(new Rect(rect.x, rect.y + 6f, rect.width, 14f), selected ? "> " + title + " <" : title, menuButtonStyle);
            GUI.Label(new Rect(rect.x, rect.y + 23f, rect.width, 10f), subtitle, messageStyle);
            GUI.color = Color.white;
        }

        private void DrawPitcherControls()
        {
            GUI.color = new Color(0.03f, 0.08f, 0.16f, 0.88f);
            GUI.DrawTexture(new Rect(5f, 34f, 246f, 18f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            for (var index = 0; index < config.Pitches.Length; index += 1)
            {
                var prefix = index == state.SelectedPitchIndex ? ">" : " ";
                GUI.Label(new Rect(8f + index * 80f, 37f, 78f, 12f), string.Format("{0}{1} {2}", prefix, index + 1, config.Pitches[index].Name), hudStyle);
            }

            var showReleasedPitch = state.Pitch != null && (state.Phase == AtBatPhase.Pitching || state.Phase == AtBatPhase.Resolved);
            var power = state.Phase == AtBatPhase.Charging ? state.Power01 : showReleasedPitch ? state.Pitch.Power01 : 0f;
            var risk = showReleasedPitch
                ? state.Pitch.MissRadius
                : Mathf.Lerp(config.MinPitchMissRadius, config.MaxPitchMissRadius, Mathf.Pow(power, config.PitchAccuracyExponent));
            GUI.color = new Color(0.03f, 0.08f, 0.16f, 0.88f);
            GUI.DrawTexture(new Rect(18f, 181f, 220f, 14f), Texture2D.whiteTexture);
            GUI.color = new Color32(32, 66, 91, 255);
            GUI.DrawTexture(new Rect(58f, 185f, 70f, 6f), Texture2D.whiteTexture);
            GUI.color = new Color32(235, 171, 52, 255);
            GUI.DrawTexture(new Rect(58f, 185f, 70f * power, 6f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(22f, 181f, 46f, 12f), "POWER", hudStyle);
            GUI.Label(new Rect(132f, 181f, 103f, 12f), string.Format("{0:000}%  LOC ±{1:0}PX", Mathf.RoundToInt(power * 100f), risk), hudStyle);
        }

        private Sprite[] LoadSpriteFrames(string prefix, int count)
        {
            var sprites = new Sprite[count];
            for (var index = 0; index < count; index += 1)
            {
                sprites[index] = Resources.Load<Sprite>(string.Format("DiamondDynasty/Art/{0}-{1}", prefix, index));
            }
            return sprites;
        }

        private SpriteRenderer CreateRenderer(string objectName, Sprite sprite, Vector3 position, int order)
        {
            var objectInstance = new GameObject(objectName);
            objectInstance.transform.SetParent(transform, false);
            objectInstance.transform.position = position;
            var renderer = objectInstance.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private SpriteRenderer[] CreateOutline(string objectName, Color color, int order)
        {
            var sprite = CreateSolidSprite(color);
            var lines = new SpriteRenderer[4];
            for (var index = 0; index < lines.Length; index += 1)
            {
                lines[index] = CreateRenderer(objectName + " " + index, sprite, Vector3.zero, order);
            }
            return lines;
        }

        private void PositionOutline(SpriteRenderer[] lines, float left, float top, float right, float bottom)
        {
            var width = right - left;
            var height = bottom - top;
            SetLine(lines[0], (left + right) * 0.5f, top, width, 1f);
            SetLine(lines[1], (left + right) * 0.5f, bottom, width, 1f);
            SetLine(lines[2], left, (top + bottom) * 0.5f, 1f, height);
            SetLine(lines[3], right, (top + bottom) * 0.5f, 1f, height);
        }

        private void SetLine(SpriteRenderer line, float x, float y, float width, float height)
        {
            line.transform.position = ToWorld(x, y, -3f);
            line.transform.localScale = new Vector3(width, height, 1f);
        }

        private static void SetOutlineEnabled(SpriteRenderer[] lines, bool enabled)
        {
            for (var index = 0; index < lines.Length; index += 1) lines[index].enabled = enabled;
        }

        private static void SetOutlineColor(SpriteRenderer[] lines, Color color)
        {
            for (var index = 0; index < lines.Length; index += 1) lines[index].color = color;
        }

        private static Sprite[] CreateBallSprites()
        {
            var diameters = new[] { 3, 5, 7, 9 };
            var sprites = new Sprite[diameters.Length];
            for (var index = 0; index < diameters.Length; index += 1)
            {
                var texture = CreateBallTexture(diameters[index]);
                sprites[index] = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 1f);
            }
            return sprites;
        }

        /// <summary>Creates a point-filtered circular baseball texture for native-pixel rendering.</summary>
        public static Texture2D CreateBallTexture(int diameter)
        {
            diameter = Mathf.Max(3, diameter | 1);
            var texture = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var center = (diameter - 1) * 0.5f;
            var radius = center + 0.25f;
            var white = new Color32(255, 253, 238, 255);
            var shadow = new Color32(205, 221, 224, 255);
            var seam = new Color32(190, 51, 48, 255);

            for (var y = 0; y < diameter; y += 1)
            {
                for (var x = 0; x < diameter; x += 1)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    Color32 color = distance <= radius
                        ? y < center && distance > radius - 1.25f ? shadow : white
                        : new Color32(0, 0, 0, 0);
                    texture.SetPixel(x, y, color);
                }
            }

            if (diameter >= 7)
            {
                texture.SetPixel(Mathf.FloorToInt(center) - 1, Mathf.CeilToInt(center) + 1, seam);
                texture.SetPixel(Mathf.CeilToInt(center) + 1, Mathf.FloorToInt(center) - 1, seam);
            }
            texture.Apply(false, false);
            return texture;
        }

        private static Sprite CreateSolidSprite(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        private Vector3 ToWorld(float x, float y, float z)
        {
            return new Vector3(x - config.NativeWidth * 0.5f, config.NativeHeight * 0.5f - y, z);
        }
    }
}
