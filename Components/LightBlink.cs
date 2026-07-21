using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayhousePlugin.Components;

public sealed class LightBlink : MonoBehaviour
{
    public float MinFlickerTime = 0.1f;
    public float MaxFlickerTime = 1.5f;
    public float TurnOnSpeed = 0.3f;
    public float MaximumIntensityDecrease = 0.5f;
    public float Offset;

    public static List<LightBlink> Instances { get; } = new();

    private Light? targetLight;
    private float nextFlicker;
    private bool isForcedFlickering;

    private void Awake()
    {
        targetLight = GetComponent<Light>();

        if (targetLight is null)
        {
            Destroy(this);
            return;
        }

        Instances.Add(this);
        nextFlicker = Random.Range(MinFlickerTime, MaxFlickerTime);
    }

    private void OnDestroy()
    {
        Instances.Remove(this);
    }

    private void FixedUpdate()
    {
        if (targetLight is null || isForcedFlickering)
            return;

        nextFlicker -= Time.fixedDeltaTime;

        if (nextFlicker <= 0f)
        {
            nextFlicker = Random.Range(MinFlickerTime, MaxFlickerTime);

            targetLight.intensity = Mathf.Max(
                targetLight.intensity
                + Offset
                - Random.Range(0f, MaximumIntensityDecrease),
                0f);
        }

        targetLight.intensity = Mathf.Lerp(
            targetLight.intensity,
            1f + Offset,
            TurnOnSpeed);
    }

    public void ForceFlicker(float duration)
    {
        if (targetLight is null || !gameObject.activeInHierarchy)
            return;

        StopAllCoroutines();
        StartCoroutine(ForcedFlicker(duration));
    }

    private IEnumerator ForcedFlicker(float duration)
    {
        if (targetLight is null)
            yield break;

        isForcedFlickering = true;

        float originalIntensity = targetLight.intensity;
        float endTime = Time.time + duration;

        while (Time.time < endTime)
        {
            targetLight.intensity = Random.Range(0f, 1f + Offset);

            yield return new WaitForSeconds(
                Random.Range(0.03f, 0.12f));
        }

        targetLight.intensity = Mathf.Max(originalIntensity, 1f + Offset);
        nextFlicker = Random.Range(MinFlickerTime, MaxFlickerTime);
        isForcedFlickering = false;
    }
}