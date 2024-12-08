using UnityEngine;

public class PlaceObjectInFront : MonoBehaviour
{
    public float distanceFromCamera;
    public GameObject objectToAlign;

    void Update()
    {
        if (Camera.main != null)
        {
            objectToAlign.transform.position = Camera.main.transform.position +
                Camera.main.transform.forward * distanceFromCamera;
        }
    }
}