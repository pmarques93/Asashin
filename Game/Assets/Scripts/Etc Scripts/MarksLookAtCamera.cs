﻿using UnityEngine;

/// <summary>
/// Class responsible for making enemy marks look at the camera.
/// </summary>
public class MarksLookAtCamera : MonoBehaviour
{
    private Camera cam;

    private void Awake() =>
        cam = Camera.main;

    private void FixedUpdate() =>
        transform.LookAt(cam.transform);
}
