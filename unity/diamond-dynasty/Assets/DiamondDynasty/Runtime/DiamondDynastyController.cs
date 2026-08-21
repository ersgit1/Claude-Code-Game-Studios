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
        private Camera presentationCamera;
        private int viewportWidth = -1;
        private int viewportHeight = -1;
        private float resolvedAt;
        private float swingStartedAt = -1f;
        private GUIStyle hudStyle;
        private GUIStyle messageStyle;

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
            if (state.Phase == AtBatPhase.Pitching && rules.PitchProgress(state.Pitch, now) >= 1f)
            {
                if (rules.ResolveTakenPitch(state, now)) resolvedAt = now;
            }
            if (state.Phase == AtBatPhase.Resolved && now - resolvedAt >= config.ResultHoldSeconds)
            {
                rules.ReadyNextPitch(state);
                swingStartedAt = -1f;
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
            batterRenderer = CreateRenderer("Batter", batterSprites[0], ToWorld(80f, 150f, -1f), 20);
            pitcherRenderer = CreateRenderer("Pitcher", pitcherSprites[0], ToWorld(128f, 94f, -1f), 10);
            ballRenderer = CreateRenderer("Ball", CreateSolidSprite(Color.white), ToWorld(128f, 111f, -2f), 30);
            ballRenderer.enabled = false;
            strikeZoneLines = CreateOutline("Strike Zone", new Color(1f, 0.96f, 0.81f, 0.45f), 31);
            aimLines = CreateOutline("Contact Cursor", new Color32(82, 224, 208, 255), 32);
        }

        private void HandleInput()
        {
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;
            if (keyboard != null)
            {
                if (keyboard.enterKey.wasPressedThisFrame) StartPitch();
                if (keyboard.spaceKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) Swing();
                if (keyboard.leftArrowKey.wasPressedThisFrame) rules.MoveAim(state, -config.AimStep, 0f);
                if (keyboard.rightArrowKey.wasPressedThisFrame) rules.MoveAim(state, config.AimStep, 0f);
                if (keyboard.upArrowKey.wasPressedThisFrame) rules.MoveAim(state, 0f, -config.AimStep);
                if (keyboard.downArrowKey.wasPressedThisFrame) rules.MoveAim(state, 0f, config.AimStep);
            }
            if (gamepad == null) return;
            if (gamepad.startButton.wasPressedThisFrame) StartPitch();
            if (gamepad.buttonSouth.wasPressedThisFrame)
            {
                if (state.Phase == AtBatPhase.Pitching) Swing();
                else StartPitch();
            }
            if (gamepad.dpad.left.wasPressedThisFrame) rules.MoveAim(state, -config.AimStep, 0f);
            if (gamepad.dpad.right.wasPressedThisFrame) rules.MoveAim(state, config.AimStep, 0f);
            if (gamepad.dpad.up.wasPressedThisFrame) rules.MoveAim(state, 0f, -config.AimStep);
            if (gamepad.dpad.down.wasPressedThisFrame) rules.MoveAim(state, 0f, config.AimStep);
        }

        private void StartPitch()
        {
            if (state.Phase == AtBatPhase.Resolved) rules.ReadyNextPitch(state);
            swingStartedAt = -1f;
            rules.BeginPitch(state, Time.unscaledTime);
        }

        private void Swing()
        {
            if (state.Phase == AtBatPhase.Ready)
            {
                StartPitch();
                return;
            }
            if (state.Phase != AtBatPhase.Pitching) return;
            swingStartedAt = Time.unscaledTime;
            if (rules.ResolveSwing(state, swingStartedAt)) resolvedAt = swingStartedAt;
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
                ballRenderer.transform.position = ToWorld(sample.X, sample.Y, -2f);
                ballRenderer.transform.localScale = Vector3.one * (sample.Radius * 2f + 1f);
            }
            else ballRenderer.enabled = false;

            PositionOutline(strikeZoneLines, config.StrikeLeft, config.StrikeTop, config.StrikeRight, config.StrikeBottom);
            PositionOutline(aimLines, state.AimX - 6f, state.AimY - 6f, state.AimX + 6f, state.AimY + 6f);
        }

        private void OnGUI()
        {
            var scale = Mathf.Min(Screen.width / (float)config.NativeWidth, Screen.height / (float)config.NativeHeight);
            var offsetX = (Screen.width - config.NativeWidth * scale) * 0.5f;
            var offsetY = (Screen.height - config.NativeHeight * scale) * 0.5f;
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
            if (hudStyle == null)
            {
                hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 7, fontStyle = FontStyle.Bold };
                hudStyle.normal.textColor = new Color32(255, 246, 207, 255);
                messageStyle = new GUIStyle(hudStyle) { alignment = TextAnchor.MiddleCenter };
            }
            GUI.color = new Color32(7, 19, 40, 255);
            GUI.DrawTexture(new Rect(0f, 0f, 256f, 31f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(6f, 3f, 70f, 12f), string.Format("HARBOR  {0:00}", state.Runs), hudStyle);
            GUI.Label(new Rect(92f, 3f, 50f, 12f), "INN " + state.Inning, hudStyle);
            GUI.Label(new Rect(140f, 3f, 42f, 12f), "H " + state.Hits, hudStyle);
            GUI.Label(new Rect(180f, 3f, 60f, 12f), "OUT " + state.Outs, hudStyle);
            GUI.Label(new Rect(6f, 16f, 80f, 12f), string.Format("B {0}  S {1}", state.Balls, state.Strikes), hudStyle);
            GUI.color = new Color(0.03f, 0.08f, 0.16f, 0.88f);
            GUI.DrawTexture(new Rect(18f, 198f, 220f, 20f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(20f, 200f, 216f, 16f), state.Result, messageStyle);
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
