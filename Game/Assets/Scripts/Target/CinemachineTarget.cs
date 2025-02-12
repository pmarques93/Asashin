﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cinemachine;

public class CinemachineTarget : MonoBehaviour, IFindPlayer, IUpdateOptions
{
    [SerializeField] private bool inTutorial;

    [SerializeField] private OptionsScriptableObj configScriptableObj;

    // Components
    private Player player;
    private PlayerWallHug playerWallHug;
    private PlayerInputCustom input;
    private PauseSystem pauseSystem;
    private SlowMotionBehaviour slowMotion;
    private Options optionsScript;
    private BlackBordersFadeIn blackBordersFadeIn;

    // Camera Variables
    [SerializeField] private CinemachineFreeLook thirdPersonCamera;
    [SerializeField] private CinemachineVirtualCamera targetCamera;
    [SerializeField] private CinemachineVirtualCamera pauseMenuCamera;
    [SerializeField] private CinemachineVirtualCamera sceneChangerCamera;
    [SerializeField] private CinemachineBrain mainCameraBrain;

    // Current target from player
    [SerializeField] private Transform currentTarget;
    public Transform CurrentTarget => currentTarget;

    // Player targets
    private Transform playerTarget;
    private Transform playerTargetForCinemachine;

    // Target variables
    [SerializeField] private float findTargetSize;
    public float FindTargetSize => findTargetSize;
    public bool Targeting { get; private set; }
    private Vector3 targetYOffset;

    // Enemies
    private Collider[] enemies;
    private IList<EnemyBase> allEnemies;
    [SerializeField] private LayerMask collisionLayers;

    // Layers
    [SerializeField] private LayerMask enemyLayer;

    // Coroutine
    private Coroutine blendingCoroutine;
    private Coroutine isLerpingTargetCoroutine;

    private void Awake()
    {
        player = FindObjectOfType<Player>();
        input = FindObjectOfType<PlayerInputCustom>();
        pauseSystem = FindObjectOfType<PauseSystem>();
        optionsScript = FindObjectOfType<Options>();
        slowMotion = FindObjectOfType<SlowMotionBehaviour>();
        blackBordersFadeIn = FindObjectOfType<BlackBordersFadeIn>();
    }

    private void Start()
    {
        Targeting = false;
        targetYOffset = new Vector3(0, 1, 0);
        blendingCoroutine = null;
        isLerpingTargetCoroutine = null;

        allEnemies = new List<EnemyBase>();

        // Disables current player's target
        currentTarget.gameObject.SetActive(false);

        // Sets all cameras follows and lookAts.
        SetAllCamerasTargets();

        StartCoroutine(KeepsFindingClosestTarget());
        
        UpdateValues();
    }

    /// <summary>
    /// Every x seconds, trieds to find an enemy, so the current target
    /// position is always updated.
    /// </summary>
    /// <returns></returns>
    private IEnumerator KeepsFindingClosestTarget()
    {
        YieldInstruction wfs = new WaitForSeconds(0.25f);

        while(true)
        {
            FindAllEnemiesAroundPlayer(findTargetSize);

            // If there are enemies around and the camera is not blending
            if (allEnemies.Count > 0 && 
                currentTarget.gameObject.activeSelf == false &&
                mainCameraBrain.IsBlending == false)
            {
                EnemyBase[] organizedEnemiesByDistance =
                            allEnemies.OrderBy(i =>
                            (i.transform.position - player.transform.position).magnitude).
                            Where(i => i.MyTarget.CanSee(playerTarget, collisionLayers))
                            .ToArray();

                // Sets current target to closest enemy
                currentTarget.transform.position =
                    organizedEnemiesByDistance[0].transform.position + targetYOffset;
            }
            
            yield return wfs;
        }
    }

    private void OnEnable()
    {
        input.TargetSet += HandleTarget;
        input.TargetChange += SwitchTarget;
        pauseSystem.GamePaused += SwitchBetweenPauseCamera;
        slowMotion.SlowMotionEvent += SlowMotionCamera;
        optionsScript.UpdatedValues += UpdateValues;

        if (blackBordersFadeIn != null)
            blackBordersFadeIn.EnteredArea += SwitchToSceneChangerCamera;
    }

    private void OnDisable()
    {
        input.TargetSet -= HandleTarget;
        input.TargetChange -= SwitchTarget;
        pauseSystem.GamePaused -= SwitchBetweenPauseCamera;
        slowMotion.SlowMotionEvent -= SlowMotionCamera;
        optionsScript.UpdatedValues -= UpdateValues;

        if (blackBordersFadeIn != null)
            blackBordersFadeIn.EnteredArea -= SwitchToSceneChangerCamera;
    }

    /// <summary>
    /// Sets current target on current targeted enemy.
    /// </summary>
    private void FixedUpdate()
    {
        if (FindCurrentTargetedEnemy() != null && isLerpingTargetCoroutine == null)
        {
            currentTarget.position =
                FindCurrentTargetedEnemy().transform.position + targetYOffset;
        }
    }

    private void Update()
    {
        if (Targeting)
        {
            // If distance becames too wide, it cancels the current target
            // (uses findTargetSize + safe distance to prevent mistakes on calculations)
            if (Vector3.Distance(player.transform.position, currentTarget.transform.position) >
                findTargetSize + 1f)
            {
                CancelCurrentTarget();
            }

            // If currentTarget is not moving and the enemy can't see the player
            if (isLerpingTargetCoroutine == null && 
                FindCurrentTargetedEnemy()?.transform.CanSee(
                    playerTarget, collisionLayers) == false)
            {
                CancelCurrentTarget();
            }
        }

        // While the camera is blending, the player can't move the camera
        if (mainCameraBrain.IsBlending)
        {
            if (blendingCoroutine == null)
            {
                blendingCoroutine = StartCoroutine(ChangeValuesOnBlending());
            }
        }
    }

    /// <summary>
    /// Stops camera movement while blending cameras.
    /// </summary>
    /// <returns></returns>
    private IEnumerator ChangeValuesOnBlending()
    {
        yield return new WaitForEndOfFrame(); // Don't remove <<
        while (mainCameraBrain.IsBlending)
        {
            thirdPersonCamera.m_YAxis.m_MaxSpeed = 0;
            thirdPersonCamera.m_XAxis.m_MaxSpeed = 0;
            yield return null;
        }
        blendingCoroutine = null;
        UpdateValues();
    }

    /// <summary>
    /// If the player isnt targetting any enemy, finds the closest enemy 
    /// when the player presses the assigned key. Else it cancels the current
    /// target 
    /// </summary>
    public void HandleTarget()
    {
        if (mainCameraBrain.IsBlending == false && 
            playerWallHug.Performing == false)
        {
            mainCameraBrain.m_DefaultBlend.m_Time = 0.5f;
            if (Targeting == false)
            {
                FindAllEnemiesAroundPlayer(findTargetSize);
                if (allEnemies.Count > 0)
                {
                    // Orders array with all VISIBLE enemies by distance
                    EnemyBase[] organizedEnemiesByDistance =
                        allEnemies.OrderBy(i =>
                        (i.transform.position - player.transform.position).magnitude).
                        Where(i => i.MyTarget.CanSee(playerTarget, collisionLayers))
                        .ToArray();

                    currentTarget.gameObject.SetActive(true);

                    // Moves the current target towards the desired target
                    if (isLerpingTargetCoroutine == null)
                        isLerpingTargetCoroutine = StartCoroutine(
                            LerpingTargetToClosestTarget(organizedEnemiesByDistance[0]));

                    // Switches camera
                    targetCamera.Priority = thirdPersonCamera.Priority + 3;
                    UpdateTargetCameraLookAt();
                    FindCurrentTargetedEnemy();

                    if (inTutorial)
                        OnTutorialTarget(TypeOfTutorial.Target);

                    Targeting = true;
                }
            }
            else
            {
                CancelCurrentTarget();
            }
        }
    }

    /// <summary>
    /// Switches to target on the left or right.
    /// </summary>
    public void SwitchTarget(Direction leftOrRight)
    {
        EnemyBase definitiveTarget = default;
        float shortestDistance = Mathf.Infinity;

        FindAllEnemiesAroundPlayer(findTargetSize);

        for (int i = 0; i < allEnemies.Count; i++)
        {
            Vector3 direction = targetCamera.transform.Direction(allEnemies[i].transform);
            float directionAngle = MathCustom.AngleDirection(targetCamera.transform.forward, direction, transform.up);

            float distanceFromTarget =
                Vector3.Distance(currentTarget.transform.position, allEnemies[i].transform.position + targetYOffset);

            if (leftOrRight == Direction.Left)
            {
                if (directionAngle < 0 && distanceFromTarget < shortestDistance)
                {
                    if (allEnemies[i].gameObject != FindCurrentTargetedEnemy())
                    {
                        if (allEnemies[i].MyTarget.CanSee(playerTarget, collisionLayers))
                        {
                            shortestDistance = distanceFromTarget;

                            definitiveTarget = allEnemies[i];

                            if (inTutorial)
                                OnTutorialTargetLeft(TypeOfTutorial.TargetLeft);
                        }
                    }
                }
            }
            else if (leftOrRight == Direction.Right)
            {
                if (directionAngle > 0 && distanceFromTarget < shortestDistance)
                {
                    if (allEnemies[i].gameObject != FindCurrentTargetedEnemy())
                    {
                        if (allEnemies[i].MyTarget.CanSee(playerTarget, collisionLayers))
                        {
                            shortestDistance = distanceFromTarget;

                            definitiveTarget = allEnemies[i];

                            if (inTutorial)
                                OnTutorialTargetRight(TypeOfTutorial.TargetRight);
                        }
                    }
                }
            }
        }

        if (definitiveTarget != default)
        {
            // Moves the current target towards the desired target
            if (isLerpingTargetCoroutine == null)
                isLerpingTargetCoroutine = 
                    StartCoroutine(LerpingTargetToClosestTarget(definitiveTarget));
        }
    }

    /// <summary>
    /// Moves the current target towards the desired target.
    /// </summary>
    /// <param name="aimTowards"></param>
    /// <returns></returns>
    private IEnumerator LerpingTargetToClosestTarget(EnemyBase aimTowards)
    {
        YieldInstruction wffup = new WaitForFixedUpdate();

        while (currentTarget.transform.position.Similiar(
            aimTowards.transform.position + targetYOffset, 0.05f) == false)
        {
            currentTarget.transform.position = 
                Vector3.MoveTowards(
                    currentTarget.transform.position,
                    aimTowards.transform.position + targetYOffset, 
                    45 * Time.fixedUnscaledDeltaTime);

            yield return wffup;
        }
        FindCurrentTargetedEnemy();
        UpdateTargetCameraLookAt();
        isLerpingTargetCoroutine = null;
    }

    /// <summary>
    /// Finds all enemies around the player.
    /// </summary>
    private void FindAllEnemiesAroundPlayer(float sizeToSearch)
    {
        allEnemies = new List<EnemyBase>();

        // Finds all enemies around
        if (player != null)
        {
            enemies =
                Physics.OverlapSphere(player.transform.position, sizeToSearch, enemyLayer);

            // If enemy has an Enemy script
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i].gameObject.TryGetComponent(out EnemyBase en))
                {
                    if (en.MyTarget.CanSee(playerTarget, collisionLayers))
                    {
                        allEnemies.Add(en);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Starts coroutine to update third person camera to player's back.
    /// </summary>
    /// <param name="positionToUpdateTo">Position to update camera to.</param>
    /// <param name="transitionCamSecs">Seconds to wait for transition.</param>
    public void UpdateThirdPersonCameraPosition(Vector3 positionToUpdateTo, float transitionCamSecs) =>
        StartCoroutine(UpdateThirdPersonCameraPositionCoroutine(positionToUpdateTo, transitionCamSecs));

    /// <summary>
    /// Updates third person camera to player's back.
    /// </summary>
    /// <param name="positionToUpdateTo">Position to update camera to.</param>
    /// <returns></returns>
    private IEnumerator UpdateThirdPersonCameraPositionCoroutine(
        Vector3 positionToUpdateTo, float transitionCamSecs)
    {
        currentTarget.transform.position = positionToUpdateTo;
        UpdateTargetCameraLookAt();
        targetCamera.Priority = thirdPersonCamera.Priority + 3;
        yield return new WaitForSeconds(transitionCamSecs);
        targetCamera.Priority = thirdPersonCamera.Priority - 3;
    }

    /// <summary>
    /// Returns currently targeted enemy.
    /// </summary>
    /// <returns>Returns a target.</returns>
    private GameObject FindCurrentTargetedEnemy()
    {
        // Finds enemies around the current target
        Collider[] currentTargetPosition =
            Physics.OverlapSphere(currentTarget.transform.position, 0.2f, enemyLayer);

        // If enemy has an Enemy script
        for (int i = 0; i < currentTargetPosition.Length; i++)
        {
            if (currentTargetPosition[i].gameObject.TryGetComponent(out EnemyBase en))
            {
                return currentTargetPosition[i].gameObject;
            }
        }
        return null;
    }

    /// <summary>
    /// Updates target camera with current target.
    /// </summary>
    private void UpdateTargetCameraLookAt() => 
        targetCamera.LookAt = currentTarget;

    /// <summary>
    /// Cancels current target.
    /// </summary>
    public void CancelCurrentTarget()
    {
        // Switches camera back to third person camera
        targetCamera.Priority = thirdPersonCamera.Priority - 3;
        if (currentTarget) currentTarget.gameObject.SetActive(false);
        Targeting = false;
    }

    /// <summary>
    /// Calls a coroutine to cancel the current target. Only happens after an
    /// enemy dies.
    /// </summary>
    public void CancelCurrentTargetOnDeath() =>
        StartCoroutine(CancelCurrentTargetOnDeathCoroutine());

    /// <summary>
    /// Cancels current target automatically. This method only happens when
    /// an enemy dies.
    /// </summary>
    private IEnumerator CancelCurrentTargetOnDeathCoroutine()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        FindAllEnemiesAroundPlayer(findTargetSize);
        if (allEnemies.Count == 0)
        {
            // Switches camera back to third person camera
            mainCameraBrain.m_DefaultBlend.m_Time = 0.75f;
            CancelCurrentTarget();
        }
    }

    /// <summary>
    /// Invokes coroutine to find the second closest target automatically. 
    /// This method is used when an enemy dies.
    /// </summary>
    public void AutomaticallyFindTargetCall(float sizeToFindTarget)
        => StartCoroutine(AutomaticallyFindTarget(sizeToFindTarget));

    /// <summary>
    /// Finds second closest target automatically after ending the current frame. 
    /// </summary>
    /// <returns>Wait for end of frame.</returns>
    private IEnumerator AutomaticallyFindTarget(float sizeToFindTarget)
    {
        yield return new WaitForEndOfFrame();

        // If player is targetting and autolock option is on
        if (Targeting && configScriptableObj.AutoLock)
        {
            currentTarget.gameObject.SetActive(true);

            FindAllEnemiesAroundPlayer(sizeToFindTarget);

            // Orders array with all VISIBLE enemies by distance
            EnemyBase[] organizedEnemiesByDistance =
                allEnemies.OrderBy(i =>
                (i.transform.position - player.transform.position).magnitude).
                Where(i => i.MyTarget.CanSee(playerTarget, collisionLayers)).
                ToArray();

            // If there's a target
            if (organizedEnemiesByDistance.Length > 0)
            {
                // Moves the current target towards the desired target
                if (isLerpingTargetCoroutine == null)
                    isLerpingTargetCoroutine = 
                        StartCoroutine(LerpingTargetToClosestTarget(
                            organizedEnemiesByDistance[0]));

                // Switches camera
                targetCamera.Priority = thirdPersonCamera.Priority + 3;
                UpdateTargetCameraLookAt();
                FindCurrentTargetedEnemy();
            }
            else
            {
                CancelCurrentTarget();
            }
        }
        // Means the player has autolock option off
        else
        {
            CancelCurrentTarget();
        }
    }

    /// <summary>
    /// Changes to SceneChangerCamera.
    /// </summary>
    /// <param name="condition">Condition to check if it should change to
    /// scene camera or back to previous camera.</param>
    private void SwitchToSceneChangerCamera(bool condition)
    {
        mainCameraBrain.m_DefaultBlend.m_Time = 0.5f;
        if (condition == true) sceneChangerCamera.Priority = 100;
        else sceneChangerCamera.Priority = -100;
    }

    /// <summary>
    /// Switches cameras when the player pauses or unpauses the game.
    /// </summary>
    /// <param name="pauseSystem">Parameter that checks if the player
    /// paused or unpaused the game.</param>
    private void SwitchBetweenPauseCamera(PauseSystemEnum pauseSystem)
    {
        if (pauseSystem == PauseSystemEnum.Paused)
        {
            mainCameraBrain.m_UpdateMethod =
            CinemachineBrain.UpdateMethod.LateUpdate;
            mainCameraBrain.m_BlendUpdateMethod =
            CinemachineBrain.BrainUpdateMethod.LateUpdate;

            mainCameraBrain.m_DefaultBlend.m_Time = 0.1f;

            pauseMenuCamera.Priority = 100;
        }
        else if (pauseSystem == PauseSystemEnum.Unpaused)
        {
            SlowMotionBehaviour slowMotionBehaviour = 
                FindObjectOfType<SlowMotionBehaviour>();

            if (slowMotionBehaviour.Performing)
            {
                mainCameraBrain.m_BlendUpdateMethod =
                    CinemachineBrain.BrainUpdateMethod.FixedUpdate;     
            }
            else
            {
                mainCameraBrain.m_BlendUpdateMethod =
                    CinemachineBrain.BrainUpdateMethod.FixedUpdate;
            }
            mainCameraBrain.m_UpdateMethod =
                    CinemachineBrain.UpdateMethod.FixedUpdate;

            pauseMenuCamera.Priority = 0;

            StartCoroutine(CameraBlendTimeToNormal());
        }
    }

    private IEnumerator CameraBlendTimeToNormal()
    {
        yield return new WaitForFixedUpdate();
        while(mainCameraBrain.IsBlending)
        {
            yield return null;
        }
        mainCameraBrain.m_DefaultBlend.m_Time = 0.75f;
    }

    /// <summary>
    /// Sets cameras follows and lookats.
    /// </summary>
    private void SetAllCamerasTargets()
    {
        if (player != null)
        {
            thirdPersonCamera.Follow = playerTargetForCinemachine;
            thirdPersonCamera.LookAt = playerTargetForCinemachine;
            targetCamera.Follow = playerTargetForCinemachine;
            pauseMenuCamera.Follow = playerTargetForCinemachine;
            pauseMenuCamera.LookAt = playerTargetForCinemachine;
        }
    }

    /// <summary>
    /// Happens every time slow motion is triggered.
    /// </summary>
    /// <param name="condition">Parameter to check if it's slow motion or normal time</param>
    private void SlowMotionCamera(SlowMotionEnum condition)
    {
        if (condition == SlowMotionEnum.SlowMotion)
        {
            mainCameraBrain.m_DefaultBlend.m_Time = 0.1f;

            mainCameraBrain.m_UpdateMethod =
                CinemachineBrain.UpdateMethod.FixedUpdate;
            mainCameraBrain.m_BlendUpdateMethod =
                CinemachineBrain.BrainUpdateMethod.FixedUpdate;
        }
        else
        {
            mainCameraBrain.m_UpdateMethod =
            CinemachineBrain.UpdateMethod.FixedUpdate;

            mainCameraBrain.m_BlendUpdateMethod =
                CinemachineBrain.BrainUpdateMethod.LateUpdate;

            mainCameraBrain.m_DefaultBlend.m_Time = 0.1f;
        }
    }

    /// <summary>
    /// Checks if camera is blending.
    /// </summary>
    /// <returns>Returns true if camera is blending.</returns>
    public bool IsBlending()
    {
        if (mainCameraBrain.IsBlending) return true;
        return false;
    }

    /// <summary>
    /// If the player spawns on the scene.
    /// </summary>
    public void FindPlayer()
    {
        player = FindObjectOfType<Player>();
        playerWallHug = FindObjectOfType<PlayerWallHug>();
        playerTarget =
            GameObject.FindGameObjectWithTag("playerTarget").transform;
        playerTargetForCinemachine =
            GameObject.FindGameObjectWithTag("playerTargetForCinemachine").transform;
        SetAllCamerasTargets();
        mainCameraBrain.enabled = true;
    }

    /// <summary>
    /// If the player is destroyed from the scene.
    /// </summary>
    public void PlayerLost()
    {
        thirdPersonCamera.Follow = null;
        thirdPersonCamera.LookAt = null;
        targetCamera.Follow = null;
        pauseMenuCamera.Follow = null;
        pauseMenuCamera.LookAt = null;
        mainCameraBrain.enabled = false;
    }

    /// <summary>
    /// Updates values when configScriptableObj are updated.
    /// </summary>
    public void UpdateValues()
    {
        thirdPersonCamera.m_YAxis.m_MaxSpeed = configScriptableObj.VerticalSensibility;
        thirdPersonCamera.m_XAxis.m_MaxSpeed = configScriptableObj.HorizontalSensibility;
    }


    ///////////////////// Tutorial methods and events //////////////////////////
    protected virtual void OnTutorialTarget(TypeOfTutorial typeOfTut) =>
        TutorialTarget?.Invoke(typeOfTut);

    protected virtual void OnTutorialTargetLeft(TypeOfTutorial typeOfTut) =>
        TutorialTargetLeft?.Invoke(typeOfTut);

    protected virtual void OnTutorialTargetRight(TypeOfTutorial typeOfTut) =>
        TutorialTargetRight?.Invoke(typeOfTut);

    public event Action<TypeOfTutorial> TutorialTarget;
    public event Action<TypeOfTutorial> TutorialTargetLeft;
    public event Action<TypeOfTutorial> TutorialTargetRight;
    ////////////////////////////////////////////////////////////////////////////
}
