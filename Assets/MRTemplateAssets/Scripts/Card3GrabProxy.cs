using UnityEngine;

public class Card3GrabProxy : MonoBehaviour
{
    public Transform cardVisual;

    void LateUpdate()
    {
        if (cardVisual != null)
        {
            cardVisual.position = transform.position;
            cardVisual.rotation = transform.rotation;
        }
    }
}