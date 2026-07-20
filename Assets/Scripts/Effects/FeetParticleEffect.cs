using System.Collections.Generic;
using UnityEngine;

/// <summary>Script-driven particle system using SpriteRenderer objects.
/// Emits star-shaped particles from the player's feet that rise upward.</summary>
public class FeetParticleEffect : MonoBehaviour
{
    [Header("Particle Settings")]
    [SerializeField] private int poolSize = 20;
    [SerializeField] private float particleLifetime = 0.6f;
    [SerializeField] private float riseSpeed = 2.5f;
    [SerializeField] private float startXRange = 0.15f;
    [SerializeField] private float driftX = 0.3f;
    [SerializeField] private float startSize = 1.0f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float startYOffset = -0.15f;

    [Header("Colors")]
    [SerializeField] private Color bottomColor = new Color(0.2f, 0.4f, 1f, 1f);
    [SerializeField] private Color topColor = new Color(0.7f, 0.9f, 1f, 1f);

    [Header("References")]
    [SerializeField] private Sprite particleSprite;

    private class Particle
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public float life;
        public float maxLife;
        public float driftX;
        public float rotSpeed;
        public bool active;
    }

    private readonly List<Particle> pool = new List<Particle>();
    private float emitTimer;
    private float emitInterval;

    void Start()
    {
        emitInterval = particleLifetime / poolSize;

        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject("Particle_" + i);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = particleSprite;
            sr.sortingLayerName = "玩家";
            sr.sortingOrder = 5;
            sr.color = bottomColor;
            sr.enabled = false;

            var p = new Particle
            {
                transform = go.transform,
                renderer = sr,
                life = 0f,
                maxLife = particleLifetime,
                active = false
            };
            pool.Add(p);

            // Stagger initial emissions
            emitTimer = -emitInterval * i;
        }
    }

    void Update()
    {
        emitTimer += Time.deltaTime;

        if (emitTimer >= emitInterval)
        {
            emitTimer = 0f;
            EmitParticle();
        }

        foreach (var p in pool)
        {
            if (!p.active) continue;

            p.life += Time.deltaTime;
            float t = p.life / p.maxLife;

            if (t >= 1f)
            {
                p.active = false;
                p.renderer.enabled = false;
                continue;
            }

            // Rise upward
            float yOffset = startYOffset + riseSpeed * p.life;
            p.transform.localPosition = new Vector3(
                p.driftX * p.life,
                yOffset,
                0f
            );

            // Rotation
            p.transform.localRotation = Quaternion.Euler(0, 0, p.rotSpeed * p.life);

            // Color lerp: dark blue → light blue
            Color c = Color.Lerp(bottomColor, topColor, t);

            // Alpha: fade in (0-0.1), hold, fade out (0.7-1.0)
            if (t < 0.1f)
                c.a = t / 0.1f;
            else if (t > 0.7f)
                c.a = (1f - t) / 0.3f;
            else
                c.a = 1f;

            p.renderer.color = c;

            // Scale: grow at start, shrink at end
            float scale = startSize;
            if (t < 0.15f)
                scale *= t / 0.15f;
            else if (t > 0.7f)
                scale *= (1f - t) / 0.3f;
            p.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private void EmitParticle()
    {
        foreach (var p in pool)
        {
            if (p.active) continue;

            p.active = true;
            p.life = 0f;
            p.driftX = Random.Range(-driftX, driftX);
            p.rotSpeed = Random.Range(-rotationSpeed, rotationSpeed);
            p.renderer.enabled = true;
            p.renderer.color = bottomColor;
            p.transform.localPosition = new Vector3(
                Random.Range(-startXRange, startXRange),
                startYOffset,
                0f
            );
            p.transform.localScale = Vector3.zero;
            p.transform.localRotation = Quaternion.identity;
            break;
        }
    }
}
