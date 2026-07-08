using UnityEngine;

public class Mirror : MonoBehaviour
{
    [SerializeField] private GameObject ReflectableLight;
    [SerializeField] private float angle;

    public void Reflect()
    {
        ReflectableLight.gameObject.SetActive(true);
    }
}
