using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class WormBalanceLabWindow : EditorWindow
{
    private const float MinResultViewHeight = 220f;
    private const float MaxResultViewHeight = 520f;
    private const float EstimatedControlsHeight = 560f;
    private const string RewardDatabasePath = "Assets/_Project/App/Gameplay/Rewards/RewardDatabase_Main.asset";
    private const string HpConfigPath = "Assets/_Project/App/Gameplay/Enemy/Worm/Balance/WormHpScalingConfig_Default.asset";
    private const string PressureConfigPath = "Assets/_Project/App/Gameplay/Enemy/Worm/Balance/WormPressureConfig_Default.asset";
    private const string MainWeaponConfigPath = "Assets/_Project/App/Gameplay/Combat/Weapons/ProjectileWeapon/Configs/MainWeaponConfig_Default.asset";
    private const string AcaciaThornConfigPath = "Assets/_Project/App/Gameplay/Combat/Weapons/AcaciaThornWeapon/Configs/AcaciaThornWeaponConfig_Default.asset";

    [SerializeField] private RewardDatabase _rewardDatabase;
    [SerializeField] private WormHpScalingConfig _hpConfig;
    [SerializeField] private WormPressureConfig _pressureConfig;
    [SerializeField] private WeaponConfig _mainWeaponConfig;
    [SerializeField] private AcaciaThornWeaponConfig _acaciaThornConfig;
    [SerializeField] private RailPath _railPath;

    [SerializeField] private int _simulationCount = 1000;
    [SerializeField] private int _seed = 12345;
    [SerializeField] private int _levelNumber = 1;
    [SerializeField] private int _totalLength = 60;
    [SerializeField] private float _pathTimeLimitSeconds = 75f;
    [SerializeField] private bool _derivePathTimeFromRail = true;
    [SerializeField] private float _wormSpeed = 3f;
    [SerializeField] private float _segmentSpacing = 0.5f;
    [SerializeField] private float _hitEfficiency = 0.9f;
    [SerializeField] private int _progressBucketCount = 5;
    [SerializeField] private bool _useRuntimePressure = true;
    [SerializeField] private bool _applySectionRollback = true;
    [SerializeField] private bool _writeCsvLog = true;
    [SerializeField] private bool _logEachRunToConsole;
    [SerializeField] private WormBalanceRewardPickStrategy _rewardPickStrategy = WormBalanceRewardPickStrategy.HighestEstimatedDpsGain;

    private Vector2 _windowScroll;
    private Vector2 _resultScroll;
    private string _lastSummary = "Run a simulation to see balance data.";

    [MenuItem("Tools/Game/Worm Balance Lab")]
    public static void Open()
    {
        GetWindow<WormBalanceLabWindow>("Worm Balance Lab");
    }

    private void OnEnable()
    {
        LoadDefaultAssets();
    }

    private void OnGUI()
    {
        _windowScroll = EditorGUILayout.BeginScrollView(
            _windowScroll,
            false,
            true);

        try
        {
            EditorGUILayout.LabelField("Worm Balance Lab", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Runs deterministic editor-only simulations using the real reward database, reward effects and HP resolver. Locked weapon unlocks are picked first. HighestEstimatedDpsGain then previews every offered reward on cloned runtime states and picks the largest estimated DPS increase.",
                MessageType.Info);

            DrawAssetFields();
            EditorGUILayout.Space(8f);
            DrawSimulationFields();
            EditorGUILayout.Space(8f);
            DrawActions();
            EditorGUILayout.Space(8f);
            DrawSummary();
        }
        finally
        {
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawAssetFields()
    {
        EditorGUILayout.LabelField("Assets", EditorStyles.boldLabel);

        _rewardDatabase = (RewardDatabase)EditorGUILayout.ObjectField(
            "Reward Database",
            _rewardDatabase,
            typeof(RewardDatabase),
            false);
        _hpConfig = (WormHpScalingConfig)EditorGUILayout.ObjectField(
            "HP Config",
            _hpConfig,
            typeof(WormHpScalingConfig),
            false);
        _pressureConfig = (WormPressureConfig)EditorGUILayout.ObjectField(
            "Pressure Config",
            _pressureConfig,
            typeof(WormPressureConfig),
            false);
        _mainWeaponConfig = (WeaponConfig)EditorGUILayout.ObjectField(
            "Main Weapon",
            _mainWeaponConfig,
            typeof(WeaponConfig),
            false);
        _acaciaThornConfig = (AcaciaThornWeaponConfig)EditorGUILayout.ObjectField(
            "Acacia Thorn",
            _acaciaThornConfig,
            typeof(AcaciaThornWeaponConfig),
            false);
        _railPath = (RailPath)EditorGUILayout.ObjectField(
            "Rail Path",
            _railPath,
            typeof(RailPath),
            true);
    }

    private void DrawSimulationFields()
    {
        EditorGUILayout.LabelField("Simulation", EditorStyles.boldLabel);

        _simulationCount = Mathf.Max(1, EditorGUILayout.IntField("Auto Test Runs", _simulationCount));
        _seed = EditorGUILayout.IntField("Seed", _seed);
        _levelNumber = Mathf.Max(1, EditorGUILayout.IntField("Level Number", _levelNumber));
        _totalLength = Mathf.Max(3, EditorGUILayout.IntField("Worm Total Length", _totalLength));
        _wormSpeed = Mathf.Max(0.01f, EditorGUILayout.FloatField("Worm Speed", _wormSpeed));
        _derivePathTimeFromRail = EditorGUILayout.Toggle("Use Rail Length", _derivePathTimeFromRail);
        using (new EditorGUI.DisabledScope(_derivePathTimeFromRail && _railPath != null))
        {
            _pathTimeLimitSeconds = Mathf.Max(1f, EditorGUILayout.FloatField("Path Time Limit", _pathTimeLimitSeconds));
        }

        if (_derivePathTimeFromRail && _railPath != null)
        {
            WormBalancePathMetrics metrics = WormBalancePathMetrics.FromRailPath(
                _railPath,
                _pathTimeLimitSeconds,
                _derivePathTimeFromRail,
                _wormSpeed,
                _progressBucketCount);
            EditorGUILayout.LabelField(
                "Derived Path Time",
                $"{metrics.PathTimeLimitSeconds:0.0}s ({metrics.PathLength:0.00} units / {_wormSpeed:0.00} speed)");
        }

        _segmentSpacing = Mathf.Max(0.01f, EditorGUILayout.FloatField("Segment Spacing", _segmentSpacing));
        _hitEfficiency = Mathf.Clamp(
            EditorGUILayout.Slider("Hit Efficiency", _hitEfficiency, 0.1f, 1.5f),
            0.1f,
            1.5f);
        _progressBucketCount = Mathf.Clamp(
            EditorGUILayout.IntField("Progress Buckets", _progressBucketCount),
            2,
            20);
        _rewardPickStrategy = (WormBalanceRewardPickStrategy)EditorGUILayout.EnumPopup(
            "Reward Pick",
            _rewardPickStrategy);
        _useRuntimePressure = EditorGUILayout.Toggle("Runtime Pressure", _useRuntimePressure);
        _applySectionRollback = EditorGUILayout.Toggle("Section Rollback", _applySectionRollback);
        _writeCsvLog = EditorGUILayout.Toggle("Write CSV Log", _writeCsvLog);
        _logEachRunToConsole = EditorGUILayout.Toggle("Log Each Run", _logEachRunToConsole);
    }

    private void DrawActions()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Run Preview"))
                RunSimulation(1);

            if (GUILayout.Button($"Run {_simulationCount} Auto Tests"))
                RunSimulation(_simulationCount);

            if (GUILayout.Button("Reload Defaults"))
                LoadDefaultAssets(force: true);
        }
    }

    private void DrawSummary()
    {
        EditorGUILayout.LabelField("Last Result", EditorStyles.boldLabel);

        GUIStyle resultStyle = new(EditorStyles.textArea)
        {
            wordWrap = false
        };
        float resultViewHeight = Mathf.Clamp(
            position.height - EstimatedControlsHeight,
            MinResultViewHeight,
            MaxResultViewHeight);
        float textHeight = Mathf.Max(
            MinResultViewHeight,
            resultStyle.CalcHeight(
                new GUIContent(_lastSummary),
                Mathf.Max(240f, position.width - 64f)));

        _resultScroll = EditorGUILayout.BeginScrollView(
            _resultScroll,
            true,
            true,
            GUILayout.Height(resultViewHeight),
            GUILayout.MinHeight(MinResultViewHeight));
        EditorGUILayout.TextArea(
            _lastSummary,
            resultStyle,
            GUILayout.MinHeight(textHeight),
            GUILayout.ExpandWidth(true));
        EditorGUILayout.EndScrollView();
    }

    private void RunSimulation(int runCount)
    {
        WormBalanceSimulationSettings settings = BuildSettings(runCount);

        if (!settings.IsValid(out string error))
        {
            EditorUtility.DisplayDialog("Worm Balance Lab", error, "OK");
            return;
        }

        WormBalanceSimulationReport report = WormBalanceSimulator.Run(settings);
        _lastSummary = report.BuildSummary();

        Debug.Log(_lastSummary);

        if (_logEachRunToConsole)
        {
            IReadOnlyList<WormBalanceRunResult> runs = report.Runs;

            for (int i = 0; i < runs.Count; i++)
                Debug.Log(runs[i].BuildDebugLine());
        }

        if (_writeCsvLog)
        {
            string csvPath = WormBalanceCsvWriter.Write(report);
            Debug.Log($"Worm Balance Lab CSV: {csvPath}");
        }
    }

    private WormBalanceSimulationSettings BuildSettings(int runCount)
    {
        return new WormBalanceSimulationSettings(
            _rewardDatabase,
            _hpConfig,
            _pressureConfig,
            _mainWeaponConfig,
            _acaciaThornConfig,
            Mathf.Max(1, runCount),
            _seed,
            _levelNumber,
            _totalLength,
            _pathTimeLimitSeconds,
            _derivePathTimeFromRail,
            _wormSpeed,
            _segmentSpacing,
            _hitEfficiency,
            _progressBucketCount,
            _useRuntimePressure,
            _applySectionRollback,
            _rewardPickStrategy,
            WormBalancePathMetrics.FromRailPath(
                _railPath,
                _pathTimeLimitSeconds,
                _derivePathTimeFromRail,
                _wormSpeed,
                _progressBucketCount));
    }

    private void LoadDefaultAssets(bool force = false)
    {
        if (force || _rewardDatabase == null)
            _rewardDatabase = AssetDatabase.LoadAssetAtPath<RewardDatabase>(RewardDatabasePath);

        if (force || _hpConfig == null)
            _hpConfig = AssetDatabase.LoadAssetAtPath<WormHpScalingConfig>(HpConfigPath);

        if (force || _pressureConfig == null)
            _pressureConfig = AssetDatabase.LoadAssetAtPath<WormPressureConfig>(PressureConfigPath);

        if (force || _mainWeaponConfig == null)
            _mainWeaponConfig = AssetDatabase.LoadAssetAtPath<WeaponConfig>(MainWeaponConfigPath);

        if (force || _acaciaThornConfig == null)
            _acaciaThornConfig = AssetDatabase.LoadAssetAtPath<AcaciaThornWeaponConfig>(AcaciaThornConfigPath);
    }
}

public enum WormBalanceRewardPickStrategy
{
    RandomChoice = 0,
    HighestRarityThenRandom = 1,
    HighestEstimatedDpsGain = 2
}

internal sealed class WormBalanceSimulationSettings
{
    public readonly RewardDatabase RewardDatabase;
    public readonly WormHpScalingConfig HpConfig;
    public readonly WormPressureConfig PressureConfig;
    public readonly WeaponConfig MainWeaponConfig;
    public readonly AcaciaThornWeaponConfig AcaciaThornConfig;
    public readonly int RunCount;
    public readonly int Seed;
    public readonly int LevelNumber;
    public readonly int TotalLength;
    public readonly float PathTimeLimitSeconds;
    public readonly bool DerivePathTimeFromRail;
    public readonly float WormSpeed;
    public readonly float SegmentSpacing;
    public readonly float HitEfficiency;
    public readonly int ProgressBucketCount;
    public readonly bool UseRuntimePressure;
    public readonly bool ApplySectionRollback;
    public readonly WormBalanceRewardPickStrategy RewardPickStrategy;
    public readonly WormBalancePathMetrics PathMetrics;

    public WormBalanceSimulationSettings(
        RewardDatabase rewardDatabase,
        WormHpScalingConfig hpConfig,
        WormPressureConfig pressureConfig,
        WeaponConfig mainWeaponConfig,
        AcaciaThornWeaponConfig acaciaThornConfig,
        int runCount,
        int seed,
        int levelNumber,
        int totalLength,
        float pathTimeLimitSeconds,
        bool derivePathTimeFromRail,
        float wormSpeed,
        float segmentSpacing,
        float hitEfficiency,
        int progressBucketCount,
        bool useRuntimePressure,
        bool applySectionRollback,
        WormBalanceRewardPickStrategy rewardPickStrategy,
        WormBalancePathMetrics pathMetrics)
    {
        RewardDatabase = rewardDatabase;
        HpConfig = hpConfig;
        PressureConfig = pressureConfig;
        MainWeaponConfig = mainWeaponConfig;
        AcaciaThornConfig = acaciaThornConfig;
        RunCount = Mathf.Max(1, runCount);
        Seed = seed;
        LevelNumber = Mathf.Max(1, levelNumber);
        TotalLength = Mathf.Max(3, totalLength);
        DerivePathTimeFromRail = derivePathTimeFromRail;
        WormSpeed = Mathf.Max(0.01f, wormSpeed);
        PathMetrics = pathMetrics ?? WormBalancePathMetrics.CreateFallback(
            pathTimeLimitSeconds,
            WormSpeed,
            progressBucketCount);
        PathTimeLimitSeconds = Mathf.Max(1f, PathMetrics.PathTimeLimitSeconds);
        SegmentSpacing = Mathf.Max(0.01f, segmentSpacing);
        HitEfficiency = Mathf.Max(0.01f, hitEfficiency);
        ProgressBucketCount = Mathf.Clamp(progressBucketCount, 2, 20);
        UseRuntimePressure = useRuntimePressure;
        ApplySectionRollback = applySectionRollback;
        RewardPickStrategy = rewardPickStrategy;
    }

    public bool IsValid(out string error)
    {
        if (RewardDatabase == null)
        {
            error = "Reward database is missing.";
            return false;
        }

        if (HpConfig == null)
        {
            error = "Worm HP config is missing.";
            return false;
        }

        if (MainWeaponConfig == null || MainWeaponConfig.Projectile == null)
        {
            error = "Main weapon config or projectile config is missing.";
            return false;
        }

        error = null;
        return true;
    }
}

internal sealed class WormBalancePathMetrics
{
    private readonly float[] _controlPointProgresses;
    private readonly int _progressBucketCount;

    private WormBalancePathMetrics(
        float pathLength,
        float pathTimeLimitSeconds,
        float[] controlPointProgresses,
        int progressBucketCount)
    {
        PathLength = Mathf.Max(0f, pathLength);
        PathTimeLimitSeconds = Mathf.Max(1f, pathTimeLimitSeconds);
        _controlPointProgresses = controlPointProgresses ?? Array.Empty<float>();
        _progressBucketCount = Mathf.Clamp(progressBucketCount, 2, 20);
    }

    public float PathLength { get; }
    public float PathTimeLimitSeconds { get; }
    public int ControlPointCount => _controlPointProgresses.Length;

    public static WormBalancePathMetrics FromRailPath(
        RailPath railPath,
        float fallbackPathTimeLimitSeconds,
        bool derivePathTimeFromRail,
        float wormSpeed,
        int progressBucketCount)
    {
        if (railPath == null)
            return CreateFallback(fallbackPathTimeLimitSeconds, wormSpeed, progressBucketCount);

        int pointCount = Mathf.Max(0, railPath.PointCount);

        if (pointCount <= 0)
            pointCount = Mathf.Max(0, railPath.LegacyWaypointCount);

        if (pointCount <= 0)
            pointCount = Mathf.Max(0, railPath.transform.childCount);

        float[] controlPointProgresses = pointCount > 0
            ? new float[pointCount]
            : Array.Empty<float>();

        float totalLength = Mathf.Max(0f, railPath.TotalLength);

        for (int i = 0; i < pointCount; i++)
        {
            if (!TryGetControlPointDistance(
                    railPath,
                    i,
                    out float distance))
            {
                controlPointProgresses[i] = pointCount > 1
                    ? i / (float)(pointCount - 1)
                    : 0f;
                continue;
            }

            totalLength = Mathf.Max(totalLength, railPath.TotalLength);
            controlPointProgresses[i] = totalLength > 0f
                ? Mathf.Clamp01(distance / totalLength)
                : (pointCount > 1 ? i / (float)(pointCount - 1) : 0f);
        }

        float pathTime = derivePathTimeFromRail && totalLength > 0f
            ? totalLength / Mathf.Max(0.01f, wormSpeed)
            : fallbackPathTimeLimitSeconds;

        return new WormBalancePathMetrics(
            totalLength,
            pathTime,
            controlPointProgresses,
            progressBucketCount);
    }

    private static bool TryGetControlPointDistance(
        RailPath railPath,
        int pointIndex,
        out float distance)
    {
        distance = 0f;

        if (railPath == null)
            return false;

        if (railPath.TryGetControlPointDistance(pointIndex, out distance))
            return true;

        if (pointIndex < 0 || pointIndex >= railPath.transform.childCount)
            return false;

        Transform child = railPath.transform.GetChild(pointIndex);

        if (child == null)
            return false;

        distance = railPath.GetClosestDistance(child.position);
        return true;
    }

    public static WormBalancePathMetrics CreateFallback(
        float pathTimeLimitSeconds,
        float wormSpeed,
        int progressBucketCount)
    {
        float safeTime = Mathf.Max(1f, pathTimeLimitSeconds);

        return new WormBalancePathMetrics(
            safeTime * Mathf.Max(0.01f, wormSpeed),
            safeTime,
            Array.Empty<float>(),
            progressBucketCount);
    }

    public WormBalancePathLocation GetLocation(float headProgress)
    {
        float progress = Mathf.Clamp01(headProgress);
        int bucketIndex = Mathf.Clamp(
            Mathf.FloorToInt(progress * _progressBucketCount),
            0,
            _progressBucketCount - 1);

        int controlPointIndex = GetReachedControlPointIndex(progress);

        return new WormBalancePathLocation(
            progress,
            bucketIndex,
            _progressBucketCount,
            controlPointIndex,
            GetControlPointProgress(controlPointIndex));
    }

    private int GetReachedControlPointIndex(float progress)
    {
        if (_controlPointProgresses.Length == 0)
            return -1;

        int index = 0;

        for (int i = 0; i < _controlPointProgresses.Length; i++)
        {
            if (progress + 0.0001f < _controlPointProgresses[i])
                break;

            index = i;
        }

        return index;
    }

    private float GetControlPointProgress(int index)
    {
        if (index < 0 || index >= _controlPointProgresses.Length)
            return -1f;

        return _controlPointProgresses[index];
    }
}

internal readonly struct WormBalancePathLocation
{
    public readonly float HeadProgress;
    public readonly int BucketIndex;
    public readonly int BucketCount;
    public readonly int ControlPointIndex;
    public readonly float ControlPointProgress;

    public WormBalancePathLocation(
        float headProgress,
        int bucketIndex,
        int bucketCount,
        int controlPointIndex,
        float controlPointProgress)
    {
        HeadProgress = Mathf.Clamp01(headProgress);
        BucketIndex = Mathf.Max(0, bucketIndex);
        BucketCount = Mathf.Max(1, bucketCount);
        ControlPointIndex = controlPointIndex;
        ControlPointProgress = controlPointProgress;
    }

    public string BucketLabel
    {
        get
        {
            float start = BucketIndex / (float)BucketCount * 100f;
            float end = (BucketIndex + 1) / (float)BucketCount * 100f;
            return $"{start:0}-{end:0}%";
        }
    }

    public string ControlPointLabel =>
        ControlPointIndex >= 0
            ? $"CP {ControlPointIndex} ({ControlPointProgress * 100f:0.0}%)"
            : "No rail";
}

internal static class WormBalanceSimulator
{
    private const int ThousandHp = 1000;
    private const int TenThousandHp = 10000;
    private const int MillionHp = 1000000;
    private const int TenMillionHp = 10000000;

    public static WormBalanceSimulationReport Run(WormBalanceSimulationSettings settings)
    {
        Random.State previousRandomState = Random.state;
        var runs = new List<WormBalanceRunResult>(settings.RunCount);

        try
        {
            for (int i = 0; i < settings.RunCount; i++)
            {
                Random.InitState(settings.Seed + i * 7919);
                runs.Add(SimulateRun(settings, i));
            }
        }
        finally
        {
            Random.state = previousRandomState;
        }

        return new WormBalanceSimulationReport(settings, runs);
    }

    private static WormBalanceRunResult SimulateRun(
        WormBalanceSimulationSettings settings,
        int runIndex)
    {
        WeaponRuntimeState mainState = CreateMainWeaponState(settings.MainWeaponConfig);
        AcaciaThornRuntimeState acaciaState = CreateAcaciaThornState(settings.AcaciaThornConfig);
        RewardRuntimeContext rewardContext = new(
            mainState,
            acaciaState,
            () => BuildMainWeaponDamage(settings.MainWeaponConfig, mainState));
        RewardRollService rewardRollService = new(settings.RewardDatabase);
        WormSectionHpResolver hpResolver = new(settings.HpConfig);
        WormBalanceSectionState[] sections = BuildSections(settings);

        float time = 0f;
        float headProgress = 0f;
        float pressureSampleTimer = 0f;
        float runtimePressureMultiplier = 1f;
        bool pressureChanged = false;
        int destroyedSegments = 0;
        int rewardsTaken = 0;
        int lastSectionIndex = -1;
        float firstRewardTime = -1f;
        StringBuilder rewardLog = new();

        RebuildSectionHp(
            settings,
            hpResolver,
            sections,
            0,
            mainState,
            acaciaState,
            runtimePressureMultiplier,
            headProgress);

        int totalProgressSegments = CountProgressSegments(sections);

        for (int i = 0; i < sections.Length; i++)
        {
            WormBalanceSectionState section = sections[i];
            WeaponPowerSnapshot power = EstimatePower(settings, mainState, acaciaState);

            if (!power.IsValid || power.EstimatedDps <= 0f)
            {
                return WormBalanceRunResult.Loss(
                    runIndex,
                    "No DPS",
                    time,
                    GetDestructionProgress(destroyedSegments, totalProgressSegments),
                    headProgress,
                    i,
                    lastSectionIndex,
                    rewardsTaken,
                    firstRewardTime,
                    0f,
                    settings.PathMetrics.GetLocation(headProgress),
                    rewardLog.ToString());
            }

            float dps = Mathf.Max(0.01f, power.EstimatedDps * settings.HitEfficiency);
            float killTime = section.Hp / dps;

            if (!AdvanceTime(
                    settings,
                    killTime,
                    ref time,
                    ref headProgress,
                    ref pressureSampleTimer,
                    ref runtimePressureMultiplier,
                    ref pressureChanged))
            {
                return WormBalanceRunResult.Loss(
                    runIndex,
                    "Path completed",
                    time,
                    GetDestructionProgress(destroyedSegments, totalProgressSegments),
                    headProgress,
                    i,
                    lastSectionIndex,
                    rewardsTaken,
                    firstRewardTime,
                    dps,
                    settings.PathMetrics.GetLocation(headProgress),
                    rewardLog.ToString());
            }

            destroyedSegments += section.SegmentCount;
            lastSectionIndex = i;

            if (settings.ApplySectionRollback)
                headProgress = ApplyRollback(settings, section.SegmentCount, headProgress);

            if (pressureChanged)
            {
                RebuildSectionHp(
                    settings,
                    hpResolver,
                    sections,
                    i + 1,
                    mainState,
                    acaciaState,
                    runtimePressureMultiplier,
                    headProgress);
                pressureChanged = false;
            }

            if (!section.HasCocoon)
                continue;

            List<RewardChoiceData> choices = rewardRollService.Roll3(
                rewardContext,
                section.CocoonProfile);
            RewardChoiceData selectedReward = PickReward(
                choices,
                settings,
                mainState,
                acaciaState,
                out float selectedDpsGain);

            if (selectedReward == null || selectedReward.Effect == null)
            {
                AppendRewardLog(
                    rewardLog,
                    time,
                    section.CocoonProfile,
                    null,
                    0f);
                continue;
            }

            selectedReward.Effect.Apply(rewardContext);
            rewardsTaken++;

            if (firstRewardTime < 0f)
                firstRewardTime = time;

            AppendRewardLog(
                rewardLog,
                time,
                section.CocoonProfile,
                selectedReward,
                selectedDpsGain);

            RebuildSectionHp(
                settings,
                hpResolver,
                sections,
                i + 1,
                mainState,
                acaciaState,
                runtimePressureMultiplier,
                headProgress);
        }

        WeaponPowerSnapshot finalPower = EstimatePower(settings, mainState, acaciaState);

        return WormBalanceRunResult.Win(
            runIndex,
            time,
            GetDestructionProgress(destroyedSegments, totalProgressSegments),
            headProgress,
            sections.Length,
            lastSectionIndex,
            rewardsTaken,
            firstRewardTime,
            finalPower.IsValid ? finalPower.EstimatedDps * settings.HitEfficiency : 0f,
            settings.PathMetrics.GetLocation(headProgress),
            rewardLog.ToString());
    }

    private static WormBalanceSectionState[] BuildSections(
        WormBalanceSimulationSettings settings)
    {
        int bodySegmentCount = Mathf.Max(1, settings.TotalLength - 2);
        int totalSections = WormCocoonRules.CountGameplaySections(bodySegmentCount);
        var sections = new WormBalanceSectionState[totalSections];
        int remainingSegments = bodySegmentCount;
        int sectionsWithoutCocoon = 0;

        for (int i = 0; i < totalSections; i++)
        {
            int segmentCount = Mathf.Min(
                WormCocoonRules.SectionSize,
                remainingSegments);
            float progress = WormCocoonRules.GetSectionProgress(i, totalSections);
            CocoonRewardProfile cocoonProfile = null;

            if (WormCocoonRules.ShouldPlaceCocoon(
                    i,
                    totalSections,
                    progress,
                    sectionsWithoutCocoon))
            {
                cocoonProfile = WormCocoonRules.RollCocoonProfile(
                    settings.RewardDatabase.CocoonProfiles,
                    progress);
                sectionsWithoutCocoon = 0;
            }
            else
            {
                sectionsWithoutCocoon++;
            }

            sections[i] = new WormBalanceSectionState(
                i,
                Mathf.Max(1, segmentCount),
                cocoonProfile);
            remainingSegments -= segmentCount;
        }

        return sections;
    }

    private static void RebuildSectionHp(
        WormBalanceSimulationSettings settings,
        WormSectionHpResolver hpResolver,
        WormBalanceSectionState[] sections,
        int startIndex,
        WeaponRuntimeState mainState,
        AcaciaThornRuntimeState acaciaState,
        float runtimePressureMultiplier,
        float headProgress)
    {
        if (sections == null || sections.Length == 0)
            return;

        WeaponPowerSnapshot power = EstimatePower(settings, mainState, acaciaState);
        int previousHp = 0;

        for (int i = 0; i < sections.Length; i++)
        {
            int baseHp = WormSectionHPGenerator.GetHP(i, settings.LevelNumber);
            int resolvedHp = hpResolver.ResolveHp(
                baseHp,
                i,
                sections.Length,
                settings.LevelNumber,
                power,
                runtimePressureMultiplier,
                GetHeadPressureMultiplier(settings, headProgress));
            int hp = EnsureHpAbovePrevious(resolvedHp, previousHp);

            if (i >= startIndex)
                sections[i].Hp = hp;

            previousHp = i >= startIndex
                ? hp
                : Mathf.Max(previousHp, sections[i].Hp);
        }
    }

    private static bool AdvanceTime(
        WormBalanceSimulationSettings settings,
        float duration,
        ref float time,
        ref float headProgress,
        ref float pressureSampleTimer,
        ref float runtimePressureMultiplier,
        ref bool pressureChanged)
    {
        float remaining = Mathf.Max(0f, duration);

        while (remaining > 0f)
        {
            float step = remaining;

            if (settings.UseRuntimePressure && settings.PressureConfig != null)
            {
                float timeToPressureSample = Mathf.Max(
                    0.0001f,
                    settings.PressureConfig.SampleInterval - pressureSampleTimer);
                step = Mathf.Min(step, timeToPressureSample);
            }

            if (settings.PathTimeLimitSeconds > 0f)
            {
                float timeToPathEnd = (1f - headProgress) * settings.PathTimeLimitSeconds;

                if (step >= timeToPathEnd)
                {
                    time += Mathf.Max(0f, timeToPathEnd);
                    headProgress = 1f;
                    return false;
                }
            }

            time += step;
            headProgress = Mathf.Clamp01(headProgress + step / settings.PathTimeLimitSeconds);
            remaining -= step;

            if (!settings.UseRuntimePressure || settings.PressureConfig == null)
                continue;

            pressureSampleTimer += step;

            if (pressureSampleTimer + Mathf.Epsilon < settings.PressureConfig.SampleInterval)
                continue;

            pressureSampleTimer = 0f;
            float nextPressure = CalculateRuntimePressure(
                settings.PressureConfig,
                time,
                headProgress,
                runtimePressureMultiplier);

            if (Mathf.Approximately(nextPressure, runtimePressureMultiplier))
                continue;

            runtimePressureMultiplier = nextPressure;
            pressureChanged = true;
        }

        return true;
    }

    private static float CalculateRuntimePressure(
        WormPressureConfig config,
        float elapsedTime,
        float headProgress,
        float currentPressure)
    {
        float expectedProgress = config.GetExpectedProgress(elapsedTime);
        float deadZone = config.ProgressDeadZone;

        if (headProgress + deadZone < expectedProgress)
            return Mathf.Min(config.MaxMultiplier, currentPressure + config.IncreasePerSample);

        if (headProgress > expectedProgress + deadZone)
            return Mathf.Max(1f, currentPressure - config.RecoveryPerSample);

        return currentPressure;
    }

    private static RewardChoiceData PickReward(
        List<RewardChoiceData> choices,
        WormBalanceSimulationSettings settings,
        WeaponRuntimeState mainState,
        AcaciaThornRuntimeState acaciaState,
        out float selectedDpsGain)
    {
        selectedDpsGain = 0f;

        if (choices == null || choices.Count == 0)
            return null;

        if (TryPickLockedWeaponUnlockReward(
                choices,
                settings,
                mainState,
                acaciaState,
                out RewardChoiceData unlockReward,
                out selectedDpsGain))
        {
            return unlockReward;
        }

        if (settings.RewardPickStrategy == WormBalanceRewardPickStrategy.RandomChoice)
            return choices[Random.Range(0, choices.Count)];

        if (settings.RewardPickStrategy == WormBalanceRewardPickStrategy.HighestEstimatedDpsGain)
            return PickHighestEstimatedDpsGainReward(
                choices,
                settings,
                mainState,
                acaciaState,
                out selectedDpsGain);

        RewardRarity bestRarity = RewardRarity.Common;

        for (int i = 0; i < choices.Count; i++)
        {
            if (choices[i] != null && choices[i].Rarity > bestRarity)
                bestRarity = choices[i].Rarity;
        }

        int matchingCount = 0;

        for (int i = 0; i < choices.Count; i++)
        {
            if (choices[i] != null && choices[i].Rarity == bestRarity)
                matchingCount++;
        }

        int selectedIndex = Random.Range(0, matchingCount);
        int currentIndex = 0;

        for (int i = 0; i < choices.Count; i++)
        {
            if (choices[i] == null || choices[i].Rarity != bestRarity)
                continue;

            if (currentIndex == selectedIndex)
                return choices[i];

            currentIndex++;
        }

        return choices[0];
    }

    private static bool TryPickLockedWeaponUnlockReward(
        List<RewardChoiceData> choices,
        WormBalanceSimulationSettings settings,
        WeaponRuntimeState mainState,
        AcaciaThornRuntimeState acaciaState,
        out RewardChoiceData selectedReward,
        out float selectedDpsGain)
    {
        selectedReward = null;
        selectedDpsGain = float.MinValue;

        RewardRuntimeContext currentContext = new(
            mainState,
            acaciaState,
            () => BuildMainWeaponDamage(settings.MainWeaponConfig, mainState));

        for (int i = 0; i < choices.Count; i++)
        {
            RewardChoiceData choice = choices[i];

            if (!IsWeaponUnlockReward(choice))
                continue;

            if (choice.Effect == null || !choice.Effect.CanApply(currentContext))
                continue;

            float dpsGain = CalculateEstimatedDpsGain(
                choice,
                settings,
                mainState,
                acaciaState);

            if (selectedReward != null && dpsGain <= selectedDpsGain + 0.0001f)
                continue;

            selectedReward = choice;
            selectedDpsGain = dpsGain;
        }

        if (selectedReward != null)
            return true;

        selectedDpsGain = 0f;
        return false;
    }

    private static bool IsWeaponUnlockReward(RewardChoiceData choice)
    {
        if (choice == null)
            return false;

        return choice.Category == RewardModifierCategory.AcaciaThornUnlock;
    }

    private static RewardChoiceData PickHighestEstimatedDpsGainReward(
        List<RewardChoiceData> choices,
        WormBalanceSimulationSettings settings,
        WeaponRuntimeState mainState,
        AcaciaThornRuntimeState acaciaState,
        out float selectedDpsGain)
    {
        selectedDpsGain = float.MinValue;
        float bestRarityScore = -1f;
        var candidates = new List<RewardChoiceData>(choices.Count);

        for (int i = 0; i < choices.Count; i++)
        {
            RewardChoiceData choice = choices[i];

            if (choice == null || choice.Effect == null)
                continue;

            float dpsGain = CalculateEstimatedDpsGain(
                choice,
                settings,
                mainState,
                acaciaState);
            float rarityScore = (int)choice.Rarity;

            if (dpsGain > selectedDpsGain + 0.0001f)
            {
                candidates.Clear();
                candidates.Add(choice);
                selectedDpsGain = dpsGain;
                bestRarityScore = rarityScore;
                continue;
            }

            if (Mathf.Abs(dpsGain - selectedDpsGain) > 0.0001f)
                continue;

            if (rarityScore > bestRarityScore)
            {
                candidates.Clear();
                candidates.Add(choice);
                bestRarityScore = rarityScore;
                continue;
            }

            if (Mathf.Approximately(rarityScore, bestRarityScore))
                candidates.Add(choice);
        }

        if (candidates.Count == 0)
            return choices[Random.Range(0, choices.Count)];

        return candidates[Random.Range(0, candidates.Count)];
    }

    private static float CalculateEstimatedDpsGain(
        RewardChoiceData choice,
        WormBalanceSimulationSettings settings,
        WeaponRuntimeState mainState,
        AcaciaThornRuntimeState acaciaState)
    {
        WeaponRuntimeState mainClone = mainState.Clone();
        AcaciaThornRuntimeState acaciaClone = acaciaState.Clone();
        RewardRuntimeContext clonedContext = new(
            mainClone,
            acaciaClone,
            () => BuildMainWeaponDamage(settings.MainWeaponConfig, mainClone));

        WeaponPowerSnapshot before = EstimatePower(settings, mainState, acaciaState);

        if (!choice.Effect.CanApply(clonedContext))
            return float.NegativeInfinity;

        choice.Effect.Apply(clonedContext);

        WeaponPowerSnapshot after = EstimatePower(settings, mainClone, acaciaClone);

        float beforeDps = before.IsValid ? before.EstimatedDps : 0f;
        float afterDps = after.IsValid ? after.EstimatedDps : 0f;

        return afterDps - beforeDps;
    }

    private static WeaponRuntimeState CreateMainWeaponState(WeaponConfig config)
    {
        WeaponRuntimeState state = new();

        if (config == null)
            return state;

        state.SetFireRateBonusLimit(config.MaxFireRateBonus);
        state.SetProjectileSpeedBonusLimit(config.MaxProjectileSpeedBonus);
        state.SetProgressionLimits(
            config.MaxDamageMultiplier,
            config.MaxCriticalChance,
            config.MaxCriticalDamageMultiplier,
            config.MaxPenetrationBonus,
            config.MaxParallelProjectiles,
            config.MaxSalvoExtraShots);

        return state;
    }

    private static AcaciaThornRuntimeState CreateAcaciaThornState(
        AcaciaThornWeaponConfig config)
    {
        AcaciaThornRuntimeState state = new();

        if (config == null)
            return state;

        state.SetProgressionLimits(
            config.MaxDamageMultiplier,
            config.MaxFireRateBonus,
            config.MaxSalvoExtraShots,
            config.MaxProjectileSpeedBonus,
            config.MaxCriticalChance,
            config.CriticalDamageMultiplier,
            config.MaxCriticalDamageMultiplier);
        state.SetBaseDamage(config.Damage);

        return state;
    }

    private static WeaponPowerSnapshot EstimatePower(
        WormBalanceSimulationSettings settings,
        WeaponRuntimeState mainState,
        AcaciaThornRuntimeState acaciaState)
    {
        return WeaponPowerEstimator.Estimate(
            settings.MainWeaponConfig,
            mainState,
            settings.AcaciaThornConfig,
            acaciaState);
    }

    private static int BuildMainWeaponDamage(
        WeaponConfig config,
        WeaponRuntimeState state)
    {
        if (config == null || config.Projectile == null || state == null)
            return 0;

        return WeaponRuntimeState.ClampDamage(
            config.Projectile.Damage * (double)state.DamageMultiplier);
    }

    private static float GetHeadPressureMultiplier(
        WormBalanceSimulationSettings settings,
        float headProgress)
    {
        return settings.HpConfig != null
            ? settings.HpConfig.GetHeadPathPressureMultiplier(headProgress)
            : 1f;
    }

    private static float ApplyRollback(
        WormBalanceSimulationSettings settings,
        int destroyedSegmentCount,
        float headProgress)
    {
        float pathLength = settings.PathMetrics.PathLength;

        if (pathLength <= 0f)
            return headProgress;

        float rollbackProgress = destroyedSegmentCount * settings.SegmentSpacing / pathLength;
        return Mathf.Clamp01(headProgress - rollbackProgress);
    }

    private static int EnsureHpAbovePrevious(int hp, int previousHp)
    {
        if (previousHp <= 0)
            return Mathf.Max(1, hp);

        if (previousHp >= WeaponRuntimeState.MaxProjectileDamage)
            return WeaponRuntimeState.MaxProjectileDamage;

        int minimumIncrease = GetMinimumVisibleHpIncrease(previousHp);

        return Mathf.Min(
            WeaponRuntimeState.MaxProjectileDamage,
            Mathf.Max(hp, previousHp + minimumIncrease));
    }

    private static int GetMinimumVisibleHpIncrease(int previousHp)
    {
        if (previousHp < ThousandHp)
            return 1;

        if (previousHp < TenThousandHp)
            return 100;

        if (previousHp < MillionHp)
            return 1000;

        if (previousHp < TenMillionHp)
            return 100000;

        return 1000000;
    }

    private static int CountProgressSegments(WormBalanceSectionState[] sections)
    {
        if (sections == null)
            return 0;

        int count = 0;

        for (int i = 0; i < sections.Length; i++)
            count += sections[i].SegmentCount;

        return count;
    }

    private static float GetDestructionProgress(
        int destroyedSegments,
        int totalSegments)
    {
        return totalSegments > 0
            ? Mathf.Clamp01(destroyedSegments / (float)totalSegments)
            : 0f;
    }

    private static void AppendRewardLog(
        StringBuilder builder,
        float time,
        CocoonRewardProfile cocoonProfile,
        RewardChoiceData reward,
        float dpsGain)
    {
        if (builder == null)
            return;

        if (builder.Length > 0)
            builder.Append(" | ");

        string profileName = cocoonProfile != null
            ? cocoonProfile.DisplayName
            : "NoProfile";

        if (reward == null)
        {
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0:0.0}s {1}: no reward",
                time,
                profileName);
            return;
        }

        builder.AppendFormat(
            CultureInfo.InvariantCulture,
            "{0:0.0}s {1}: {2} {3} {4}",
            time,
            profileName,
            reward.Rarity,
            reward.Title,
            reward.ValueText);

        if (dpsGain > 0f)
        {
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                " (+{0:0.00} DPS)",
                dpsGain);
        }
    }
}

internal sealed class WormBalanceSectionState
{
    public readonly int Index;
    public readonly int SegmentCount;
    public readonly CocoonRewardProfile CocoonProfile;

    public int Hp;
    public bool HasCocoon => CocoonProfile != null;

    public WormBalanceSectionState(
        int index,
        int segmentCount,
        CocoonRewardProfile cocoonProfile)
    {
        Index = index;
        SegmentCount = Mathf.Max(1, segmentCount);
        CocoonProfile = cocoonProfile;
    }
}

internal sealed class WormBalanceRunResult
{
    public readonly int RunIndex;
    public readonly bool Won;
    public readonly string Reason;
    public readonly float TimeSeconds;
    public readonly float DestructionProgress;
    public readonly float HeadProgress;
    public readonly int SectionsDestroyed;
    public readonly int LastSectionIndex;
    public readonly int RewardsTaken;
    public readonly float FirstRewardTime;
    public readonly float FinalDps;
    public readonly WormBalancePathLocation EndLocation;
    public readonly string RewardLog;

    private WormBalanceRunResult(
        int runIndex,
        bool won,
        string reason,
        float timeSeconds,
        float destructionProgress,
        float headProgress,
        int sectionsDestroyed,
        int lastSectionIndex,
        int rewardsTaken,
        float firstRewardTime,
        float finalDps,
        WormBalancePathLocation endLocation,
        string rewardLog)
    {
        RunIndex = runIndex;
        Won = won;
        Reason = reason;
        TimeSeconds = timeSeconds;
        DestructionProgress = destructionProgress;
        HeadProgress = headProgress;
        SectionsDestroyed = sectionsDestroyed;
        LastSectionIndex = lastSectionIndex;
        RewardsTaken = rewardsTaken;
        FirstRewardTime = firstRewardTime;
        FinalDps = finalDps;
        EndLocation = endLocation;
        RewardLog = rewardLog ?? string.Empty;
    }

    public static WormBalanceRunResult Win(
        int runIndex,
        float timeSeconds,
        float destructionProgress,
        float headProgress,
        int sectionsDestroyed,
        int lastSectionIndex,
        int rewardsTaken,
        float firstRewardTime,
        float finalDps,
        WormBalancePathLocation endLocation,
        string rewardLog)
    {
        return new WormBalanceRunResult(
            runIndex,
            true,
            "Worm destroyed",
            timeSeconds,
            destructionProgress,
            headProgress,
            sectionsDestroyed,
            lastSectionIndex,
            rewardsTaken,
            firstRewardTime,
            finalDps,
            endLocation,
            rewardLog);
    }

    public static WormBalanceRunResult Loss(
        int runIndex,
        string reason,
        float timeSeconds,
        float destructionProgress,
        float headProgress,
        int sectionsDestroyed,
        int lastSectionIndex,
        int rewardsTaken,
        float firstRewardTime,
        float finalDps,
        WormBalancePathLocation endLocation,
        string rewardLog)
    {
        return new WormBalanceRunResult(
            runIndex,
            false,
            reason,
            timeSeconds,
            destructionProgress,
            headProgress,
            sectionsDestroyed,
            lastSectionIndex,
            rewardsTaken,
            firstRewardTime,
            finalDps,
            endLocation,
            rewardLog);
    }

    public string BuildDebugLine()
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "WormBalance run={0} result={1} reason='{2}' time={3:0.0}s destroyed={4:0.0}% head={5:0.0}% bucket={6} rail={7} sections={8} rewards={9} firstReward={10} dps={11:0.00} rewardsLog='{12}'",
            RunIndex,
            Won ? "WIN" : "LOSS",
            Reason,
            TimeSeconds,
            DestructionProgress * 100f,
            HeadProgress * 100f,
            EndLocation.BucketLabel,
            EndLocation.ControlPointLabel,
            SectionsDestroyed,
            RewardsTaken,
            FirstRewardTime >= 0f ? FirstRewardTime.ToString("0.0s", CultureInfo.InvariantCulture) : "none",
            FinalDps,
            RewardLog);
    }
}

internal sealed class WormBalanceSimulationReport
{
    public readonly WormBalanceSimulationSettings Settings;
    public readonly List<WormBalanceRunResult> Runs;

    public WormBalanceSimulationReport(
        WormBalanceSimulationSettings settings,
        List<WormBalanceRunResult> runs)
    {
        Settings = settings;
        Runs = runs ?? new List<WormBalanceRunResult>();
    }

    public string BuildSummary()
    {
        int winCount = 0;
        int lossCount = 0;
        float totalTime = 0f;
        float totalRewards = 0f;
        float totalFirstRewardTime = 0f;
        int firstRewardSamples = 0;
        List<float> lossProgress = new();
        List<float> lossHeadProgress = new();
        List<float> winTimes = new();
        List<float> winHeadProgress = new();

        for (int i = 0; i < Runs.Count; i++)
        {
            WormBalanceRunResult run = Runs[i];
            totalTime += run.TimeSeconds;
            totalRewards += run.RewardsTaken;

            if (run.FirstRewardTime >= 0f)
            {
                totalFirstRewardTime += run.FirstRewardTime;
                firstRewardSamples++;
            }

            if (run.Won)
            {
                winCount++;
                winTimes.Add(run.TimeSeconds);
                winHeadProgress.Add(run.HeadProgress);
            }
            else
            {
                lossCount++;
                lossProgress.Add(run.DestructionProgress);
                lossHeadProgress.Add(run.HeadProgress);
            }
        }

        float runCount = Mathf.Max(1, Runs.Count);
        float winRate = winCount / runCount;
        float averageRewards = totalRewards / runCount;
        float averageFirstRewardTime = firstRewardSamples > 0
            ? totalFirstRewardTime / firstRewardSamples
            : -1f;
        lossProgress.Sort();
        lossHeadProgress.Sort();
        winTimes.Sort();
        winHeadProgress.Sort();

        StringBuilder builder = new();
        builder.AppendLine("Worm Balance Lab");
        builder.AppendLine($"Runs: {Runs.Count}");
        builder.AppendLine($"Reward pick: {Settings.RewardPickStrategy}");
        builder.AppendLine($"Result: {winCount} wins / {lossCount} losses ({winRate * 100f:0.0}% win rate)");
        builder.AppendLine($"Average time: {totalTime / runCount:0.0}s");
        builder.AppendLine($"Average rewards: {averageRewards:0.00}");
        builder.AppendLine(firstRewardSamples > 0
            ? $"Average first reward: {averageFirstRewardTime:0.0}s"
            : "Average first reward: none");
        builder.AppendLine();
        AppendBalanceVerdict(
            builder,
            winRate,
            averageRewards,
            averageFirstRewardTime,
            lossCount,
            lossProgress,
            lossHeadProgress);

        if (lossCount > 0)
        {
            builder.AppendLine(
                $"Loss destruction progress: avg={Average(lossProgress) * 100f:0.0}% p10={Percentile(lossProgress, 0.1f) * 100f:0.0}% p50={Percentile(lossProgress, 0.5f) * 100f:0.0}% p90={Percentile(lossProgress, 0.9f) * 100f:0.0}%");
            builder.AppendLine(
                $"Loss path progress: avg={Average(lossHeadProgress) * 100f:0.0}% p10={Percentile(lossHeadProgress, 0.1f) * 100f:0.0}% p50={Percentile(lossHeadProgress, 0.5f) * 100f:0.0}% p90={Percentile(lossHeadProgress, 0.9f) * 100f:0.0}%");
        }

        if (winCount > 0)
        {
            builder.AppendLine(
                $"Win time: avg={Average(winTimes):0.0}s p50={Percentile(winTimes, 0.5f):0.0}s p90={Percentile(winTimes, 0.9f):0.0}s");
            builder.AppendLine(
                $"Win kill path progress: avg={Average(winHeadProgress) * 100f:0.0}% p10={Percentile(winHeadProgress, 0.1f) * 100f:0.0}% p50={Percentile(winHeadProgress, 0.5f) * 100f:0.0}% p90={Percentile(winHeadProgress, 0.9f) * 100f:0.0}%");
        }

        builder.AppendLine();
        AppendLocationDistribution(builder, "Win kill locations", won: true);
        AppendLocationDistribution(builder, "Loss locations", won: false);

        builder.AppendLine();
        builder.AppendLine("Worst losses:");
        AppendWorstLosses(builder);

        builder.AppendLine();
        builder.AppendLine("Sample runs:");
        AppendSampleRuns(builder);

        return builder.ToString();
    }

    private static void AppendBalanceVerdict(
        StringBuilder builder,
        float winRate,
        float averageRewards,
        float averageFirstRewardTime,
        int lossCount,
        List<float> lossProgress,
        List<float> lossHeadProgress)
    {
        float averageLossDestroyed = Average(lossProgress);
        float averageLossPath = Average(lossHeadProgress);

        builder.AppendLine("Verdict:");

        if (averageFirstRewardTime < 0f)
        {
            builder.AppendLine("- Player does not reach the first reward. Lower early HP heavily.");
        }
        else if (averageFirstRewardTime > 12f)
        {
            builder.AppendLine(
                $"- First reward is late ({averageFirstRewardTime:0.0}s). Lower only early HP, keep mid/end pressure.");
        }
        else if (averageFirstRewardTime < 6f)
        {
            builder.AppendLine(
                $"- First reward is very fast ({averageFirstRewardTime:0.0}s). Early hook is strong enough.");
        }
        else
        {
            builder.AppendLine(
                $"- First reward timing is good ({averageFirstRewardTime:0.0}s).");
        }

        if (averageRewards < 3f)
        {
            builder.AppendLine(
                $"- Too few rewards before loss ({averageRewards:0.00}). Early/mid HP is too high for fun pacing.");
        }
        else if (averageRewards > 7f)
        {
            builder.AppendLine(
                $"- Many rewards before loss ({averageRewards:0.00}). Raise mid/end HP if win rate grows.");
        }
        else
        {
            builder.AppendLine(
                $"- Reward count before loss is in a useful range ({averageRewards:0.00}).");
        }

        if (winRate <= 0.1f)
        {
            builder.AppendLine(
                $"- Loss target is strong ({winRate * 100f:0.0}% wins). Keep this if revive/ad continuation is the goal.");
        }
        else
        {
            builder.AppendLine(
                $"- Too many wins ({winRate * 100f:0.0}%). Raise mid/end HP, not early HP.");
        }

        if (lossCount > 0)
        {
            builder.AppendLine(
                $"- Loss happens at path {averageLossPath * 100f:0.0}% with worm destroyed {averageLossDestroyed * 100f:0.0}%.");

            if (averageLossPath >= 0.95f && averageLossDestroyed < 0.9f)
                builder.AppendLine("- Player reaches the endpoint with a lot of worm left. Make early rewards faster, then retest.");
            else if (averageLossPath >= 0.85f && averageLossDestroyed >= 0.85f)
                builder.AppendLine("- Loss is late and close. This is a good tension zone.");
        }

        builder.AppendLine("Recommended first tuning:");
        builder.AppendLine("- Set early Base Section HP around 3-4.");
        builder.AppendLine("- Set target lifetime curve early keys around 1.1-1.6s, first reward target 8-12s.");
        builder.AppendLine("- If wins appear after that, raise only mid/end lifetime or pressure curve.");
    }

    private void AppendWorstLosses(StringBuilder builder)
    {
        int appended = 0;
        HashSet<int> listedRuns = new();

        for (int i = 0; i < Runs.Count && appended < 8; i++)
        {
            WormBalanceRunResult worst = null;

            for (int j = 0; j < Runs.Count; j++)
            {
                WormBalanceRunResult candidate = Runs[j];

                if (candidate.Won)
                    continue;

                if (listedRuns.Contains(candidate.RunIndex))
                    continue;

                if (worst == null || candidate.DestructionProgress < worst.DestructionProgress)
                    worst = candidate;
            }

            if (worst == null)
                break;

            builder.AppendLine(worst.BuildDebugLine());
            listedRuns.Add(worst.RunIndex);
            appended++;
        }

        if (appended == 0)
            builder.AppendLine("No losses.");
    }

    private void AppendLocationDistribution(
        StringBuilder builder,
        string title,
        bool won)
    {
        int total = 0;
        Dictionary<int, int> bucketCounts = new();
        Dictionary<int, int> controlPointCounts = new();

        for (int i = 0; i < Runs.Count; i++)
        {
            WormBalanceRunResult run = Runs[i];

            if (run.Won != won)
                continue;

            total++;
            Increment(bucketCounts, run.EndLocation.BucketIndex);
            Increment(controlPointCounts, run.EndLocation.ControlPointIndex);
        }

        builder.AppendLine($"{title}:");

        if (total == 0)
        {
            builder.AppendLine("No samples.");
            return;
        }

        builder.Append("Path buckets: ");
        AppendBucketCounts(
            builder,
            bucketCounts,
            total,
            Settings.ProgressBucketCount);
        builder.AppendLine();

        if (Settings.PathMetrics.ControlPointCount <= 0)
        {
            builder.AppendLine("Rail control points: no RailPath assigned.");
            return;
        }

        builder.Append("Rail control points: ");
        AppendControlPointCounts(
            builder,
            controlPointCounts,
            total,
            Settings.PathMetrics.ControlPointCount);
        builder.AppendLine();
    }

    private static void AppendBucketCounts(
        StringBuilder builder,
        Dictionary<int, int> counts,
        int total,
        int bucketCount)
    {
        bool wroteAny = false;

        for (int i = 0; i < bucketCount; i++)
        {
            if (!counts.TryGetValue(i, out int count))
                continue;

            if (wroteAny)
                builder.Append("; ");

            float start = i / (float)Mathf.Max(1, bucketCount) * 100f;
            float end = (i + 1) / (float)Mathf.Max(1, bucketCount) * 100f;
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0:0}-{1:0}%: {2} ({3:0.0}%)",
                start,
                end,
                count,
                count / (float)total * 100f);
            wroteAny = true;
        }

        if (!wroteAny)
            builder.Append("none");
    }

    private static void AppendControlPointCounts(
        StringBuilder builder,
        Dictionary<int, int> counts,
        int total,
        int controlPointCount)
    {
        bool wroteAny = false;

        for (int i = 0; i < controlPointCount; i++)
        {
            if (!counts.TryGetValue(i, out int count))
                continue;

            if (wroteAny)
                builder.Append("; ");

            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "CP {0}: {1} ({2:0.0}%)",
                i,
                count,
                count / (float)total * 100f);
            wroteAny = true;
        }

        if (!wroteAny)
            builder.Append("none");
    }

    private static void Increment(Dictionary<int, int> counts, int key)
    {
        if (counts.TryGetValue(key, out int value))
        {
            counts[key] = value + 1;
            return;
        }

        counts.Add(key, 1);
    }

    private void AppendSampleRuns(StringBuilder builder)
    {
        int count = Mathf.Min(8, Runs.Count);

        for (int i = 0; i < count; i++)
            builder.AppendLine(Runs[i].BuildDebugLine());
    }

    private static float Average(List<float> values)
    {
        if (values == null || values.Count == 0)
            return 0f;

        float total = 0f;

        for (int i = 0; i < values.Count; i++)
            total += values[i];

        return total / values.Count;
    }

    private static float Percentile(List<float> sortedValues, float percentile)
    {
        if (sortedValues == null || sortedValues.Count == 0)
            return 0f;

        if (sortedValues.Count == 1)
            return sortedValues[0];

        float position = Mathf.Clamp01(percentile) * (sortedValues.Count - 1);
        int lower = Mathf.FloorToInt(position);
        int upper = Mathf.CeilToInt(position);

        if (lower == upper)
            return sortedValues[lower];

        return Mathf.Lerp(
            sortedValues[lower],
            sortedValues[upper],
            position - lower);
    }
}

internal static class WormBalanceCsvWriter
{
    public static string Write(WormBalanceSimulationReport report)
    {
        string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp"));
        Directory.CreateDirectory(directory);

        string path = Path.Combine(
            directory,
            $"WormBalanceLab_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

        File.WriteAllText(path, BuildCsv(report), Encoding.UTF8);
        return path;
    }

    private static string BuildCsv(WormBalanceSimulationReport report)
    {
        StringBuilder builder = new();
        builder.AppendLine("run,result,reason,time_seconds,destruction_progress,head_progress,path_bucket,rail_control_point,rail_control_point_progress,sections_destroyed,last_section_index,rewards_taken,first_reward_time,final_dps,reward_log");

        IReadOnlyList<WormBalanceRunResult> runs = report.Runs;

        for (int i = 0; i < runs.Count; i++)
        {
            WormBalanceRunResult run = runs[i];
            builder.Append(run.RunIndex);
            builder.Append(',');
            builder.Append(run.Won ? "WIN" : "LOSS");
            builder.Append(',');
            AppendCsv(builder, run.Reason);
            builder.Append(',');
            AppendFloat(builder, run.TimeSeconds);
            builder.Append(',');
            AppendFloat(builder, run.DestructionProgress);
            builder.Append(',');
            AppendFloat(builder, run.HeadProgress);
            builder.Append(',');
            AppendCsv(builder, run.EndLocation.BucketLabel);
            builder.Append(',');
            builder.Append(run.EndLocation.ControlPointIndex);
            builder.Append(',');
            AppendFloat(builder, run.EndLocation.ControlPointProgress);
            builder.Append(',');
            builder.Append(run.SectionsDestroyed);
            builder.Append(',');
            builder.Append(run.LastSectionIndex);
            builder.Append(',');
            builder.Append(run.RewardsTaken);
            builder.Append(',');
            AppendFloat(builder, run.FirstRewardTime);
            builder.Append(',');
            AppendFloat(builder, run.FinalDps);
            builder.Append(',');
            AppendCsv(builder, run.RewardLog);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static void AppendFloat(StringBuilder builder, float value)
    {
        builder.Append(value.ToString("0.###", CultureInfo.InvariantCulture));
    }

    private static void AppendCsv(StringBuilder builder, string value)
    {
        value ??= string.Empty;
        builder.Append('"');
        builder.Append(value.Replace("\"", "\"\""));
        builder.Append('"');
    }
}
