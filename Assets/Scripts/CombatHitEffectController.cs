using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Converts each combat result event into one result-specific visual effect.
/// </summary>
[DefaultExecutionOrder(-900)]
[DisallowMultipleComponent]
[RequireComponent(typeof(MasterController))]
public sealed class CombatHitEffectController : MonoBehaviour
{
    [Header("Particle Presentation")]
    [SerializeField] private Material particleMaterial;
    [SerializeField, Min(0.05f)] private float effectLifetime = 0.32f;
    [SerializeField, Min(0.01f)] private float baseSize = 0.28f;
    [SerializeField, Range(1, 64)] private int sparkCount = 20;
    [SerializeField, Min(0f)] private float lightIntensity = 3.5f;

    [Header("Outcome Colors")]
    [SerializeField] private Color normalHitColor = new Color(1f, 0.78f, 0.08f, 1f);
    [SerializeField] private Color normalHitSecondary = Color.white;
    [SerializeField] private Color guardSuccessColor = new Color(0.08f, 0.48f, 1f, 1f);
    [SerializeField] private Color guardSuccessSecondary = new Color(0.75f, 0.92f, 1f, 1f);
    [SerializeField] private Color guardFailureColor = new Color(1f, 0.08f, 0.02f, 1f);
    [SerializeField] private Color guardFailureSecondary = new Color(1f, 0.48f, 0.04f, 1f);

    private MasterController masterController;
    private Transform effectRoot;
    private Material runtimeParticleMaterial;
    private Texture2D runtimeParticleTexture;
    private bool hasHandledSequence;
    private uint lastHandledSequence;

    public event Action<CombatImpact> EffectSpawned;

    public int ActiveEffectCount { get; private set; }

    private void Awake()
    {
        masterController = GetComponent<MasterController>();
    }

    private void OnEnable()
    {
        if (masterController == null)
        {
            masterController = GetComponent<MasterController>();
        }

        masterController.CombatImpactResolved += SpawnEffect;
    }

    private void OnDisable()
    {
        if (masterController != null)
        {
            masterController.CombatImpactResolved -= SpawnEffect;
        }
    }

    private void OnDestroy()
    {
        if (effectRoot != null)
        {
            Destroy(effectRoot.gameObject);
        }
        if (runtimeParticleMaterial != null)
        {
            Destroy(runtimeParticleMaterial);
        }
        if (runtimeParticleTexture != null)
        {
            Destroy(runtimeParticleTexture);
        }
    }

    /// <summary>
    /// Creates exactly one effect for an impact sequence. This public boundary
    /// also makes it possible to replace creation with an object pool later.
    /// </summary>
    public void SpawnEffect(CombatImpact impact)
    {
        if (hasHandledSequence && impact.Sequence == lastHandledSequence)
        {
            return;
        }

        hasHandledSequence = true;
        lastHandledSequence = impact.Sequence;

        GetOutcomeColors(impact.Outcome, out Color primaryColor, out Color secondaryColor);
        float strengthScale = Mathf.Lerp(0.85f, 1.15f, Mathf.Clamp01(impact.Strength / 3f));

        GameObject effectObject = new GameObject($"Combat Hit Effect {impact.Sequence}");
        effectObject.transform.SetParent(EnsureEffectRoot(), false);
        CombatHitEffect effect = effectObject.AddComponent<CombatHitEffect>();
        effect.Completed += ReleaseEffect;
        effect.Initialize(
            impact,
            ResolveParticleMaterial(),
            primaryColor,
            secondaryColor,
            effectLifetime,
            baseSize * strengthScale,
            sparkCount,
            lightIntensity * strengthScale);

        ActiveEffectCount++;
        EffectSpawned?.Invoke(impact);
    }

    private Transform EnsureEffectRoot()
    {
        if (effectRoot == null)
        {
            GameObject rootObject = new GameObject("Combat Hit Effects");
            effectRoot = rootObject.transform;
        }

        return effectRoot;
    }

    private Material ResolveParticleMaterial()
    {
        if (runtimeParticleMaterial != null)
        {
            return runtimeParticleMaterial;
        }

        if (particleMaterial != null)
        {
            runtimeParticleMaterial = new Material(particleMaterial);
        }
        else
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }
            if (shader == null)
            {
                Debug.LogError("No supported particle shader was found for combat hit effects.", this);
                return null;
            }

            runtimeParticleMaterial = new Material(shader);
        }

        runtimeParticleMaterial.name = "Runtime Combat Hit Particle Material";
        runtimeParticleMaterial.renderQueue = (int)RenderQueue.Transparent;
        if (runtimeParticleMaterial.HasProperty("_BaseColor"))
        {
            runtimeParticleMaterial.SetColor("_BaseColor", Color.white);
        }
        runtimeParticleTexture = CreateSoftParticleTexture();
        if (runtimeParticleMaterial.HasProperty("_BaseMap"))
        {
            runtimeParticleMaterial.SetTexture("_BaseMap", runtimeParticleTexture);
        }
        if (runtimeParticleMaterial.HasProperty("_MainTex"))
        {
            runtimeParticleMaterial.SetTexture("_MainTex", runtimeParticleTexture);
        }
        if (runtimeParticleMaterial.HasProperty("_Surface"))
        {
            runtimeParticleMaterial.SetFloat("_Surface", 1f);
        }
        if (runtimeParticleMaterial.HasProperty("_Blend"))
        {
            runtimeParticleMaterial.SetFloat("_Blend", 2f);
        }
        if (runtimeParticleMaterial.HasProperty("_SrcBlend"))
        {
            runtimeParticleMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        }
        if (runtimeParticleMaterial.HasProperty("_DstBlend"))
        {
            runtimeParticleMaterial.SetFloat("_DstBlend", (float)BlendMode.One);
        }
        if (runtimeParticleMaterial.HasProperty("_ZWrite"))
        {
            runtimeParticleMaterial.SetFloat("_ZWrite", 0f);
        }
        runtimeParticleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        return runtimeParticleMaterial;
    }

    private static Texture2D CreateSoftParticleTexture()
    {
        const int textureSize = 32;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            name = "Runtime Soft Combat Particle",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[textureSize * textureSize];
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float normalizedX = (x + 0.5f) / textureSize * 2f - 1f;
                float normalizedY = (y + 0.5f) / textureSize * 2f - 1f;
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(1f - new Vector2(normalizedX, normalizedY).magnitude));
                pixels[y * textureSize + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private void ReleaseEffect(CombatHitEffect effect)
    {
        effect.Completed -= ReleaseEffect;
        ActiveEffectCount = Mathf.Max(0, ActiveEffectCount - 1);
        Destroy(effect.gameObject);
    }

    private void GetOutcomeColors(CombatImpactOutcome outcome, out Color primary, out Color secondary)
    {
        switch (outcome)
        {
            case CombatImpactOutcome.NormalHit:
                primary = normalHitColor;
                secondary = normalHitSecondary;
                break;
            case CombatImpactOutcome.GuardSuccess:
                primary = guardSuccessColor;
                secondary = guardSuccessSecondary;
                break;
            case CombatImpactOutcome.GuardFailure:
                primary = guardFailureColor;
                secondary = guardFailureSecondary;
                break;
            default:
                primary = Color.white;
                secondary = Color.white;
                break;
        }
    }
}
