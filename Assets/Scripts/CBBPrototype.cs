using System.Collections;
using UnityEngine;

public class CBBPrototype : MonoBehaviour
{
    private enum Move { Charge, Beam, Barrier }
    private enum CpuDifficulty { Normal, Strong }
    private enum AppScreen { Splash, Title, Battle }

    private int playerCharge;
    private int cpuCharge;
    private int playerScore;
    private int cpuScore;
    private int roundNumber = 1;
    private int turnNumber = 1;
    private bool suddenDeath;
    private bool matchOver;
    private bool inputLocked;
    private float roundBannerUntil;
    private float matchStartTime;
    private int manualMatchNumber = 1;
    private int totalTurnCount;
    private int playerChargeMoves;
    private int playerBeamMoves;
    private int playerBarrierMoves;
    private int cpuChargeMoves;
    private int cpuBeamMoves;
    private int cpuBarrierMoves;
    private int beamWinCount;
    private int chargeWinCount;
    private int suddenDeathCount;
    private int suddenDeathTurnCount;
    private readonly int[] playerActionHistory = new int[3];
    private readonly int[,] playerActionsByCpuState = new int[2, 3];
    private CpuDifficulty cpuDifficulty = CpuDifficulty.Normal;
    private AppScreen appScreen = AppScreen.Splash;
    private float splashEndsAt;
    private string titleNotice = "対戦モードを選んでください";
    private string message = "行動を選んでください";
    private string lastActions = "あなた －     CPU －";

    private GameObject playerFighter;
    private GameObject cpuFighter;
    private Material playerMaterial;
    private Material cpuMaterial;
    private Material floorMaterial;
    private GUIStyle titleStyle;
    private GUIStyle statusStyle;
    private GUIStyle messageStyle;
    private GUIStyle buttonStyle;
    private GUIStyle debugButtonStyle;
    private GUIStyle smallStyle;
    private Font uiFont;
    private Texture2D panelTexture;
    private Texture2D accentTexture;
    private Texture2D loadingTexture;
    private Texture2D whiteTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsurePrototypeExists()
    {
        if (FindFirstObjectByType<CBBPrototype>() == null)
        {
            new GameObject("CBB Game Manager").AddComponent<CBBPrototype>();
        }
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;
        BuildArena();
    }

    private void Start()
    {
        splashEndsAt = Time.unscaledTime + 2.2f;
        roundBannerUntil = Time.unscaledTime + 1.6f;
        matchStartTime = Time.realtimeSinceStartup;
    }

    private void BuildArena()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
        }

        camera.transform.position = new Vector3(0f, 4.35f, -10.8f);
        camera.transform.LookAt(new Vector3(0f, 0.25f, 2.6f));
        camera.fieldOfView = 40f;
        camera.backgroundColor = new Color(0.025f, 0.04f, 0.09f);

        if (FindFirstObjectByType<Light>() == null)
        {
            GameObject lightObject = new GameObject("Key Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.color = new Color(0.78f, 0.88f, 1f);
            lightObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
        }

        floorMaterial = NewMaterial(new Color(0.035f, 0.08f, 0.13f));
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Arena";
        floor.transform.position = new Vector3(0f, -1.15f, 2.6f);
        floor.transform.localScale = new Vector3(6.5f, 0.25f, 10.5f);
        floor.GetComponent<Renderer>().material = floorMaterial;

        playerMaterial = NewMaterial(new Color(0.05f, 0.68f, 1f));
        cpuMaterial = NewMaterial(new Color(1f, 0.2f, 0.42f));
        playerFighter = CreateFighter("PLAYER", new Vector3(-1.2f, -0.05f, 0.15f), playerMaterial);
        cpuFighter = CreateFighter("CPU", new Vector3(1.05f, 0.05f, 5.05f), cpuMaterial);
        playerFighter.transform.localScale = Vector3.one * 1.08f;
        cpuFighter.transform.localScale = Vector3.one * 0.86f;

        for (int i = -2; i <= 2; i++)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Grid Marker";
            marker.transform.position = new Vector3(i * 1.2f, -0.98f, 2.6f);
            marker.transform.localScale = new Vector3(0.035f, 0.04f, 9.5f);
            marker.GetComponent<Renderer>().material = NewMaterial(new Color(0.08f, 0.35f, 0.5f));
        }

        for (int i = 0; i <= 7; i++)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Grid Marker";
            marker.transform.position = new Vector3(0f, -0.98f, -1.6f + i * 1.2f);
            marker.transform.localScale = new Vector3(6.1f, 0.04f, 0.035f);
            marker.GetComponent<Renderer>().material = NewMaterial(new Color(0.08f, 0.35f, 0.5f));
        }
    }

    private GameObject CreateFighter(string fighterName, Vector3 position, Material material)
    {
        GameObject root = new GameObject(fighterName);
        root.transform.position = position;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        body.transform.localScale = new Vector3(1.25f, 1.15f, 1.25f);
        body.GetComponent<Renderer>().material = material;

        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.transform.SetParent(root.transform, false);
        core.transform.localPosition = new Vector3(0f, 0.3f, -0.7f);
        core.transform.localScale = Vector3.one * 0.42f;
        core.GetComponent<Renderer>().material = NewMaterial(Color.white);
        return root;
    }

    private Material NewMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    private void OnGUI()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;
        CreateStyles();
        float scale = Mathf.Max(0.01f, Mathf.Min(Screen.width / 1080f, Screen.height / 1920f));
        float offsetX = (Screen.width - 1080f * scale) * 0.5f;
        float offsetY = (Screen.height - 1920f * scale) * 0.5f;
        GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
        const float width = 1080f;
        const float height = 1920f;

        if (appScreen == AppScreen.Splash)
        {
            if (Time.unscaledTime >= splashEndsAt) appScreen = AppScreen.Title;
            else
            {
                DrawSplashScreen(width, height);
                return;
            }
        }

        if (appScreen == AppScreen.Title)
        {
            DrawTitleScreen(width, height);
            return;
        }

        GUI.Label(new Rect(0f, 45f, width, 90f), "C · B · B", titleStyle);
        GUI.Label(new Rect(0f, 135f, width, 55f), "チャージ・ビーム・バリア", smallStyle);
        if (GUI.Button(new Rect(55f, 62f, 190f, 58f), "タイトルへ", debugButtonStyle))
        {
            appScreen = AppScreen.Title;
            titleNotice = "対戦モードを選んでください";
            return;
        }

        DrawPanel(new Rect(55f, 205f, width - 110f, 235f));
        GUI.Label(new Rect(95f, 230f, 360f, 54f), "あなた", statusStyle);
        GUI.Label(new Rect(width - 455f, 230f, 360f, 54f), "CPU", statusStyle);
        GUI.Label(new Rect(95f, 292f, 360f, 72f), $"ポイント  {playerScore} / 7", statusStyle);
        GUI.Label(new Rect(width - 455f, 292f, 360f, 72f), $"ポイント  {cpuScore} / 7", statusStyle);
        GUI.Label(new Rect(95f, 360f, 360f, 54f), $"チャージ  {playerCharge}", smallStyle);
        GUI.Label(new Rect(width - 455f, 360f, 360f, 54f), $"チャージ  {cpuCharge}", smallStyle);

        GUI.Label(new Rect(80f, 448f, 250f, 58f), "CPU難易度", smallStyle);
        bool canChangeDifficulty = totalTurnCount == 0 && !inputLocked;
        GUI.enabled = canChangeDifficulty;
        if (GUI.Button(new Rect(330f, 448f, 290f, 58f), cpuDifficulty == CpuDifficulty.Normal ? "● 普通" : "○ 普通", debugButtonStyle))
            SetCpuDifficulty(CpuDifficulty.Normal);
        if (GUI.Button(new Rect(640f, 448f, 290f, 58f), cpuDifficulty == CpuDifficulty.Strong ? "● 強い" : "○ 強い", debugButtonStyle))
            SetCpuDifficulty(CpuDifficulty.Strong);
        GUI.enabled = true;

        if (suddenDeath)
        {
            GUI.DrawTexture(new Rect(55f, 525f, width - 110f, 70f), accentTexture);
            GUI.Label(new Rect(55f, 532f, width - 110f, 55f), "サドンデス", statusStyle);
        }

        DrawPanel(new Rect(55f, height - 615f, width - 110f, 510f));
        GUI.Label(new Rect(90f, height - 585f, width - 180f, 54f), $"ラウンド {roundNumber}   ・   ターン {turnNumber}", smallStyle);
        GUI.Label(new Rect(90f, height - 525f, width - 180f, 68f), lastActions, statusStyle);
        GUI.Label(new Rect(90f, height - 448f, width - 180f, 90f), message, messageStyle);

        if (!matchOver)
        {
            GUI.enabled = !inputLocked;
            if (GUI.Button(new Rect(90f, height - 270f, 280f, 145f), "チャージ\n＋1", buttonStyle)) PlayTurn(Move.Charge);

            GUI.enabled = !inputLocked && playerCharge > 0;
            if (GUI.Button(new Rect(width / 2f - 140f, height - 270f, 280f, 145f), "ビーム\n－1", buttonStyle)) PlayTurn(Move.Beam);

            GUI.enabled = !inputLocked;
            if (GUI.Button(new Rect(width - 370f, height - 270f, 280f, 145f), "バリア\n防御", buttonStyle)) PlayTurn(Move.Barrier);
        }
        else
        {
            if (GUI.Button(new Rect(width / 2f - 230f, height - 270f, 460f, 145f), "もう一度対戦", buttonStyle)) ResetMatch();
        }
        GUI.enabled = true;

        if (GUI.Button(new Rect(300f, 1835f, 480f, 58f), "強いCPUを20試合テスト", debugButtonStyle))
        {
            message = CBBPlaytest.RunAutomatedMatches(20);
        }
        GUI.Label(new Rect(80f, 1892f, 920f, 24f), "テストログ：PlaytestLogs / CBB_playtest_log.csv", smallStyle);

        if (!matchOver && Time.unscaledTime < roundBannerUntil)
        {
            Rect bannerRect = new Rect(120f, height / 2f - 120f, width - 240f, 240f);
            GUI.DrawTexture(bannerRect, accentTexture);
            GUI.Label(bannerRect, $"ラウンド {roundNumber}", titleStyle);
        }
    }

    private void DrawSplashScreen(float width, float height)
    {
        GUI.DrawTexture(new Rect(0f, 0f, width, height), panelTexture);
        GUI.Label(new Rect(0f, 610f, width, 120f), "C · B · B", titleStyle);
        GUI.Label(new Rect(0f, 745f, width, 60f), "CHARGE · BEAM · BARRIER", smallStyle);

        DrawPanel(new Rect(260f, 925f, 560f, 210f));
        GUI.Label(new Rect(280f, 955f, 520f, 62f), "MADE WITH", smallStyle);
        GUI.Label(new Rect(280f, 1018f, 520f, 82f), "UNITY", statusStyle);

        float progress = Mathf.Clamp01(1f - (splashEndsAt - Time.unscaledTime) / 2.2f);
        GUI.DrawTexture(new Rect(220f, 1275f, 640f, 18f), accentTexture);
        GUI.DrawTexture(new Rect(220f, 1275f, 640f * progress, 18f), loadingTexture);
        GUI.Label(new Rect(0f, 1320f, width, 50f), "NOW LOADING...", smallStyle);
    }

    private void DrawTitleScreen(float width, float height)
    {
        GUI.DrawTexture(new Rect(0f, 0f, width, height), panelTexture);

        // 仮キャラクターを色とシルエットで表現。正式デザイン時に画像へ差し替える。
        GUI.color = new Color(0.05f, 0.68f, 1f, 0.78f);
        GUI.DrawTexture(new Rect(70f, 350f, 310f, 520f), whiteTexture);
        GUI.color = new Color(1f, 0.2f, 0.42f, 0.78f);
        GUI.DrawTexture(new Rect(700f, 350f, 310f, 520f), whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(0f, 95f, width, 125f), "C · B · B", titleStyle);
        GUI.Label(new Rect(0f, 220f, width, 60f), "読み合い × チャージバトル", smallStyle);
        GUI.Label(new Rect(80f, 500f, 290f, 90f), "PLAYER", statusStyle);
        GUI.Label(new Rect(710f, 500f, 290f, 90f), "RIVAL", statusStyle);
        GUI.Label(new Rect(0f, 755f, width, 70f), "CHARGE  /  BEAM  /  BARRIER", smallStyle);

        DrawPanel(new Rect(105f, 915f, width - 210f, 660f));
        GUI.Label(new Rect(145f, 955f, width - 290f, 70f), "バトルモード", statusStyle);
        GUI.Label(new Rect(145f, 1030f, width - 290f, 55f), titleNotice, smallStyle);

        if (GUI.Button(new Rect(175f, 1120f, width - 350f, 145f), "CPU戦\nひとりで遊ぶ", buttonStyle))
        {
            ResetMatch();
            appScreen = AppScreen.Battle;
            return;
        }

        if (GUI.Button(new Rect(175f, 1300f, width - 350f, 145f), "ランクマッチ\nオンライン対戦", buttonStyle))
        {
            titleNotice = "ランクマッチは現在準備中です";
        }

        GUI.Label(new Rect(0f, 1645f, width, 55f), "開発中プロトタイプ", smallStyle);
        GUI.Label(new Rect(0f, 1710f, width, 45f), "© C・B・B PROJECT", smallStyle);
    }

    private void DrawPanel(Rect rect)
    {
        GUI.DrawTexture(rect, panelTexture);
    }

    private void CreateStyles()
    {
        if (titleStyle != null) return;
        uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic UI Semibold", "Meiryo UI", "Meiryo", "Noto Sans CJK JP", "Arial" }, 36);
        panelTexture = MakeTexture(new Color(0.015f, 0.025f, 0.055f, 0.92f));
        accentTexture = MakeTexture(new Color(0.88f, 0.1f, 0.18f, 0.92f));
        loadingTexture = MakeTexture(new Color(0.2f, 0.82f, 1f, 1f));
        whiteTexture = MakeTexture(Color.white);
        titleStyle = NewStyle(70, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        statusStyle = NewStyle(38, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        messageStyle = NewStyle(34, FontStyle.Bold, new Color(0.8f, 0.91f, 1f), TextAnchor.MiddleCenter);
        messageStyle.wordWrap = true;
        smallStyle = NewStyle(29, FontStyle.Bold, new Color(0.3f, 0.82f, 1f), TextAnchor.MiddleCenter);
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            font = uiFont,
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        debugButtonStyle = new GUIStyle(GUI.skin.button)
        {
            font = uiFont,
            fontSize = 25,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
    }

    private GUIStyle NewStyle(int size, FontStyle style, Color color, TextAnchor alignment)
    {
        return new GUIStyle(GUI.skin.label)
        {
            font = uiFont,
            fontSize = size,
            fontStyle = style,
            normal = { textColor = color },
            alignment = alignment
        };
    }

    private Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void PlayTurn(Move playerMove)
    {
        if (matchOver || inputLocked || (playerMove == Move.Beam && playerCharge <= 0)) return;
        int cpuStateBeforeTurn = cpuCharge > 0 ? 1 : 0;
        Move cpuMove = ChooseCpuMove();
        playerActionHistory[(int)playerMove]++;
        playerActionsByCpuState[cpuStateBeforeTurn, (int)playerMove]++;
        lastActions = $"あなた：{MoveName(playerMove)}     CPU：{MoveName(cpuMove)}";
        totalTurnCount++;
        CountManualMove(playerMove, true);
        CountManualMove(cpuMove, false);
        if (suddenDeath) suddenDeathTurnCount++;

        if (playerMove == Move.Beam) playerCharge--;
        if (cpuMove == Move.Beam) cpuCharge--;

        StartCoroutine(AnimateActions(playerMove, cpuMove));

        if (playerMove == Move.Beam && cpuMove == Move.Charge)
        {
            EndRound(true, suddenDeath ? playerCharge : 2, suddenDeath ? "サドンデスでビーム勝利！" : "CPUのチャージにビームが命中！");
            return;
        }
        if (cpuMove == Move.Beam && playerMove == Move.Charge)
        {
            EndRound(false, suddenDeath ? cpuCharge : 2, suddenDeath ? "CPUがサドンデスに勝利" : "チャージ中にCPUのビームが命中");
            return;
        }

        if (playerMove == Move.Charge) playerCharge++;
        if (cpuMove == Move.Charge) cpuCharge++;

        if (!suddenDeath)
        {
            bool playerAtFive = playerCharge >= 5;
            bool cpuAtFive = cpuCharge >= 5;
            if (playerAtFive && cpuAtFive)
            {
                suddenDeath = true;
                suddenDeathCount++;
                message = "両者5チャージ！ サドンデス開始！";
                turnNumber++;
                return;
            }
            if (playerAtFive)
            {
                EndRound(true, 5, "5チャージ達成！");
                return;
            }
            if (cpuAtFive)
            {
                EndRound(false, 5, "CPUが5チャージ達成");
                return;
            }
        }

        if (playerMove == Move.Beam && cpuMove == Move.Beam) message = "ビーム同士が相殺！ 両者1チャージ消費";
        else if (playerMove == Move.Beam && cpuMove == Move.Barrier) message = "CPUのバリアにビームを防がれた";
        else if (playerMove == Move.Barrier && cpuMove == Move.Beam) message = "バリアでCPUのビームを防いだ！";
        else if (playerMove == Move.Barrier && cpuMove == Move.Barrier) message = "両者バリア。何も起こらなかった";
        else if (playerMove == Move.Charge && cpuMove == Move.Charge) message = "両者が1チャージ獲得";
        else if (playerMove == Move.Charge) message = "1チャージ獲得";
        else if (cpuMove == Move.Charge) message = "CPUが1チャージ獲得";
        turnNumber++;
    }

    private void CountManualMove(Move move, bool player)
    {
        if (player)
        {
            if (move == Move.Charge) playerChargeMoves++;
            else if (move == Move.Beam) playerBeamMoves++;
            else playerBarrierMoves++;
        }
        else
        {
            if (move == Move.Charge) cpuChargeMoves++;
            else if (move == Move.Beam) cpuBeamMoves++;
            else cpuBarrierMoves++;
        }
    }

    private Move ChooseCpuMove()
    {
        return cpuDifficulty == CpuDifficulty.Normal ? ChooseNormalCpuMove() : ChooseStrongCpuMove();
    }

    private Move ChooseNormalCpuMove()
    {
        if (cpuCharge <= 0) return Random.value < 0.72f ? Move.Charge : Move.Barrier;

        float beamChance = 0.28f;
        float barrierChance = 0.25f;
        if (playerCharge >= 4) beamChance += 0.22f;
        if (suddenDeath) beamChance += Mathf.Clamp((cpuCharge - 5) * 0.035f, 0f, 0.22f);
        if (playerCharge <= 0) beamChance *= 0.25f;
        if (cpuCharge >= 4 && !suddenDeath) barrierChance += 0.08f;

        float roll = Random.value;
        if (roll < beamChance) return Move.Beam;
        if (roll < beamChance + barrierChance) return Move.Barrier;
        return Move.Charge;
    }

    private Move ChooseStrongCpuMove()
    {
        if (cpuCharge <= 0)
        {
            int emptyTotal = playerActionsByCpuState[0, 0] + playerActionsByCpuState[0, 1] + playerActionsByCpuState[0, 2];
            float predictedBeam = emptyTotal > 0
                ? (playerActionsByCpuState[0, (int)Move.Beam] + 1f) / (emptyTotal + 3f)
                : (playerCharge > 0 ? 0.22f : 0f);
            float barrierChance = playerCharge > 0 ? Mathf.Lerp(0.18f, 0.7f, predictedBeam) : 0.08f;
            return Random.value < 1f - barrierChance ? Move.Charge : Move.Barrier;
        }

        int historyTotal = playerActionHistory[0] + playerActionHistory[1] + playerActionHistory[2];
        int readyTotal = playerActionsByCpuState[1, 0] + playerActionsByCpuState[1, 1] + playerActionsByCpuState[1, 2];
        float chargeProbability;
        float beamProbability;
        float barrierProbability;

        if (readyTotal >= 2)
        {
            // 「CPUに撃てるチャージがある時」の行動だけを学習する。
            // これにより、チャージ0ではチャージ、1以上ではバリアという条件付き戦術を見抜く。
            chargeProbability = (playerActionsByCpuState[1, (int)Move.Charge] + 1.2f) / (readyTotal + 3.6f);
            beamProbability = (playerActionsByCpuState[1, (int)Move.Beam] + 1.2f) / (readyTotal + 3.6f);
            barrierProbability = (playerActionsByCpuState[1, (int)Move.Barrier] + 1.2f) / (readyTotal + 3.6f);
        }
        else if (historyTotal >= 4)
        {
            chargeProbability = (playerActionHistory[(int)Move.Charge] + 2f) / (historyTotal + 6f);
            beamProbability = (playerActionHistory[(int)Move.Beam] + 2f) / (historyTotal + 6f);
            barrierProbability = (playerActionHistory[(int)Move.Barrier] + 2f) / (historyTotal + 6f);
        }
        else
        {
            chargeProbability = playerCharge <= 0 ? 0.48f : 0.38f;
            beamProbability = playerCharge <= 0 ? 0f : 0.25f;
            barrierProbability = 1f - chargeProbability - beamProbability;
            if (playerCharge >= 4)
            {
                chargeProbability = 0.32f;
                beamProbability = 0.43f;
                barrierProbability = 0.25f;
            }
        }

        float roundValue = suddenDeath ? Mathf.Max(1f, cpuCharge - 1f) : 2f;
        float chargeReward = !suddenDeath && cpuCharge == 4 ? 5f : 1.05f + cpuCharge * 0.18f;
        float chargeUtility = (chargeProbability + barrierProbability) * chargeReward
            - beamProbability * (roundValue + 1.5f);
        float beamUtility = chargeProbability * (roundValue + 2.2f)
            - beamProbability * 0.55f - barrierProbability * 1.25f;
        float barrierUtility = beamProbability * 1.9f
            - chargeProbability * 0.65f - barrierProbability * 0.18f;

        // 相手の主目的を推定し、対抗する勝ち筋へ切り替える。
        if (barrierProbability > chargeProbability + 0.1f && barrierProbability > beamProbability)
            chargeUtility += cpuCharge == 4 && !suddenDeath ? 2.8f : 1.5f; // バリア待ちには5チャージ
        if (chargeProbability > barrierProbability && chargeProbability > beamProbability)
            beamUtility += 1.25f; // 5チャージ狙いにはビーム
        if (beamProbability > chargeProbability && beamProbability > barrierProbability)
        {
            barrierUtility += 1.35f; // ビーム狙いには守る
            chargeUtility += 0.35f;  // 守るだけでなく5チャージも進める
        }
        if (playerCharge <= 0)
        {
            chargeUtility += 0.9f;
            beamUtility -= 1.4f;
        }
        if (suddenDeath && cpuCharge >= 8) beamUtility += 1f;
        if (playerCharge >= 4 && !suddenDeath) beamUtility += 0.55f;

        float chargeWeight = Mathf.Exp(chargeUtility / 0.9f) * 0.9f + 0.1f;
        float beamWeight = Mathf.Exp(beamUtility / 0.9f) * 0.9f + 0.1f;
        float barrierWeight = Mathf.Exp(barrierUtility / 0.9f) * 0.9f + 0.1f;
        float totalWeight = chargeWeight + beamWeight + barrierWeight;
        float roll = Random.value * totalWeight;

        if (roll < chargeWeight) return Move.Charge;
        if (roll < chargeWeight + beamWeight) return Move.Beam;
        return Move.Barrier;
    }

    private void EndRound(bool playerWon, int points, string reason)
    {
        points = Mathf.Max(0, points);
        bool wonByCharge = !suddenDeath && points == 5 && (playerCharge >= 5 || cpuCharge >= 5);
        if (wonByCharge) chargeWinCount++; else beamWinCount++;
        if (playerWon) playerScore += points;
        else cpuScore += points;

        message = $"{reason}  ＋{points}ポイント";
        matchOver = playerScore >= 7 || cpuScore >= 7;
        if (matchOver)
        {
            string winner = playerScore >= 7 ? "プレイヤー" : "CPU";
            message = playerScore >= 7 ? "あなたの勝利！" : "CPUの勝利";
            CBBPlaytest.LogMatch(
                $"手動プレイ（{DifficultyName()}）", manualMatchNumber, winner, Time.realtimeSinceStartup - matchStartTime,
                roundNumber, totalTurnCount, playerScore, cpuScore,
                playerChargeMoves, playerBeamMoves, playerBarrierMoves,
                cpuChargeMoves, cpuBeamMoves, cpuBarrierMoves,
                beamWinCount, chargeWinCount, suddenDeathCount, suddenDeathTurnCount);
            return;
        }

        inputLocked = true;
        StartCoroutine(BeginNextRound());
    }

    private IEnumerator BeginNextRound()
    {
        yield return new WaitForSecondsRealtime(1.25f);

        playerCharge = 0;
        cpuCharge = 0;
        suddenDeath = false;
        roundNumber++;
        turnNumber = 1;
        lastActions = "あなた －     CPU －";
        message = $"ラウンド{roundNumber}開始";
        roundBannerUntil = Time.unscaledTime + 1.6f;

        yield return new WaitForSecondsRealtime(1.6f);
        message = "行動を選んでください";
        inputLocked = false;
    }

    private void ResetMatch()
    {
        playerCharge = 0;
        cpuCharge = 0;
        playerScore = 0;
        cpuScore = 0;
        roundNumber = 1;
        turnNumber = 1;
        suddenDeath = false;
        matchOver = false;
        inputLocked = false;
        roundBannerUntil = Time.unscaledTime + 1.6f;
        manualMatchNumber++;
        matchStartTime = Time.realtimeSinceStartup;
        totalTurnCount = 0;
        playerChargeMoves = playerBeamMoves = playerBarrierMoves = 0;
        cpuChargeMoves = cpuBeamMoves = cpuBarrierMoves = 0;
        beamWinCount = chargeWinCount = suddenDeathCount = suddenDeathTurnCount = 0;
        playerActionHistory[0] = playerActionHistory[1] = playerActionHistory[2] = 0;
        for (int state = 0; state < 2; state++)
            for (int action = 0; action < 3; action++)
                playerActionsByCpuState[state, action] = 0;
        message = "行動を選んでください";
        lastActions = "あなた －     CPU －";
    }

    private string MoveName(Move move)
    {
        return move == Move.Charge ? "チャージ" : move == Move.Beam ? "ビーム" : "バリア";
    }

    private string DifficultyName()
    {
        return cpuDifficulty == CpuDifficulty.Normal ? "普通" : "強い";
    }

    private void SetCpuDifficulty(CpuDifficulty difficulty)
    {
        cpuDifficulty = difficulty;
        playerActionHistory[0] = playerActionHistory[1] = playerActionHistory[2] = 0;
        for (int state = 0; state < 2; state++)
            for (int action = 0; action < 3; action++)
                playerActionsByCpuState[state, action] = 0;
        message = $"CPU難易度を「{DifficultyName()}」にしました";
    }

    private IEnumerator AnimateActions(Move playerMove, Move cpuMove)
    {
        StartCoroutine(AnimateMove(playerFighter, playerMove, new Color(0.05f, 0.75f, 1f)));
        StartCoroutine(AnimateMove(cpuFighter, cpuMove, new Color(1f, 0.18f, 0.48f)));

        if (playerMove == Move.Beam || cpuMove == Move.Beam)
        {
            GameObject beam = new GameObject("Beam Flash");
            LineRenderer line = beam.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.startWidth = 0.22f;
            line.endWidth = 0.22f;
            line.material = NewMaterial(Color.white);
            line.startColor = playerMove == Move.Beam ? Color.cyan : Color.red;
            line.endColor = line.startColor;
            bool playerIsShooting = playerMove == Move.Beam;
            bool beamIsBlocked = playerIsShooting ? cpuMove == Move.Barrier : playerMove == Move.Barrier;
            GameObject shooter = playerIsShooting ? playerFighter : cpuFighter;
            GameObject target = playerIsShooting ? cpuFighter : playerFighter;
            Vector3 beamStart = shooter.transform.position + Vector3.up * 0.25f;
            Vector3 beamEnd = beamIsBlocked
                ? GetShieldPosition(target) + Vector3.up * 0.15f
                : target.transform.position + Vector3.up * 0.25f;
            line.SetPosition(0, beamStart);
            line.SetPosition(1, beamEnd);
            Destroy(beam, 0.38f);

            if (beamIsBlocked) CreateShieldImpact(beamEnd, line.startColor);
        }

        yield return new WaitForSeconds(0.58f);
    }

    private IEnumerator AnimateMove(GameObject fighter, Move move, Color actionColor)
    {
        Transform fighterTransform = fighter.transform;
        Vector3 baseScale = fighterTransform.localScale;
        Vector3 basePosition = fighterTransform.position;
        GameObject effect = null;

        if (move == Move.Charge)
        {
            effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            effect.name = "Charge Energy";
            Destroy(effect.GetComponent<Collider>());
            effect.transform.position = basePosition + Vector3.up * 0.4f;
            effect.transform.localScale = Vector3.one * 0.12f;
            effect.GetComponent<Renderer>().material = NewEffectMaterial(new Color(actionColor.r, actionColor.g, actionColor.b, 0.85f));

            float duration = 0.85f;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(progress * Mathf.PI);
                fighterTransform.localScale = new Vector3(baseScale.x * (1f + pulse * 0.14f), baseScale.y * (1f - pulse * 0.12f), baseScale.z * (1f + pulse * 0.14f));
                effect.transform.position = basePosition + Vector3.up * Mathf.Lerp(-0.35f, 1.45f, progress);
                effect.transform.localScale = Vector3.one * Mathf.Lerp(0.12f, 0.72f, pulse);
                yield return null;
            }
        }
        else if (move == Move.Barrier)
        {
            effect = CreateSilverShield(fighter);
            effect.transform.localScale = Vector3.one * 0.08f;

            float duration = 0.85f;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / duration);
                float shieldStrength = progress < 0.2f
                    ? Mathf.SmoothStep(0f, 1f, progress / 0.2f)
                    : (progress < 0.72f ? 1f : Mathf.SmoothStep(1f, 0f, (progress - 0.72f) / 0.28f));
                float shieldSize = Mathf.Lerp(0.08f, 1f, shieldStrength);
                effect.transform.localScale = Vector3.one * shieldSize;
                effect.transform.localRotation = Quaternion.Euler(0f, Mathf.Sin(progress * Mathf.PI * 2f) * 6f, 0f);
                fighterTransform.localScale = baseScale * (1f + Mathf.Sin(progress * Mathf.PI) * 0.06f);
                yield return null;
            }
        }
        else
        {
            GameObject opponent = fighter == playerFighter ? cpuFighter : playerFighter;
            Vector3 direction = (opponent.transform.position - basePosition).normalized;
            float duration = 0.32f;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / duration);
                float kick = Mathf.Sin(progress * Mathf.PI);
                fighterTransform.position = basePosition + direction * kick * 0.42f;
                fighterTransform.localScale = baseScale * (1f + kick * 0.1f);
                yield return null;
            }
        }

        fighterTransform.position = basePosition;
        fighterTransform.localScale = baseScale;
        if (effect != null) Destroy(effect);
    }

    private Vector3 GetShieldPosition(GameObject fighter)
    {
        GameObject opponent = fighter == playerFighter ? cpuFighter : playerFighter;
        Vector3 towardOpponent = (opponent.transform.position - fighter.transform.position).normalized;
        return fighter.transform.position + towardOpponent * 1.05f;
    }

    private GameObject CreateSilverShield(GameObject fighter)
    {
        GameObject root = new GameObject("Silver Shield");
        root.transform.position = GetShieldPosition(fighter) + Vector3.up * 0.12f;
        GameObject opponent = fighter == playerFighter ? cpuFighter : playerFighter;
        root.transform.rotation = Quaternion.LookRotation((opponent.transform.position - root.transform.position).normalized, Vector3.up);

        Material silver = NewMetalMaterial(new Color(0.72f, 0.78f, 0.86f));
        Material darkSilver = NewMetalMaterial(new Color(0.25f, 0.3f, 0.38f));

        GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        face.name = "Shield Face";
        face.transform.SetParent(root.transform, false);
        face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        face.transform.localScale = new Vector3(0.92f, 0.12f, 1.05f);
        face.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        Destroy(face.GetComponent<Collider>());
        face.GetComponent<Renderer>().material = silver;

        GameObject point = GameObject.CreatePrimitive(PrimitiveType.Cube);
        point.name = "Shield Point";
        point.transform.SetParent(root.transform, false);
        point.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        point.transform.localScale = new Vector3(0.72f, 0.72f, 0.16f);
        point.transform.localPosition = new Vector3(0f, -0.58f, 0f);
        Destroy(point.GetComponent<Collider>());
        point.GetComponent<Renderer>().material = silver;

        GameObject boss = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        boss.name = "Shield Boss";
        boss.transform.SetParent(root.transform, false);
        boss.transform.localPosition = new Vector3(0f, 0.15f, -0.18f);
        boss.transform.localScale = new Vector3(0.34f, 0.34f, 0.16f);
        Destroy(boss.GetComponent<Collider>());
        boss.GetComponent<Renderer>().material = darkSilver;

        GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stripe.name = "Shield Stripe";
        stripe.transform.SetParent(root.transform, false);
        stripe.transform.localPosition = new Vector3(0f, 0.15f, -0.16f);
        stripe.transform.localScale = new Vector3(0.13f, 1.45f, 0.1f);
        Destroy(stripe.GetComponent<Collider>());
        stripe.GetComponent<Renderer>().material = darkSilver;
        return root;
    }

    private void CreateShieldImpact(Vector3 position, Color beamColor)
    {
        GameObject impact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        impact.name = "Shield Impact";
        impact.transform.position = position;
        impact.transform.localScale = Vector3.one * 0.42f;
        Destroy(impact.GetComponent<Collider>());
        impact.GetComponent<Renderer>().material = NewEffectMaterial(new Color(beamColor.r, beamColor.g, beamColor.b, 0.95f));
        Destroy(impact, 0.3f);
    }

    private Material NewMetalMaterial(Color color)
    {
        Material material = NewMaterial(color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.9f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.82f);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.82f);
        return material;
    }

    private Material NewEffectMaterial(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader);
        material.color = color;
        return material;
    }
}
