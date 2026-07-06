using UnityEngine;
using UnityEngine.InputSystem; 

public class FreelookCamera : MonoBehaviour
{
    public float movementSpeed = 100f;
    public float shiftSpeedMultiplier = 2.5f;
    public float lookSensitivity = 0.05f; 

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        Vector3 currentRotation = transform.localRotation.eulerAngles;
        rotationX = currentRotation.y;
        rotationY = currentRotation.x > 180 ? 360 - currentRotation.x : -currentRotation.x;
    }

    void Update()
    {
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;

        if (mouse == null || keyboard == null) return;

        if (mouse.rightButton.isPressed)
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            Vector2 mouseDelta = mouse.delta.ReadValue();
            rotationX += mouseDelta.x * lookSensitivity;
            rotationY += mouseDelta.y * lookSensitivity;
            rotationY = Mathf.Clamp(rotationY, -90f, 90f);
            transform.localRotation = Quaternion.Euler(-rotationY, rotationX, 0f);
        }
        else
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        float currentSpeed = movementSpeed;
        if (keyboard.leftShiftKey.isPressed) currentSpeed *= shiftSpeedMultiplier;

        Vector3 moveDirection = Vector3.zero;
        if (keyboard.wKey.isPressed) moveDirection += transform.forward;
        if (keyboard.sKey.isPressed) moveDirection -= transform.forward;
        if (keyboard.aKey.isPressed) moveDirection -= transform.right;
        if (keyboard.dKey.isPressed) moveDirection += transform.right;
        if (keyboard.eKey.isPressed) moveDirection += transform.up;
        if (keyboard.qKey.isPressed) moveDirection -= transform.up;

        if (moveDirection != Vector3.zero)
        {
            transform.position += moveDirection.normalized * currentSpeed * Time.deltaTime;
        }
    }
}