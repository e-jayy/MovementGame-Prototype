using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CrumblingPlatform : MonoBehaviour
{
    [Header("Timings")]
    [SerializeField] private float crumbleTime = 1f;     // Time it takes to turn black
    [SerializeField] private float respawnTime = 2f;     // Time before platform returns

    private SpriteRenderer sr;
    [SerializeField] private Tilemap tm;
    [SerializeField] private TilemapRenderer tmr;

    public GameObject spikes;
    private Collider2D col;

    private Color startColor = Color.white;
    private Color endColor = Color.black;

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

        float t = 0f;

        if (tm!= null)
        {
            tm.color = startColor;
        }
        else
        {
            sr.color = startColor;
        }

        // Fade from white → black
        while (t < crumbleTime)
        {
            t += Time.deltaTime;
            float lerp = t / crumbleTime;

            if (tm!= null)
            {
                tm.color = Color.Lerp(startColor, endColor, lerp);
            }
            else
            {
                sr.color = Color.Lerp(startColor, endColor, lerp);
            }
            yield return null;
        }

        // Disable platform
        if (tm!= null)
        {
            tmr.enabled = false;
            if(spikes != null)
            {
                spikes.SetActive(false);
            }
        }
        else
        {
            sr.enabled = false;
        }
        col.enabled = false;
        
        if (transform.childCount > 0)
        {
            foreach (Transform child in transform)
            {
                if(child.GetComponent<SpriteRenderer>() != null && child.GetComponent<Collider2D>() != null)
                {
                    child.GetComponent<SpriteRenderer>().enabled = false;
                    child.GetComponent<Collider2D>().enabled = false;
                }
            }
        }

        // Wait before respawn
        yield return new WaitForSeconds(respawnTime);

        // Reset

        if (tm!= null)
        {
            tm.color = startColor;
            tmr.enabled = true;
            if(spikes != null)
            {
                spikes.SetActive(true);
            }
        }
        else
        {
            sr.color = startColor;
            sr.enabled = true;
        }
        col.enabled = true;
        
        if (transform.childCount > 0)
        {
            foreach (Transform child in transform)
            {
                if(child.GetComponent<SpriteRenderer>() != null && child.GetComponent<Collider2D>() != null)
                {
                    child.GetComponent<SpriteRenderer>().enabled = true;
                    child.GetComponent<Collider2D>().enabled = true;
                }
            }
        }

        isCrumbing = false;
    }
}
