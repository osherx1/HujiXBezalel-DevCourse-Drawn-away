using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This script handles purely aesthetic things like particles, squash & stretch, and tilt

public class characterJuice : MonoBehaviour
{
    [Header("Components")]
    characterMovement moveScript;
    characterJump jumpScript;
    [SerializeField] Animator myAnimator;
    [SerializeField] GameObject characterSprite;

    [Header("Components - Particles")]
    [SerializeField] private ParticleSystem moveParticles;
    [SerializeField] private ParticleSystem jumpParticles;
    [SerializeField] private ParticleSystem landParticles;

    [Header("Components - Audio")]
    [SerializeField] AudioSource jumpSFX;
    [SerializeField] AudioSource landSFX;

    [Header("Settings - Squash and Stretch")]
    [SerializeField] bool squashAndStretch;
    [SerializeField, Tooltip("Width Squeeze, Height Squeeze, Duration")] Vector3 jumpSquashSettings;
    [SerializeField, Tooltip("Width Squeeze, Height Squeeze, Duration")] Vector3 landSquashSettings;
    [SerializeField, Tooltip("How powerful should the effect be?")] public float landSqueezeMultiplier;
    [SerializeField, Tooltip("How powerful should the effect be?")] public float jumpSqueezeMultiplier;
    [SerializeField] float landDrop = 1;

    [Header("Tilting")]

    [SerializeField] bool leanForward;
    [SerializeField, Tooltip("How far should the character tilt?")] public float maxTilt;
    [SerializeField, Tooltip("How fast should the character tilt?")] public float tiltSpeed;

    [Header("Calculations")]
    [SerializeField, Tooltip("Animator float parameter that represents horizontal speed.")] private string runSpeedParameter = "runSpeed";
    [SerializeField, Tooltip("When true, vertical velocity is included when calculating the run speed value.")] private bool includeVerticalSpeedInRunParameter;
    [SerializeField, Tooltip("Trigger parameter that plays when the character jumps.")] private string jumpTriggerParameter = "Jump";
    [SerializeField, Tooltip("Trigger parameter that plays when the character lands.")] private string landedTriggerParameter = "Landed";
    [SerializeField, Tooltip("Runtime-only readout of the current run speed value.")]
    private float debugCurrentSpeed;
    [SerializeField, Tooltip("Log every time the run speed value updates (useful while tuning animator thresholds).")]
    private bool logSpeedChanges;
    [SerializeField] public float maxSpeed;
    public float runningSpeed;

    [Header("Current State")]
    public bool squeezing;
    public bool jumpSqueezing;
    public bool landSqueezing;
    public bool playerGrounded;

    [Header("Platformer Toolkit Stuff")]
    [SerializeField] bool showJumpLine;
    [SerializeField] jumpTester jumpLine;
    public bool cameraFalling = false;
    [SerializeField, Tooltip("Consider the character grounded for landing purposes when the ground is closer than this distance.")]
    private float earlyLandDistance = 0.1f;
    [SerializeField] private characterGround ground;

    void Start()
    {
        moveScript = GetComponent<characterMovement>();
        jumpScript = GetComponent<characterJump>();
        if (ground == null)
        {
            ground = GetComponent<characterGround>();
        }
    }

    void Update()
    {
        tiltCharacter();

        //We need to change the character's running animation to suit their current speed
        float previousSpeed = debugCurrentSpeed;
        Vector2 sourceVelocity = moveScript != null ? moveScript.velocity : Vector2.zero;
        if (jumpScript != null && jumpScript.body != null)
        {
            sourceVelocity = jumpScript.body.linearVelocity;
        }

        float computedSpeed = includeVerticalSpeedInRunParameter ? sourceVelocity.magnitude : Mathf.Abs(sourceVelocity.x);

        float speedClamp = maxSpeed;
        if (speedClamp <= 0f && moveScript != null)
        {
            //Fall back to the movement script's max speed so animator thresholds stay in sync.
            speedClamp = moveScript.maxSpeed;
        }

        runningSpeed = speedClamp > 0f ? Mathf.Clamp(computedSpeed, 0f, speedClamp) : computedSpeed;
        debugCurrentSpeed = runningSpeed;

        if (logSpeedChanges && !Mathf.Approximately(previousSpeed, debugCurrentSpeed))
        {
            Debug.Log($"[characterJuice] runSpeed: {debugCurrentSpeed:F2}");
        }

        if (myAnimator != null && !string.IsNullOrEmpty(runSpeedParameter) && HasAnimatorParameter(runSpeedParameter))
        {
            myAnimator.SetFloat(runSpeedParameter, runningSpeed);
        }

        checkForLanding();

        checkForGoingPastJumpLine();
    }

    private void tiltCharacter()
    {
        //See which direction the character is currently running towards, and tilt in that direction
        float directionToTilt = 0;
        if (moveScript.velocity.x != 0)
        {
            directionToTilt = Mathf.Sign(moveScript.velocity.x);
        }

        //Create a vector that the character will tilt towards
        Vector3 targetRotVector = new Vector3(0, 0, Mathf.Lerp(-maxTilt, maxTilt, Mathf.InverseLerp(-1, 1, directionToTilt)));

        //And then rotate the character in that direction
        myAnimator.transform.rotation = Quaternion.RotateTowards(myAnimator.transform.rotation, Quaternion.Euler(-targetRotVector), tiltSpeed * Time.deltaTime);
    }

    private void checkForLanding()
    {
        bool isOnGround = jumpScript.onGround;
        bool almostGrounded = ground != null && ground.IsNearGround(earlyLandDistance) && jumpScript.velocity.y <= 0.1f;

        if (!playerGrounded && (isOnGround || almostGrounded))
        {
            //By checking for this, and then immediately setting playerGrounded to true, we only run this code once when the player hits the ground 
            playerGrounded = true;
            cameraFalling = false;

            //This is related to the "ignore jumps" option on the camera panel.
            if (jumpLine != null)
            {
                jumpLine.characterY = transform.position.y;
            }

            //Play an animation, some particles, and a sound effect when the player lands
            TriggerAnimator(landedTriggerParameter);
            if (landParticles != null)
            {
                landParticles.Play();
            }

            if (landSFX != null && !landSFX.isPlaying && landSFX.enabled)
            {
                landSFX.Play();
            }

            if (moveParticles != null)
            {
                moveParticles.Play();
            }

            //Start the landing squash and stretch coroutine.
            if (!landSqueezing && landSqueezeMultiplier > 1)
            {
                StartCoroutine(JumpSqueeze(landSquashSettings.x * landSqueezeMultiplier, landSquashSettings.y / landSqueezeMultiplier, landSquashSettings.z, landDrop, false));
            }

        }
        else if (playerGrounded && !isOnGround)

        {
            // Player has left the ground, so stop playing the running particles
            playerGrounded = false;
            if (moveParticles != null)
            {
                moveParticles.Stop();
            }
        }
    }

    private void checkForGoingPastJumpLine()
    {
        if (jumpLine == null)
        {
            return;
        }

        //This is related to the "ignore jumps" option on the camera panel.
        if (transform.position.y < jumpLine.transform.position.y - 3)
        {
            cameraFalling = true;
        }

        if (cameraFalling)
        {
            jumpLine.characterY = transform.position.y;
        }
    }

    public void jumpEffects()
    {
        //Play these effects when the player jumps, courtesy of jump script
        ResetAnimatorTrigger(landedTriggerParameter);
        TriggerAnimator(jumpTriggerParameter);

        if (jumpSFX != null && jumpSFX.enabled)
        {
            jumpSFX.Play();

        }

        if (!jumpSqueezing && jumpSqueezeMultiplier > 1)
        {
            StartCoroutine(JumpSqueeze(jumpSquashSettings.x / jumpSqueezeMultiplier, jumpSquashSettings.y * jumpSqueezeMultiplier, jumpSquashSettings.z, 0, true));

        }

        if (jumpParticles != null)
        {
            jumpParticles.Play();
        }
    }

    IEnumerator JumpSqueeze(float xSqueeze, float ySqueeze, float seconds, float dropAmount, bool jumpSqueeze)
    {
        //We log that the player is squashing/stretching, so we don't do these calculations more than once
        if (jumpSqueeze) { jumpSqueezing = true; }
        else { landSqueezing = true; }
        squeezing = true;

        Vector3 originalSize = Vector3.one;
        Vector3 newSize = new Vector3(xSqueeze, ySqueeze, originalSize.z);

        Vector3 originalPosition = Vector3.zero;
        Vector3 newPosition = new Vector3(0, -dropAmount, 0);

        //We very quickly lerp the character's scale and position to their squashed and stretched pose...
        float t = 0f;
        while (t <= 1.0)
        {
            t += Time.deltaTime / 0.01f;
            characterSprite.transform.localScale = Vector3.Lerp(originalSize, newSize, t);
            characterSprite.transform.localPosition = Vector3.Lerp(originalPosition, newPosition, t);
            yield return null;
        }

        //And then we lerp back to the original scale and position at a speed dicated by the developer
        //It's important to do this to the character's sprite, not the gameobject with a Rigidbody an/or collision detection
        t = 0f;
        while (t <= 1.0)
        {
            t += Time.deltaTime / seconds;
            characterSprite.transform.localScale = Vector3.Lerp(newSize, originalSize, t);
            characterSprite.transform.localPosition = Vector3.Lerp(newPosition, originalPosition, t);
            yield return null;
        }

        if (jumpSqueeze) { jumpSqueezing = false; }
        else { landSqueezing = false; }
    }

    private void TriggerAnimator(string triggerName)
    {
        if (myAnimator == null || string.IsNullOrEmpty(triggerName))
        {
            return;
        }

        if (HasAnimatorParameter(triggerName, AnimatorControllerParameterType.Trigger))
        {
            myAnimator.SetTrigger(triggerName);
        }
    }

    private void ResetAnimatorTrigger(string triggerName)
    {
        if (myAnimator == null || string.IsNullOrEmpty(triggerName))
        {
            return;
        }

        if (HasAnimatorParameter(triggerName, AnimatorControllerParameterType.Trigger))
        {
            myAnimator.ResetTrigger(triggerName);
        }
    }

    private bool HasAnimatorParameter(string paramName, AnimatorControllerParameterType type = AnimatorControllerParameterType.Float)
    {
        if (myAnimator == null)
        {
            return false;
        }

        foreach (var param in myAnimator.parameters)
        {
            if (param.name == paramName && param.type == type)
            {
                return true;
            }
        }

        return false;
    }
}