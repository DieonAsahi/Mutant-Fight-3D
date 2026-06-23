using UnityEngine;
interface IInteractable
{
    public void Interact();
}
public class Interactable : MonoBehaviour
{
    public GameObject player;
    public Transform holdPos;

    public float throwForce = 500f;
    public float pickUpRange = 5f;
    public float moveSpeed = 10f;
    public float rotationSensitivity = 3f;

    private GameObject heldObj;
    private Rigidbody heldObjRb;
    private bool canDrop = true;
    [SerializeField] private PlayerInputHandler inputHandler;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldObj == null)
            {
                TryPickUp();
            }
            else
            {
                if (canDrop)
                {
                    DropObject();
                }
            }
        }

        if (heldObj != null)
        {
            MoveObject();
            RotateObject();

            if (Input.GetKeyDown(KeyCode.Mouse0) && canDrop)
            {
                ThrowObject();
            }
        }
    }

    void TryPickUp()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickUpRange))
        {
            if (hit.transform.CompareTag("pickup"))
            {
                PickUpObject(hit.transform.gameObject);
            }
        }
    }

    void PickUpObject(GameObject pickUpObj)
    {
        heldObj = pickUpObj;
        heldObjRb = heldObj.GetComponent<Rigidbody>();

        heldObjRb.useGravity = false;
        heldObjRb.linearDamping = 10;

        Physics.IgnoreCollision(
            heldObj.GetComponent<Collider>(),
            player.GetComponent<Collider>(),
            true
        );
    }

    void MoveObject()
    {
        Vector3 direction = holdPos.position - heldObj.transform.position;
        heldObjRb.linearVelocity = direction * moveSpeed;
    }

    void DropObject()
    {
        Physics.IgnoreCollision(
            heldObj.GetComponent<Collider>(),
            player.GetComponent<Collider>(),
            false
        );

        heldObjRb.useGravity = true;
        heldObjRb.linearDamping = 1;

        heldObj = null;
    }

    void ThrowObject()
    {
        Physics.IgnoreCollision(
            heldObj.GetComponent<Collider>(),
            player.GetComponent<Collider>(),
            false
        );

        heldObjRb.useGravity = true;
        heldObjRb.linearDamping = 1;

        heldObjRb.AddForce(transform.forward * throwForce);

        heldObj = null;
    }

    void RotateObject()
    {
        if (inputHandler.RotateObjectTriggered)
        {
            canDrop = false;

            Vector2 mouseDelta = inputHandler.RotationInput;

            float XaxisRotation = mouseDelta.x * rotationSensitivity;
            float YaxisRotation = mouseDelta.y * rotationSensitivity;

            heldObj.transform.Rotate(Vector3.down, XaxisRotation);
            heldObj.transform.Rotate(Vector3.right, YaxisRotation);
        }
        else
        {
            canDrop = true;
        }
    }
}
