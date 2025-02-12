﻿using System.Collections;
using UnityEngine;
using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Class responsible for handling slow motion.
/// </summary>
public class SlowMotionBehaviour : MonoBehaviour, IFindPlayer
{
    [SerializeField] private bool inTutorial;

    // Slowmotion Variables
    private float defaultTimeScale;
    private float defaultFixedDeltaTime;
    private float slowMotionSpeed; // Speed of slow motion (Time.timeScale)
    private float slowMotionDuration;
    private float slowMotionSmoothSpeed; //Smoothing time beetween the transition from normal time to slow motion
    private Coroutine SlowmotionCoroutine;

    // Components
    private PlayerRoll playerRoll;
    private PlayerSounds playerSounds;
    private PlayerDeathBehaviour playerDeath;
    private PauseSystem pauseSystem;
    private Volume postProcessing;

    // Slow motion variables
    public bool Performing { get; private set; }
    private ChromaticAberration chromaticA;
    private LensDistortion lensDistor;
    private ColorAdjustments colorAdj;

    // Particles from player's mesh
    [SerializeField] private OptionsScriptableObj options;
    [SerializeField] private ParticleSystem slowMotionParticles;
    private ParticleSystem cloneParticles;

    // Slow Motion Shader Variables
    [SerializeField] private Material slowMotionMaterial;

    private void Awake()
    {
        playerRoll = FindObjectOfType<PlayerRoll>();
        playerSounds = FindObjectOfType<PlayerSounds>();
        playerDeath = FindObjectOfType<PlayerDeathBehaviour>();
        pauseSystem = FindObjectOfType<PauseSystem>();
        postProcessing = 
            GameObject.FindGameObjectWithTag("postProcessing").GetComponent<Volume>();
    }

    private void Start()
    {
        defaultTimeScale = 1f;
        defaultFixedDeltaTime = 0.01f;
        Performing = false;
        SlowmotionCoroutine = null;

        slowMotionMaterial.SetFloat("Vector1_1D53D2E0", 0f); // WaveSize
        slowMotionMaterial.SetFloat("Vector1_34F127BD", 0f); // WaveStrength
        slowMotionMaterial.SetFloat("Vector1_58B5DC2F", 0f); // TimeMultiplication
        slowMotionMaterial.SetFloat("Vector1_24514F13", 0f); // WaveTime

        slowMotionSpeed = 0.3f;
        slowMotionDuration = 5f;
        slowMotionSmoothSpeed = 0.005f;

        StopSlowMotion();
    }

    private void OnEnable()
    {
        if (playerRoll != null) playerRoll.Dodge += TriggerSlowMotion;
        if (playerDeath != null) playerDeath.PlayerDied += StopSlowMotion;
    }

    private void OnDisable()
    {
        if (playerRoll != null) playerRoll.Dodge -= TriggerSlowMotion;
        if (playerDeath != null) playerDeath.PlayerDied -= StopSlowMotion;

        StopSlowMotion();
    }

    private void TriggerSlowMotion()
    {
        if (SlowmotionCoroutine == null)
            SlowmotionCoroutine = StartCoroutine(SlowMotion());
    }

    /// <summary>
    /// Coroutine responsible for slowing time.
    /// </summary>
    /// <returns>Null.</returns>
    private IEnumerator SlowMotion()
    {
        // Changes camera update mode.
        // Event for Cinemachine Target. Controls cinemachine brain.
        OnSlowMotionEvent(SlowMotionEnum.SlowMotion);
        playerSounds.PlaySound(Sound.SlowMotion);

        if (inTutorial)
            OnTutorialSlowMotion(TypeOfTutorial.SlowMotion);

        Performing = true;

        // Only plays if after images are enabled
        if (options.AfterImages)
        {
            SkinnedMeshRenderer playerMesh =
               GameObject.FindGameObjectWithTag("playerMesh").
               GetComponent<SkinnedMeshRenderer>();
            cloneParticles = Instantiate(slowMotionParticles);
            var slowMotionParticlesMesh = cloneParticles.shape;
            slowMotionParticlesMesh.skinnedMeshRenderer = playerMesh;
        }

        // Post process variables
        if (postProcessing.profile.TryGet(out chromaticA))
        {
            chromaticA.active = true;
            chromaticA.intensity.value = 0;
        }
            
        if (postProcessing.profile.TryGet(out lensDistor))
        {
            lensDistor.active = true;
            lensDistor.intensity.value = 0;
        }

        if (postProcessing.profile.TryGet(out colorAdj))
            colorAdj.saturation.value = 0;
            
        slowMotionMaterial.SetFloat("Vector1_1D53D2E0", 0.03f); // WaveSize
        slowMotionMaterial.SetFloat("Vector1_34F127BD", -0.2f); // WaveStrength
        slowMotionMaterial.SetFloat("Vector1_58B5DC2F", 1f);    // TimeMultiplication
        float waveTime = 0f;

        float currentTimePassed = 0;
        while (currentTimePassed < slowMotionDuration)
        {
            if (chromaticA.intensity.value < 1)
                chromaticA.intensity.value += Time.fixedUnscaledDeltaTime;
            if (lensDistor.intensity.value > -0.6f)
                lensDistor.intensity.value -= Time.fixedUnscaledDeltaTime;
            if (colorAdj.saturation.value > -100)
                colorAdj.saturation.value -= Time.fixedUnscaledDeltaTime * 40;

            // This variable goes from 0 to 1, growing the wave until the
            // edge of the screen
            // Wave time
            if (waveTime < 0.99f) waveTime = currentTimePassed / (slowMotionDuration * 0.5f);
            slowMotionMaterial.SetFloat("Vector1_24514F13", waveTime);

            // First half of the slow motion effect
            if (!pauseSystem.PausedGame && currentTimePassed < slowMotionDuration * 0.25f)
            {
                Time.timeScale = Mathf.Lerp(
                    Time.timeScale, 
                    slowMotionSpeed, 
                    slowMotionSmoothSpeed * 8);
            }

            // Second half of the slow motion effect, returns to normal speed
            else if (!pauseSystem.PausedGame && currentTimePassed > slowMotionDuration - (slowMotionDuration * 0.175))
            {
                Time.timeScale = Mathf.Lerp(
                    Time.timeScale, 
                    defaultTimeScale, 
                    slowMotionSmoothSpeed * 9);

                slowMotionMaterial.SetFloat("Vector1_1D53D2E0", 0f); // WaveSize
                slowMotionMaterial.SetFloat("Vector1_34F127BD", 0f); // WaveStrength
                slowMotionMaterial.SetFloat("Vector1_58B5DC2F", 0f); // TimeMultiplication
                slowMotionMaterial.SetFloat("Vector1_24514F13", 0f); // WaveTime

                if (chromaticA.intensity.value > 0)
                    chromaticA.intensity.value -= Time.fixedUnscaledDeltaTime * 2f;
                if (lensDistor.intensity.value < -0)
                    lensDistor.intensity.value += Time.fixedUnscaledDeltaTime * 2f;
                if (colorAdj.saturation.value < 0)
                    colorAdj.saturation.value += Time.fixedUnscaledDeltaTime * 100;
            }

            if (pauseSystem.PausedGame)
            {
                Time.fixedDeltaTime = 0f;
                currentTimePassed += 0f;
            }
            else if (!pauseSystem.PausedGame)
            {
                Time.fixedDeltaTime = Time.timeScale * 0.01f;
                currentTimePassed += Time.unscaledDeltaTime;
            }

            yield return null;
        }

        StopSlowMotion();
        SlowmotionCoroutine = null;
    }

    /// <summary>
    /// Returns time to normal.
    /// </summary>
    private void StopSlowMotion()
    {
        if (postProcessing.profile.TryGet(out chromaticA))
            chromaticA.active = false;
        if (postProcessing.profile.TryGet(out lensDistor))
            lensDistor.active = false;
        if (postProcessing.profile.TryGet(out colorAdj))
            colorAdj.saturation.value = 0;

        slowMotionMaterial.SetFloat("Vector1_1D53D2E0", 0f); // WaveSize
        slowMotionMaterial.SetFloat("Vector1_34F127BD", 0f); // WaveStrength
        slowMotionMaterial.SetFloat("Vector1_58B5DC2F", 0f); // TimeMultiplication
        slowMotionMaterial.SetFloat("Vector1_24514F13", 0f); // WaveTime

        Time.timeScale = defaultTimeScale;
        Time.fixedDeltaTime = defaultFixedDeltaTime;

        // Event for Cinemachine Target. Controls cinemachine brain.
        OnSlowMotionEvent(SlowMotionEnum.NormalTime);

        Performing = false;

        if (options.AfterImages)
        {
            if (cloneParticles && cloneParticles.isPlaying) 
                cloneParticles.Stop();
        }
    }

    public void FindPlayer()
    {
        playerRoll = FindObjectOfType<PlayerRoll>();
        playerSounds = FindObjectOfType<PlayerSounds>();
        playerDeath = FindObjectOfType<PlayerDeathBehaviour>();
        playerRoll.Dodge += TriggerSlowMotion;
        playerDeath.PlayerDied += StopSlowMotion;
    }

    public void PlayerLost()
    {
        playerRoll.Dodge -= TriggerSlowMotion;
        playerDeath.PlayerDied -= StopSlowMotion;
    }

    protected virtual void OnSlowMotionEvent(SlowMotionEnum condition) => 
        SlowMotionEvent?.Invoke(condition);

    /// <summary>
    /// Event registered on CinemachineTarget. 
    /// Event registered on PlayerMovement.
    /// Event registered on AudioController.
    /// </summary>
    public event Action<SlowMotionEnum> SlowMotionEvent;


    ///////////////////// Tutorial methods and events //////////////////////////
    protected virtual void OnTutorialSlowMotion(TypeOfTutorial typeOfTut) =>
        TutorialSlowMotion?.Invoke(typeOfTut);

    public event Action<TypeOfTutorial> TutorialSlowMotion;
    ////////////////////////////////////////////////////////////////////////////
}
