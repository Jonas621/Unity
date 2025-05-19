using UnityEngine;

public class BackgroundLeftGrabProxy : MonoBehaviour
{
    public Transform cardVisual;
    public Transform grabHandle;

    private Vector3 localGrabOffset;
    private Quaternion localGrabRotation;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        if (grabHandle != null && cardVisual != null)
        {
            localGrabOffset = cardVisual.InverseTransformPoint(grabHandle.position);
            localGrabRotation = Quaternion.Inverse(cardVisual.rotation) * grabHandle.rotation;
        }

        if (cardVisual != null)
        {
            initialPosition = cardVisual.position;
            initialRotation = cardVisual.rotation;
        }
    }

    void LateUpdate()
    {
        if (grabInteractable != null && grabInteractable.isSelected && cardVisual != null)
        {
            cardVisual.position = transform.position;
            cardVisual.rotation = transform.rotation;
        }

        if (!grabInteractable.isSelected && grabHandle != null && cardVisual != null)
        {
            grabHandle.position = cardVisual.TransformPoint(localGrabOffset);
            grabHandle.rotation = cardVisual.rotation * localGrabRotation;
        }
    }

    public void ResetPosition()
    {
        if (cardVisual != null)
        {
            Transform cameraTransform = Camera.main.transform;
            Vector3 forwardOffset = cameraTransform.forward * 0.6f;
            Vector3 horizontalOffset = Vector3.left * 0.5f; // 50 cm weiter links als vorher
            Vector3 spawnPosition = cameraTransform.position + forwardOffset + horizontalOffset;
            Quaternion spawnRotation = Quaternion.LookRotation(cameraTransform.forward, Vector3.up);

            //Auskommentiert wegen besserem spawnen.
            //cardVisual.position = initialPosition;
            cardVisual.rotation = initialRotation;
            cardVisual.position = spawnPosition;
            // cardVisual.rotation = spawnRotation;
        }
        
    }
}
