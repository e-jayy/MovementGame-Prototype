using UnityEngine;
using UnityEngine.Tilemaps;

public class CrumblingPlatform : MonoBehaviour
{
    [Header("Timings")]
    [SerializeField] private float crumbleTime = 1f;
    [SerializeField] private float respawnTime = 2f;

    [Header("Particles")]
    [SerializeField] private ParticleSystem rubbleParticles;
    [SerializeField] private ParticleSystem respawnParticles;
    [SerializeField] private float respawnParticlesMinRate = 1f;
    [SerializeField] private float respawnParticlesMaxRate = 50f;

    [SerializeField] private Tilemap tm;
    [SerializeField] private TilemapRenderer tmr;

    public GameObject spikes;
    private Collider2D col;
    private SpriteRenderer sr;

    private bool isCrumbing = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isCrumbing && collision.collider.CompareTag("Player"))
        {
            StartCoroutine(CrumbleRoutine());
        }
    }

    private System.Collections.IEnumerator CrumbleRoutine()
    {
        isCrumbing = true;

        if (rubbleParticles != null)
            rubbleParticles.Play();

        yield return new WaitForSeconds(crumbleTime);

        // Disable platform
        if (tm != null)
        {
            tmr.enabled = false;
            if (spikes != null) spikes.SetActive(false);
        }
        else
        {
            if (sr != null) sr.enabled = false;
        }
        col.enabled = false;

        if (transform.childCount > 0)
        {
            foreach (Transform child in transform)
            {
                if (child.GetComponent<SpriteRenderer>() != null && child.GetComponent<Collider2D>() != null)
                {
                    child.GetComponent<SpriteRenderer>().enabled = false;
                    child.GetComponent<Collider2D>().enabled = false;
                }
            }
        }

        if (rubbleParticles != null)
            rubbleParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Ramp up respawn particles over respawnTime
        if (respawnParticles != null)
        {
            respawnParticles.Play();

            float t = 0f;
            while (t < respawnTime)
            {
                t += Time.deltaTime;
                float lerp = t / respawnTime;

                // Ramp emission rate from min to max
                var emission = respawnParticles.emission;
                emission.rateOverTime = Mathf.Lerp(respawnParticlesMinRate, respawnParticlesMaxRate, lerp);

                yield return null;
            }

            respawnParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        else
        {
            yield return new WaitForSeconds(respawnTime);
        }

        // Reset
        if (tm != null)
        {
            tmr.enabled = true;
            if (spikes != null) spikes.SetActive(true);
        }
        else
        {
            if (sr != null) sr.enabled = true;
        }
        col.enabled = true;

        if (transform.childCount > 0)
        {
            foreach (Transform child in transform)
            {
                if (child.GetComponent<SpriteRenderer>() != null && child.GetComponent<Collider2D>() != null)
                {
                    child.GetComponent<SpriteRenderer>().enabled = true;
                    child.GetComponent<Collider2D>().enabled = true;
                }
            }
        }

        isCrumbing = false;
    }
}