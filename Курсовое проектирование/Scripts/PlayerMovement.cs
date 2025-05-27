using Photon.Pun;
using UnityEngine;
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float gravity = -9.81f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 100f;
    public Transform playerCamera;
    public float maxCameraAngle = 80f;

  /*  [Header("Ground Check")]
    public float groundCheckDistance = 0.4f; // Расстояние для проверки земли
    public LayerMask groundMask; // Слой для проверки земли
    public Transform groundCheck; // Точка для проверки (разместите у ног персонажа)
*/

    private Animator animator;
    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;
    private bool isGrounded;

    PhotonView View;

    //private PlayerInventory inventory;

    void Start()
    {
        View = GetComponent<PhotonView>();
        //inventory = GetComponent<PlayerInventory>();
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        
        if (!View.IsMine)
        {
            playerCamera.gameObject.SetActive(false);
            //GetComponent<PlayerInput>().enabled = false;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
    }
 /*   void OnDrawGizmos()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckDistance);
    }*/
    void Update()
    {
        if (View.IsMine)
        {
            isGrounded = Physics.Raycast(
                transform.position,
                Vector3.down,
                0.01f,
                LayerMask.GetMask("Ground")
            ) && controller.isGrounded;


            /*if (!inventory.IsEmpty())
            {
                //Debug.Log(inventory.isEmpty());
                animator.SetBool("hasWeapon", true);
            }
            else
            {
                animator.SetBool("hasWeapon", false);
            }
            if (Input.GetKey(KeyCode.G))
            {
                inventory.Drop(transform.position);
                animator.SetBool("hasWeapon", false);
            }*/
            HandleMouseLook();
            HandleMovement();
            HandleJump();
        }
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        if (Input.GetMouseButton(1))
        {
            animator.SetBool("isAiming", true);
        }
        else
        {
            animator.SetBool("isAiming", false);
        }

            transform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxCameraAngle, maxCameraAngle);
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            animator.SetBool("isFalling", true);
            animator.SetBool("isWalking", false);
            velocity.y = -2f;
        }
        else
        {

            animator.SetBool("isFalling", false);
        }
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        var moveDirection = new Vector3(horizontal, 0, vertical).normalized;

        if (moveDirection.magnitude >= 0.1f)
        {
            animator.SetBool("isWalking", true);

            if (Input.GetKey(KeyCode.LeftShift))
            {
                controller.Move(move * moveSpeed * 2 * Time.deltaTime);

            }
            else
            {
                controller.Move(move * moveSpeed * Time.deltaTime);
            }
        }
        else{
            animator.SetBool("isWalking", false);
        }
    }

    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}