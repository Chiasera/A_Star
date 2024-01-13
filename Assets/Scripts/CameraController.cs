using System.Threading.Tasks;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float movementSpeed = 5.0f;
    public float mouseSensitivity = 2.0f;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private Quaternion initialRotation;
    private Quaternion initialRotationCamera;
    private Vector3 initialPosition;
    private bool updateCameraMovement = false;

    void Awake()
    {
        // Locks the cursor to the center of the screen and hides it
        Cursor.lockState = CursorLockMode.Locked;

        // Store initial rotation
        initialRotation = transform.rotation;
        initialRotationCamera = Camera.main.transform.rotation;
        initialPosition = transform.position;
        OnGameStartWait(1000);
    }
     
    private async void OnGameStartWait(int ms)
    {
        await Task.Delay(ms);
        updateCameraMovement = true;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            ResetCameraPosition();
        }  else if(updateCameraMovement)
        {
            // Handle mouse rotation
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            yRotation += mouseX;

            //Update the camera by adjusting its xRotation
            Camera.main.transform.localRotation = Quaternion.Euler(initialRotationCamera.eulerAngles.x + xRotation, 0, 0);
            //Update the player transform by rotation arround the y axis, and with it the child Camera
            transform.localRotation = Quaternion.Euler(0, initialRotation.eulerAngles.y + yRotation, 0);

            // Handle WASD movement
            Vector3 moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).normalized;

            // Reset to initial position and apply the new translations based on keyboard input
            transform.position += Camera.main.transform.right * moveDirection.x * movementSpeed * Time.deltaTime;
            //camera is orthographic, so going forward won't change perspective, only displace the clipping planes
            Camera.main.orthographicSize -= moveDirection.z * movementSpeed * Time.deltaTime;
        }      
    }
    
    private void ResetCameraPosition()
    {
        Debug.Log(initialRotation.y);
        Camera.main.transform.localRotation = Quaternion.Euler(initialRotationCamera.eulerAngles.x, 0, 0); 
        transform.localRotation = Quaternion.Euler(0, initialRotation.eulerAngles.y, 0);
        transform.position = initialPosition;
        xRotation = 0;
        yRotation = 0;
    }
}
