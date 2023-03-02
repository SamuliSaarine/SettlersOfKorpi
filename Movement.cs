using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public bool isGrounded;
    public bool isSprinting;
    public bool isCrouching;

    public Player player;
    public Transform cam;
    public Transform model;

    public float speed = 20f;
    public float sprintSpeed = 40f;
    public float sneakSpeed = 15f;
    public float slideFriction;
    public float slideMomentum;
    public float jumpForce = 20f;
    public float bigJumpForce = 40f;

    public float playerWidth = 0.3f;
    public float groundedCheckOffset = 0.1f;

    public float playerHeight = 3.1f;

    public float gravity = -8f;

    public float mouseSensitivity = 4f;

    public bool trickJumps;

    public AudioSource footSteps;
    private bool playingSteps;
    private bool requestStep;
    private float stepSpeed;

    [HideInInspector] public float horizontal;
    [HideInInspector] public float vertical;
    float lastGroundedHor = 0;
    float lastGroundedVer = 0;
    [HideInInspector] public float mouseX;
    [HideInInspector] public float mouseY;
    Vector3 direction;
    Vector3 velocity;
    private float verticalMomentum = 0;
    [HideInInspector] public bool jumpRequest;
    private bool rotFree = false;

    public void Update()
    {
        if (player.InUI) return;

        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");

        if (isGrounded)
        {
           isSprinting = Input.GetButton("Sprint");
        }

        if (isGrounded && Input.GetButtonDown("Jump") && player.stats.staminaSlider.value > 0.05f)
        {
            jumpRequest = true;
        }


        Look();
        Move();
    }

    void Look()
    {
        if (trickJumps)
        {
            if (isGrounded)
            {
                CancelInvoke("FreeRot");
                rotFree = false;

                if (cam.parent != transform)
                {
                    cam.SetParent(transform);
                    cam.localPosition = new Vector3(0, 1.9f, 0.1f);
                    cam.localRotation = Quaternion.Euler(model.localRotation.eulerAngles.x, 0, 0);

                    if (cam.localRotation.eulerAngles.x <= 275 && cam.localRotation.eulerAngles.x > 180)
                    {
                        cam.localRotation = Quaternion.Euler(0, 0, 0);
                    }

                }

                if (model.localRotation.eulerAngles.y != 0 || model.eulerAngles.x != 0)
                {
                    transform.rotation = Quaternion.Euler(0, model.rotation.eulerAngles.y, 0);
                    model.localPosition = new Vector3(0, 1, 0);
                    model.localRotation = Quaternion.Euler(0, 0, 0);
                }

                transform.Rotate(mouseX * mouseSensitivity * Vector3.up);

                if (cam.localEulerAngles.x > 275 || cam.localEulerAngles.x < 85)
                {
                    cam.Rotate(-mouseY * mouseSensitivity * Vector3.right);
                }
                else if (cam.localEulerAngles.x < 95 && -mouseY < 0)
                {
                    cam.Rotate(-mouseY * mouseSensitivity * Vector3.right);
                }
                else if (cam.localEulerAngles.x > 265 && -mouseY > 0)
                {
                    cam.Rotate(-mouseY * mouseSensitivity * Vector3.right);
                }

            }
            else
            {
                if (!rotFree)
                {
                    Invoke(nameof(FreeRot), 0.1f);
                }
                else
                {
                    if (cam.parent != model)
                    {
                        cam.SetParent(model);
                        model.localRotation = Quaternion.Euler(cam.localRotation.eulerAngles.x, model.localRotation.eulerAngles.y, 0);
                        cam.localRotation = Quaternion.Euler(0, 0, 0);
                    }

                    model.Rotate(mouseX * mouseSensitivity * Vector3.up);
                    model.Rotate(-mouseY * mouseSensitivity * Vector3.right);
                }
            }
        }
        else
        {
            if (isGrounded)
            {
                if (cam.localEulerAngles.y != 0)
                {
                    transform.rotation = Quaternion.Euler(0, cam.localEulerAngles.y, 0);
                    cam.localRotation = Quaternion.Euler(cam.localEulerAngles.x, 0, 0);
                }

                transform.Rotate(mouseX * mouseSensitivity * Vector3.up);

                if (cam.localEulerAngles.x >= 280 || cam.localEulerAngles.x <= 75)
                {
                    cam.Rotate(-mouseY * mouseSensitivity * Vector3.right);
                }
                else if (cam.localEulerAngles.x < 280 && cam.localEulerAngles.x > 180)
                {
                    cam.localRotation = Quaternion.Euler(280, 0, 0);
                }
                else if (cam.localEulerAngles.x > 75 && cam.localEulerAngles.x <= 180)
                {
                    cam.localRotation = Quaternion.Euler(75, 0, 0);
                }
                else
                {
                    cam.localRotation = Quaternion.Euler(0, 0, 0);
                }
            }
            else
            {
                cam.Rotate(mouseX * mouseSensitivity * Vector3.up);
                if (cam.localEulerAngles.x >= 280 || cam.localEulerAngles.x <= 75)
                {
                    cam.Rotate(-mouseY * mouseSensitivity * Vector3.right);
                }
                else if (cam.localEulerAngles.x < 280 && cam.localEulerAngles.x > 180)
                {
                    cam.localRotation = Quaternion.Euler(280, 0, 0);
                }
                else if (cam.localEulerAngles.x > 75 && cam.localEulerAngles.x <= 180)
                {
                    cam.localRotation = Quaternion.Euler(75, 0, 0);
                }
                else
                {
                    cam.localRotation = Quaternion.Euler(0, 0, 0);
                }
            }
        }
    }

    void FreeRot()
    {
        rotFree = true;
    }

    void Move()
    {
        if (isGrounded)
        {
            direction = Vector3.forward * vertical + Vector3.right * horizontal;
            lastGroundedHor = horizontal;
            lastGroundedVer = vertical;
        }
        else
        {
            playingSteps = false;
            direction = Vector3.forward * ((vertical + lastGroundedVer) / 2) + Vector3.right * ((horizontal + lastGroundedHor) / 2);
        }
    }

    void Jump()
    {
        verticalMomentum = isSprinting ? bigJumpForce : jumpForce;
        player.stats.staminaSlider.gameObject.SetActive(true);
        float drop = isSprinting ? -0.2f : -0.1f;
        player.stats.ChangeStamina(drop);
        isGrounded = false;
        jumpRequest = false;
    }

    private void FixedUpdate()
    {
        if (player.loadingPanel.activeSelf) return;

        CalculateVelocity();
        if (jumpRequest)
        {
            Jump();
        }
        transform.Translate(velocity, Space.Self);
    }

    private void CalculateVelocity()
    {
        if (verticalMomentum > gravity)
        {
            verticalMomentum += Time.fixedDeltaTime * gravity;
        }

        float s = speed;
        footSteps.volume = 0.8f;

        if (isSprinting && direction.z > 0)
        {
            if (isCrouching)
            {
                if (!BackSlope && slideMomentum > sneakSpeed)
                {
                    int multiplier = FrontSlope ? 4 : 1;
                    slideMomentum -= slideFriction * multiplier;
                    s = slideMomentum;
                }
            }
            else
            {
                player.stats.staminaSlider.gameObject.SetActive(true);

                if (player.stats.staminaSlider.value > 0)
                {
                    s = sprintSpeed;
                    footSteps.volume = 1f;
                    slideMomentum = sprintSpeed;
                    player.stats.ChangeStamina(-0.001f);
                }
            }

        }
        else if (isCrouching && direction.magnitude > 0)
        {
            s = sneakSpeed;
            player.stats.ChangeStamina(-0.0002f);
            footSteps.volume = 0.2f;
        }
        else if (player.stats.staminaSlider.value < 1)
        {
            float regressionspeed = direction.magnitude > 0 ? 0.001f : (isCrouching ? 0.004f : 0.002f);
            player.stats.ChangeStamina(regressionspeed);
        }
        else
        {
            player.stats.staminaSlider.gameObject.SetActive(false);
        }

        stepSpeed = 8f / s;

        velocity = s * Time.fixedDeltaTime * direction;

        if (isGrounded && velocity.magnitude > 0.1f && !(isSprinting && isCrouching))
        {
            if (!playingSteps)
            {
                playingSteps = true;
                if (!IsInvoking(nameof(PlayFootstep))) Invoke(nameof(PlayFootstep), stepSpeed);
            }
        }
        else if (playingSteps)
        {
            playingSteps = false;
            CancelInvoke(nameof(PlayFootstep));
        }

        velocity += Time.fixedDeltaTime * verticalMomentum * Vector3.up;

        if ((velocity.z > 0 && FrontBlocked) || (velocity.z < 0 && BackBlocked))
        {
            velocity.z = 0;
        }
        else if (velocity.z > 0 && FrontSlope)
        {
            velocity.y = velocity.z;
        }
        else if (velocity.z < 0 && BackSlope)
        {
            velocity.y = -velocity.z;
        }

        if ((velocity.x > 0 && RightBlocked) || (velocity.x < 0 && LeftBlocked))
        {
            velocity.x = 0;
        }
        else if (velocity.x > 0 && RightSlope)
        {
            velocity.y = velocity.x;
        }
        else if (velocity.x < 0 && LeftSlope)
        {
            velocity.y = -velocity.x;
        }

        if (velocity.y < 0)
        {
            velocity.y = CheckDownSpeed(velocity.y);
        }
        else if (velocity.y > 0)
        {
            velocity.y = CheckUpSpeed(velocity.y);
        }
    }

    private float CheckDownSpeed(float downSpeed)
    {
        if (player.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + downSpeed, transform.position.z) + transform.forward * (1 - groundedCheckOffset), true) ||
            player.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + downSpeed, transform.position.z) - transform.forward * (1 - groundedCheckOffset), true) ||
            player.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + downSpeed, transform.position.z) + transform.right * (1 - groundedCheckOffset), true) ||
            player.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + downSpeed, transform.position.z) - transform.right * (1 - groundedCheckOffset), true))
        {
            AudioClip clip = player.CheckForAudio(new Vector3(transform.position.x, transform.position.y + downSpeed, transform.position.z));
            if (clip != null)
            {
                footSteps.clip = clip;
            }
            isGrounded = true;
            return 0;
        }
        else
        {
            isGrounded = false;
            return downSpeed;
        }
    }

    public float CheckUpSpeed(float upSpeed)
    {
        if (player.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + upSpeed + playerHeight, transform.position.z) + transform.forward * (1 - groundedCheckOffset), true) ||
            player.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + upSpeed + playerHeight, transform.position.z) - transform.forward * (1 - groundedCheckOffset), true) ||
            player.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + upSpeed + playerHeight, transform.position.z) + transform.right * (1 - groundedCheckOffset), true) ||
            player.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + upSpeed + playerHeight, transform.position.z) - transform.right * (1 - groundedCheckOffset), true))
        {
            return 0;
        }
        else
        {
            return upSpeed;
        }
    }

    public bool FrontSlope
    {
        get
        {
            if (player.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z) + transform.forward * playerWidth))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public bool BackSlope
    {
        get
        {
            if (player.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z) - (playerWidth * transform.forward)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public bool FrontBlocked
    {
        get
        {
            if (CheckIfBlocked(0, 1))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public bool BackBlocked
    {
        get
        {
            if (CheckIfBlocked(0, -1))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public bool RightSlope
    {
        get
        {
            if (player.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z) + playerWidth * transform.right))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public bool LeftSlope
    {
        get
        {
            if (player.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z) - (transform.right * playerWidth)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public bool LeftBlocked
    {
        get
        {
            if (CheckIfBlocked(-1, 0))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public bool RightBlocked
    {
        get
        {
            if (CheckIfBlocked(1, 0))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public bool CheckIfBlocked(int x, int y)
    {
        for (int i = 1; i < (isCrouching ? 5 : 7); i++)
        {
            if (player.CheckForVoxel(new Vector3(transform.position.x + playerWidth * x, transform.position.y + i, transform.position.z + playerWidth * y), true)) return true;
        }

        return false;
    }

    void PlayFootstep()
    {
        footSteps.pitch = footSteps.pitch == 1 ? 0.8f : 1;
        footSteps.Play();
        if (playingSteps && !requestStep)
        {
            Invoke(nameof(PlayFootstep), stepSpeed);
        }
    }


}
