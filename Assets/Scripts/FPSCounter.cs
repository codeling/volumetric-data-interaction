﻿using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    public int FPS;

    void Update()
    {
        FPS = (int)(1f / Time.deltaTime);
    }
}
