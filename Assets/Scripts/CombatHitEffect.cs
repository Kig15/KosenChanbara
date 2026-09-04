using System;
using UnityEngine;

/// <summary>
/// Runtime presentation for one resolved combat impact. The controller owns
/// creation and release so this instance can later be replaced by a pooled one.
/// </summary>
public sealed class CombatHitEffect : MonoBehaviour
{
    private ParticleSystem flashParticles;
    private ParticleSystem sparkParticles;
    private Light impactLight;
    private float remainingLifetime;
    private float initialLightIntensity;
    private float totalLifetime;
    private bool initialized;
    private bool completionRaised;

    public event Action<CombatHitEffect> Completed;

    public uint Sequence { get; private set; }
    public CombatImpactOutcome Outcome { get; private set; }
    public Color PrimaryColor { get; private set; }
    public Color SecondaryColor { get; private set; }

    public void Initialize(
        CombatImpact impact,
        Material particleMaterial,
        Color primaryColor,
        Color secondaryColor,
        float lifetime,
        float size,
        int sparkCount,
        float lightIntensity)
    {
        Sequence = impact.Sequence;
        Outcome = impact.Outcome;
        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor;

        Vector3 direction = impact.Direction.sqrMagnitude > Mathf.Epsilon
            ? impact.Direction.normalized
            : Vector3.forward;
        Vector3 up = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.98f
            ? Vector3.right
            : Vector3.up;
        transform.SetPositionAndRotation(impact.WorldPosition, Quaternion.LookRotation(direction, up));

        totalLifetime = Mathf.Max(0.05f, lifetime);
        remainingLifetime = totalLifetime;
        initialLightIntensity = Mathf.Max(0f, lightIntensity);

        flashParticles = CreateParticleSystem("Impact Flash", particleMaterial);
        sparkParticles = CreateParticleSystem("Impact Sparks", particleMaterial);
        float presentationOffset = impact.Outcome == CombatImpactOutcome.NormalHit ? size * 1.6f : 0f;
        Vector3 localPresentationPosition = Vector3.back * presentationOffset;
        flashParticles.transform.localPosition = localPresentationPosition;
        sparkParticles.transform.localPosition = localPresentationPosition;
        ConfigureFlash(flashParticles, secondaryColor, totalLifetime, size);
        ConfigureSparks(sparkParticles, primaryColor, secondaryColor, totalLifetime, size, sparkCount);

        impactLight = gameObject.AddComponent<Light>();
        impactLight.type = LightType.Point;
        impactLight.color = primaryColor;
        impactLight.intensity = initialLightIntensity;
        impactLight.range = Mathf.Max(0.5f, size * 8f);
        impactLight.shadows = LightShadows.None;

        initialized = true;
        flashParticles.Play(true);
        sparkParticles.Play(true);
    }

    private void Update()
    {
        if (!initialized || completionRaised)
        {
            return;
        }

        remainingLifetime -= Time.unscaledDeltaTime;
        if (impactLight != null)
        {
            float normalized = Mathf.Clamp01(remainingLifetime / totalLifetime);
            impactLight.intensity = initialLightIntensity * normalized * normalized;
        }

        if (remainingLifetime <= 0f)
        {
            completionRaised = true;
            Completed?.Invoke(this);
        }
    }

    private ParticleSystem CreateParticleSystem(string objectName, Material material)
    {
        GameObject particleObject = new GameObject(objectName);
        particleObject.transform.SetParent(transform, false);
        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
        // AddComponent starts a ParticleSystem immediately on an active object.
        // Stop it before changing duration and other settings that Unity locks
        // while a system is playing.
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortingFudge = 1f;
        if (material != null)
        {
            renderer.sharedMaterial = material;
        }

        return particles;
    }

    private static void ConfigureFlash(ParticleSystem particles, Color color, float lifetime, float size)
    {
        ParticleSystem.MainModule main = particles.main;
        main.duration = Mathf.Max(0.05f, lifetime * 0.25f);
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.useUnscaledTime = true;
        main.maxParticles = 1;
        main.startLifetime = lifetime * 0.45f;
        main.startSpeed = 0f;
        main.startSize = size * 2.8f;
        main.startColor = color;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = false;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(0.2f, 1f),
                new Keyframe(1f, 0f)));

        ApplyFade(particles);
    }

    private static void ConfigureSparks(
        ParticleSystem particles,
        Color primaryColor,
        Color secondaryColor,
        float lifetime,
        float size,
        int sparkCount)
    {
        ParticleSystem.MainModule main = particles.main;
        main.duration = Mathf.Max(0.05f, lifetime * 0.25f);
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.useUnscaledTime = true;
        main.maxParticles = Mathf.Max(1, sparkCount);
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.55f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(size * 8f, size * 16f);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.14f, size * 0.38f);
        main.startColor = new ParticleSystem.MinMaxGradient(primaryColor, secondaryColor);
        main.gravityModifier = 0.25f;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)Mathf.Clamp(sparkCount, 1, short.MaxValue))
        });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = size * 0.12f;
        shape.radiusThickness = 0f;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.65f, 0.65f),
                new Keyframe(1f, 0f)));

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = size * 1.5f;
        noise.frequency = 1.2f;
        noise.scrollSpeed = 0.8f;
        noise.damping = true;

        ApplyFade(particles);
    }

    private static void ApplyFade(ParticleSystem particles)
    {
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.85f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            });

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);
    }
}
