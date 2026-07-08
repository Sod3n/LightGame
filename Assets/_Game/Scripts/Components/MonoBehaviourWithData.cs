using UnityEngine;

public abstract class MonoBehaviourWithData<T> : MonoBehaviour
{
    [SerializeField] private T data;

    public T Data
    {
        get => data;
        set => data = value;
    }
}
