using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // For the New Input System

public class SliderRaycastTest : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main; // Ensure you have the main camera assigned
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame) // Detect mouse click
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue(); // Get the mouse position
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = mousePosition
            };

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                Debug.Log($"Raycast hit: {result.gameObject.name}");
            }
        }
    }
}
