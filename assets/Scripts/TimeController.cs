using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeController : MonoBehaviour
{
    [SerializeField] private Light sun;
    [SerializeField] private float secondsFullDay = 120f;

    [Range(0, 1)] [SerializeField] private float currentTimeOfDay = 0.0f;
    private float timeMultiplier = 1f;
    private float sunInitialIntensity;


    // Start is called before the first frame update
    void Start()
    {
        sunInitialIntensity = sun.intensity;
    }


    // Update is called once per frame
    void Update()
    {
        UpdateSun();
        currentTimeOfDay += (Time.deltaTime / secondsFullDay) * timeMultiplier;

        while (currentTimeOfDay >= 1.0f)
        {
            currentTimeOfDay -= 1.0f;
        }
    }

    void UpdateSun()
    {
        sun.transform.localRotation = Quaternion.Euler((currentTimeOfDay * 360f) - 90.0f, 170.0f, 0.0f);
        float intensityMultiplier = 0.5f * (1.0f - Mathf.Cos(2 * Mathf.PI * currentTimeOfDay));

        sun.intensity = sunInitialIntensity * intensityMultiplier;
    }
}
