using UnityEngine;

public class MovedPlatform : MonoBehaviour
{
    [SerializeField] private Vector3 PositivBorderOffset;
    [SerializeField] private Vector3 NegativeBorderOffset;
    private Vector3 startPoint;

    [SerializeField, Range(-1, 1)]
    private int horizontalDirection;
    [SerializeField, Range(-1, 1)]
    private int verticalDirection;
    [SerializeField] private float speed;

    private Rigidbody rb;

    void Start()
    {
        startPoint = transform.position;
        if (horizontalDirection == 0) horizontalDirection = 1;
        if (verticalDirection == 0) verticalDirection = 1;
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    void FixedUpdate()
    {
        MovePlatform();
    }

    private void MovePlatform()
    {
        Vector3 pos = transform.position;
        Vector3 movement = Vector3.zero;

        // Горизонтальное движение
        if (horizontalDirection == 1)
        {
            float rightBound = startPoint.x + PositivBorderOffset.x;
            if (pos.x < rightBound)
            {
                movement += Vector3.right * speed * Time.fixedDeltaTime;
            }
            else
            {
                horizontalDirection = -1;
            }
        }
        else if (horizontalDirection == -1)
        {
            float leftBound = startPoint.x - NegativeBorderOffset.x;
            if (pos.x > leftBound)
            {
                movement -= Vector3.right * speed * Time.fixedDeltaTime;
            }
            else
            {
                horizontalDirection = 1;
            }
        }

        // Вертикальное движение
        if (verticalDirection == 1)
        {
            float topBound = startPoint.y + PositivBorderOffset.y;
            if (pos.y < topBound)
            {
                movement += Vector3.up * speed * Time.fixedDeltaTime;
            }
            else
            {
                verticalDirection = -1;
            }
        }
        else if (verticalDirection == -1)
        {
            float bottomBound = startPoint.y - NegativeBorderOffset.y;
            if (pos.y > bottomBound)
            {
                movement -= Vector3.up * speed * Time.fixedDeltaTime;
            }
            else
            {
                verticalDirection = 1;
            }
        }

        // Применяем движение только если есть перемещение
        if (movement != Vector3.zero)
        {
            rb.MovePosition(pos + movement);
        }
    }
}