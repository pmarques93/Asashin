﻿using UnityEngine;

/// <summary>
/// Class responsible for handling player movement.
/// </summary>
public class PlayerMovement : MonoBehaviour, IAction
{
    // Components
    private CharacterController controller;
    private PlayerInputCustom input;
    private Transform mainCamera;

    // Movement Variables
    [SerializeField] private float speed = 5f;
    public Vector3 Direction { get; private set; }
    private float hVel;
    private float vVel;
    private float smoothTimeVelocity;
    private Vector3 moveDirection;
    public bool CanMove { get; set; }

    // Rotation Variables
    private float turnSmooth = 0.1f;
    private float smoothTimeRotation;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputCustom>();
        mainCamera = Camera.main.transform;
    }

    private void Start()
    {
        CanMove = true;
        smoothTimeVelocity = 0.025f;
    }

    
    public void ComponentUpdate()
    {
        // Updates movement direction variable
        if (CanMove)
        {
            Direction = new Vector3(
                Mathf.SmoothDamp(Direction.x, input.Movement.x, ref hVel, smoothTimeVelocity), 
                0f, 
                Mathf.SmoothDamp(Direction.z, input.Movement.y, ref vVel, smoothTimeVelocity));
        }
        else
        {
            Direction = Vector3.zero;
        }
    }

    public void ComponentFixedUpdate()
    {
        Movement();
        Rotation();
    }

    /// <summary>
    /// Handles movement.
    /// </summary>
    private void Movement()
    {
        if (Direction.magnitude > 0.01f)
        {
            // Moves controllers towards the moveDirection set on Rotation()
            controller.Move(moveDirection.normalized * speed * Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// Handles rotation.
    /// </summary>
    private void Rotation()
    {
        if (Direction.magnitude > 0.01f)
        {
            // Finds out angle
            float targetAngle = Mathf.Atan2(Direction.x, Direction.z) * 
                Mathf.Rad2Deg + mainCamera.eulerAngles.y;

            float angle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y, targetAngle, ref smoothTimeRotation, turnSmooth);

            // Rotates to that angle
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // Sets moving Direction
            moveDirection = Quaternion.Euler(0f, targetAngle, 0f) *
                Vector3.forward;
        }
    }
}