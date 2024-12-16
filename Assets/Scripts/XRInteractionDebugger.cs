using UnityEngine;


public class XRInteractionDebugger : MonoBehaviour
{
    private Rigidbody rb;
    private Collider col;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    void Start()
    {
        // Check for Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody is missing! Add a Rigidbody component to the object.");
        }
        else
        {
            Debug.Log("Rigidbody found. Checking settings...");
            if (rb.isKinematic)
                Debug.LogWarning("Rigidbody is kinematic. Consider unchecking 'Is Kinematic' for dynamic physics.");
            else
                Debug.Log("Rigidbody is set correctly for interaction.");
        }

        // Check for Collider
        col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("Collider is missing! Add a Collider component to the object.");
        }
        else
        {
            Debug.Log("Collider found. Type: " + col.GetType());
            if (!col.enabled)
                Debug.LogWarning("Collider is disabled. Enable the Collider for interaction.");
        }

        // Check for XRGrabInteractable
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable == null)
        {
            Debug.LogError("XRGrabInteractable is missing! Add the XRGrabInteractable component to the object.");
        }
        else
        {
            Debug.Log("XRGrabInteractable is set up correctly.");
        }
    }

    void Update()
    {
        // Monitor object movement
        if (rb != null)
        {
            Debug.Log("Rigidbody velocity: " + rb.velocity + ", Position: " + transform.position);
        }
    }

    // Log when the object is hovered or grabbed
    private void OnHoverEnter()
    {
        Debug.Log("Object hovered by an interactor.");
    }

    private void OnSelectEnter()
    {
        Debug.Log("Object grabbed by an interactor.");
    }

    private void OnSelectExit()
    {
        Debug.Log("Object released by an interactor.");
    }
}
