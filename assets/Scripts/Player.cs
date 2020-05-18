using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public bool isGrounded;
    public bool isSprinting;

    public Transform cam;
    private World world;

    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float jumpForce = 5f;
    public float gravity = -9.81f;

    public float playerWidth = 0.3f;
    public float boundsTolerance = 0.1f;

    private float horizontal;
    private float vertical;
    private float mouseHorizontal;
    private float mouseVertical;

    private Vector3 velocity;
    private float verticalMomentum = 0;
    private bool jumpRequest;

    public void Start()
    {
        cam = GameObject.Find("Main Camera").transform;
        world = GameObject.Find("World").GetComponent<World>();
    }

    public void FixedUpdate()
    {
        CalculateVelocity();
        if (jumpRequest) Jump();

        cam.Rotate(Vector3.up * mouseHorizontal * 5f);
        cam.Rotate(Vector3.right * -mouseVertical * 5f);
        cam.Translate(velocity, Space.World);
    }

    public void Update()
    {
        GetPlayerInputs();
    }

    private void Jump()
    {
        verticalMomentum = jumpForce;
        isGrounded = false;
        jumpRequest = false;
    }

    private void CalculateVelocity()
    {
        // Affect vertical momentum with gravity
        if (verticalMomentum > gravity) verticalMomentum += Time.fixedDeltaTime * gravity;

        // if we are sprinting use the sprint multiplier
        var speed = walkSpeed;
        if (isSprinting) speed = sprintSpeed;

        velocity = ((cam.forward * vertical) + (cam.right * horizontal)) * Time.fixedDeltaTime * speed;

        // Apply vertical momentum (falling / jumping)
        velocity += Vector3.up * verticalMomentum * Time.fixedDeltaTime;

        if ((velocity.z > 0 && front) || (velocity.z < 0 && back)) velocity.z = 0;
        if ((velocity.x > 0 && right) || (velocity.x < 0 && left)) velocity.x = 0;

        if (velocity.y < 0) velocity.y = checkDownSpeed(velocity.y);
        else if (velocity.y > 0) velocity.y = checkUpSpeed(velocity.y);
    }


    private void GetPlayerInputs()
    {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        mouseHorizontal = Input.GetAxis("Mouse X");
        mouseVertical = Input.GetAxis("Mouse Y");

        if (Input.GetButtonDown("Sprint")) isSprinting = true;
        else if (Input.GetButtonUp("Sprint")) isSprinting = false;

        if (isGrounded && Input.GetButtonDown("Jump")) jumpRequest = true;

    }

    private float checkDownSpeed (float downSpeed)
    {
        if (world.CheckForVoxel(cam.position.x - playerWidth, cam.position.y - 1.8f + downSpeed, cam.position.z - playerWidth) ||
            world.CheckForVoxel(cam.position.x + playerWidth, cam.position.y - 1.8f + downSpeed, cam.position.z - playerWidth) ||
            world.CheckForVoxel(cam.position.x + playerWidth, cam.position.y - 1.8f + downSpeed, cam.position.z + playerWidth) ||
            world.CheckForVoxel(cam.position.x - playerWidth, cam.position.y - 1.8f + downSpeed, cam.position.z + playerWidth))
        {
            isGrounded = true;
            return 0;
        }

        isGrounded = false;
        return downSpeed;
    }

    private float checkUpSpeed(float upSpeed)
    {
        if (world.CheckForVoxel(cam.position.x - playerWidth, cam.position.y + .2f + upSpeed, cam.position.z - playerWidth) ||
            world.CheckForVoxel(cam.position.x + playerWidth, cam.position.y + .2f + upSpeed, cam.position.z - playerWidth) ||
            world.CheckForVoxel(cam.position.x + playerWidth, cam.position.y + .2f + upSpeed, cam.position.z + playerWidth) ||
            world.CheckForVoxel(cam.position.x - playerWidth, cam.position.y + .2f + upSpeed, cam.position.z + playerWidth))
        {
            return 0;
        }
        return upSpeed;
    }

    public bool front
    {
        get
        {
            if (world.CheckForVoxel(cam.position.x, cam.position.y - 1.8f, cam.position.z + playerWidth) ||
                world.CheckForVoxel(cam.position.x, cam.position.y - .8f, cam.position.z + playerWidth))
            {
                return true;
            }
            return false;
        }
    }

    public bool back
    {
        get
        {
            if (world.CheckForVoxel(cam.position.x, cam.position.y - 1.8f, cam.position.z - playerWidth) ||
                world.CheckForVoxel(cam.position.x, cam.position.y - .8f, cam.position.z - playerWidth))
            {
                return true;
            }
            return false;
        }
    }

    public bool left
    {
        get
        {
            if (world.CheckForVoxel(cam.position.x - playerWidth, cam.position.y - 1.8f, cam.position.z) ||
                world.CheckForVoxel(cam.position.x - playerWidth, cam.position.y - .8f, cam.position.z))
            {
                return true;
            }
            return false;
        }
    }

    public bool right
    {
        get
        {
            if (world.CheckForVoxel(cam.position.x + playerWidth, cam.position.y - 1.8f, cam.position.z) ||
                world.CheckForVoxel(cam.position.x + playerWidth, cam.position.y - .8f, cam.position.z))
            {
                return true;
            }
            return false;
        }
    }
}
