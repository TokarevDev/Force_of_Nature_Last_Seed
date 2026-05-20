using UnityEngine;

[DisallowMultipleComponent]
public sealed class CocoonVisualController : MonoBehaviour
{
    private static readonly int BaseTintId = Shader.PropertyToID("_BaseTint");
    private static readonly int WormAccentColorId = Shader.PropertyToID("_WormAccentColor");
    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int GlowStrengthId = Shader.PropertyToID("_GlowStrength");
    private static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");
    private static readonly int PulseAmountId = Shader.PropertyToID("_PulseAmount");
    private static readonly int PearlStrengthId = Shader.PropertyToID("_PearlStrength");
    private static readonly int AccentStrengthId = Shader.PropertyToID("_AccentStrength");
    private static readonly int RimStrengthId = Shader.PropertyToID("_RimStrength");
    private static readonly int LegendaryBlendId = Shader.PropertyToID("_LegendaryBlend");

    [Header("Refs")]
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private ParticleSystem _legendarySparks;
    [SerializeField] private CocoonLegendaryLightningEffect _legendaryLightning;

    [Header("Normal Cocoon")]
    [SerializeField] private Color _baseTint = new(0.82f, 0.96f, 0.98f, 1f);
    [SerializeField] private Color _wormAccentColor = new(0f, 0.62f, 0.72f, 1f);
    [SerializeField] private Color _glowColor = new(0.58f, 1f, 1f, 1f);
    [SerializeField, Range(0f, 1f)] private float _glowStrength = 0.08f;
    [SerializeField, Min(0f)] private float _pulseSpeed = 1.7f;
    [SerializeField, Range(0f, 0.5f)] private float _pulseAmount = 0.05f;
    [SerializeField, Range(0f, 1f)] private float _pearlStrength = 0.24f;
    [SerializeField, Range(0f, 1f)] private float _accentStrength = 0.58f;
    [SerializeField, Range(0f, 1f)] private float _rimStrength = 0.78f;

    [Header("Legendary Cocoon")]
    [SerializeField] private Color _legendaryGlowColor = new(1f, 0.48f, 0.08f, 1f);
    [SerializeField] private Color _legendarySparkColor = new(1f, 0.58f, 0.08f, 1f);
    [SerializeField, Range(0f, 2f)] private float _legendaryGlowStrength = 0.72f;
    [SerializeField, Min(0f)] private float _legendaryPulseSpeed = 4f;
    [SerializeField, Range(0f, 0.75f)] private float _legendaryPulseAmount = 0.24f;
    [SerializeField, Range(0f, 1f)] private float _legendaryAccentStrength = 0.18f;

    private MaterialPropertyBlock _propertyBlock;
    private bool _hasEffectSorting;
    private int _effectSortingLayerId;
    private int _effectSortingOrder;

    private void Reset()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        CacheReferences();
        ApplyNormalVisual();
        SetLegendaryEffectsActive(false);
    }

    private void OnDisable()
    {
        SetLegendaryEffectsActive(false);
    }

    public void Apply(CocoonRewardProfile profile)
    {
        bool useLegendaryVisual = profile != null && profile.UsesLegendaryCocoonVisual;

        if (useLegendaryVisual)
            ApplyLegendaryVisual();
        else
            ApplyNormalVisual();

        SetLegendaryEffectsActive(useLegendaryVisual);
    }

    public void ResetVisual()
    {
        ApplyNormalVisual();
        SetLegendaryEffectsActive(false);
    }

    public void SetEffectSorting(int sortingLayerId, int sortingOrder)
    {
        _hasEffectSorting = true;
        _effectSortingLayerId = sortingLayerId;
        _effectSortingOrder = sortingOrder;

        ApplyEffectSorting();
    }

    private void ApplyNormalVisual()
    {
        ApplyShaderProperties(
            _baseTint,
            _wormAccentColor,
            _glowColor,
            _glowStrength,
            _pulseSpeed,
            _pulseAmount,
            _pearlStrength,
            _accentStrength,
            _rimStrength,
            0f);
    }

    private void ApplyLegendaryVisual()
    {
        ApplyShaderProperties(
            _baseTint,
            _legendaryGlowColor,
            _legendaryGlowColor,
            _legendaryGlowStrength,
            _legendaryPulseSpeed,
            _legendaryPulseAmount,
            _pearlStrength,
            _legendaryAccentStrength,
            _rimStrength,
            1f);
    }

    private void ApplyShaderProperties(
        Color baseTint,
        Color accentColor,
        Color glowColor,
        float glowStrength,
        float pulseSpeed,
        float pulseAmount,
        float pearlStrength,
        float accentStrength,
        float rimStrength,
        float legendaryBlend)
    {
        if (_renderer == null)
            return;

        _propertyBlock ??= new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(BaseTintId, baseTint);
        _propertyBlock.SetColor(WormAccentColorId, accentColor);
        _propertyBlock.SetColor(GlowColorId, glowColor);
        _propertyBlock.SetFloat(GlowStrengthId, glowStrength);
        _propertyBlock.SetFloat(PulseSpeedId, pulseSpeed);
        _propertyBlock.SetFloat(PulseAmountId, pulseAmount);
        _propertyBlock.SetFloat(PearlStrengthId, pearlStrength);
        _propertyBlock.SetFloat(AccentStrengthId, accentStrength);
        _propertyBlock.SetFloat(RimStrengthId, rimStrength);
        _propertyBlock.SetFloat(LegendaryBlendId, legendaryBlend);
        _renderer.SetPropertyBlock(_propertyBlock);
        _renderer.color = Color.white;
    }

    private void SetLegendaryEffectsActive(bool active)
    {
        if (active)
            EnsureLegendaryEffects();

        SetParticleActive(_legendarySparks, active);

        if (_legendaryLightning != null)
            _legendaryLightning.SetActive(active);
    }

    private void EnsureLegendaryEffects()
    {
        if (_legendarySparks == null)
            _legendarySparks = CreateLegendarySparks();

        if (_legendaryLightning == null)
            _legendaryLightning = CreateLegendaryLightning();

        ApplyEffectSorting();
    }

    private ParticleSystem CreateLegendarySparks()
    {
        GameObject sparksObject = new("LegendarySparks");
        sparksObject.transform.SetParent(transform, false);

        ParticleSystem particles = sparksObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 1f;
        main.startDelay = new ParticleSystem.MinMaxCurve(0f, 0.2f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.075f);
        main.startColor = _legendarySparkColor;
        main.maxParticles = 18;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 8f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.42f;
        shape.radiusThickness = 0.2f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = CreateSparkGradient(_legendarySparkColor);

        ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;

        sparksObject.SetActive(false);
        return particles;
    }

    private CocoonLegendaryLightningEffect CreateLegendaryLightning()
    {
        GameObject lightningObject = new("LegendaryLightning");
        lightningObject.transform.SetParent(transform, false);

        CocoonLegendaryLightningEffect effect =
            lightningObject.AddComponent<CocoonLegendaryLightningEffect>();
        effect.Configure(_legendarySparkColor);

        lightningObject.SetActive(false);
        return effect;
    }

    private void ApplyEffectSorting()
    {
        if (!_hasEffectSorting)
            return;

        if (_legendarySparks != null)
        {
            ParticleSystemRenderer particleRenderer =
                _legendarySparks.GetComponent<ParticleSystemRenderer>();

            if (particleRenderer != null)
            {
                particleRenderer.sortingLayerID = _effectSortingLayerId;
                particleRenderer.sortingOrder = _effectSortingOrder;
            }
        }

        if (_legendaryLightning != null)
            _legendaryLightning.SetSorting(_effectSortingLayerId, _effectSortingOrder + 1);
    }

    private void CacheReferences()
    {
        if (_renderer == null)
            _renderer = GetComponent<SpriteRenderer>();

        if (_legendarySparks == null)
            _legendarySparks = GetComponentInChildren<ParticleSystem>(true);

        if (_legendaryLightning == null)
            _legendaryLightning = GetComponentInChildren<CocoonLegendaryLightningEffect>(true);
    }

    private static void SetParticleActive(ParticleSystem particles, bool active)
    {
        if (particles == null)
            return;

        GameObject particlesObject = particles.gameObject;

        if (active)
        {
            if (!particlesObject.activeSelf)
                particlesObject.SetActive(true);

            if (!particles.isPlaying)
                particles.Play();
        }
        else
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (particlesObject.activeSelf)
                particlesObject.SetActive(false);
        }
    }

    private static Gradient CreateSparkGradient(Color color)
    {
        Gradient gradient = new();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(color, 0.35f),
                new GradientColorKey(color, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.15f),
                new GradientAlphaKey(0f, 1f)
            });

        return gradient;
    }
}
