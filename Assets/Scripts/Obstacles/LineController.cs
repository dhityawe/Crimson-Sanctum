using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineController : MonoBehaviour
{
    private LineRenderer lineRenderer;

    [SerializeField]
    private Texture[] textures;

    private int animationStep;

    [SerializeField]
    private float fps = 30f;
    private float fpsCounter;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        
        // Set up LineRenderer for 2 points (start and end)
        lineRenderer.positionCount = 5;
    }
    private void Update()
    {
        // Update line positions if start and end points are assigned
        // UpdateLinePositions();
        
        // Handle texture animation
        fpsCounter += Time.deltaTime;
        if (fpsCounter >= 1f / fps)
        {
            animationStep++;
            if (animationStep >= textures.Length)
            {
                animationStep = 0;
            }

            lineRenderer.material.SetTexture("_MainTex", textures[animationStep]);

            fpsCounter = 0f;
        }
    }

    // private void UpdateLinePositions()
    // {
    //     if (start != null && end != null)
    //     {
    //         lineRenderer.SetPosition(0, start.position);
    //         lineRenderer.SetPosition(1, end.position);
    //     }
    // }

}
