using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightRayScaller : MonoBehaviour
{
    [SerializeField] private Light2D light;
    [SerializeField] private Vector3[] shape;
    public float speed;
    public LayerMask collisionLayerMask = -1; // Слой для проверки столкновений
    public float raycastOffset = 0.1f; // Смещение для избежания самопересечения
    public float maxRayDistance = 50f; // Максимальная дистанция луча

    private bool isGrowing = true;
    private float currentGrowthDistance = 0f;
    private Transform parentTransform;
    private Collider2D[] ignoredColliders; // Коллайдеры, которые нужно игнорировать

    private void Start()
    {
        // Получаем трансформ родителя
        parentTransform = transform.parent;
        if (parentTransform == null)
        {
            parentTransform = transform; // Если родителя нет, используем текущий объект
            Debug.LogWarning("LightRayScaller: Parent transform not found, using self");
        }
        
        // Получаем все коллайдеры на этом объекте и его дочерних объектах, которые нужно игнорировать
        ignoredColliders = GetComponentsInChildren<Collider2D>();
    }

    void Update()
    {
        if (!isGrowing) return;

        // Вычисляем желаемое изменение
        float deltaGrowth = speed * Time.deltaTime;
        
        // Проверяем столкновение с помощью рейкаста
        float allowedGrowth = CheckCollision(deltaGrowth);
        
        if (allowedGrowth > 0)
        {
            // Изменяем форму света - обе точки по X
            shape[0].x += allowedGrowth;
            if (shape.Length > 1)
            {
                shape[1].x += allowedGrowth;
            }
            light.SetShapePath(shape);

            currentGrowthDistance += allowedGrowth;
        }
        else
        {
            isGrowing = false;
            Debug.Log("Light growth stopped due to collision");
        }
    }

    private float CheckCollision(float desiredGrowth)
    {
        // Вычисляем начальную точку для рейкаста из родителя
        Vector2 rayStart = parentTransform.position;
        
        // Направление луча (вправо от родителя)
        Vector2 rayDirection = parentTransform.right;
        
        // Выполняем рейкаст от родителя
        RaycastHit2D[] hits = Physics2D.RaycastAll(rayStart, rayDirection, maxRayDistance, collisionLayerMask);
        
        // Сортируем попадания по расстоянию
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        
        // Ищем первое попадание, которое не является игнорируемым коллайдером
        RaycastHit2D validHit = new RaycastHit2D();
        bool foundValidHit = false;
        
        foreach (RaycastHit2D hit in hits)
        {
            // Проверяем, не является ли коллайдер игнорируемым
            bool shouldIgnore = false;
            foreach (Collider2D ignoredCollider in ignoredColliders)
            {
                if (hit.collider == ignoredCollider)
                {
                    shouldIgnore = true;
                    break;
                }
            }
            
            if (!shouldIgnore && hit.collider != null)
            {
                validHit = hit;
                foundValidHit = true;
                break;
            }
        }
        
        // Визуализация рейкаста в редакторе
        Color rayColor = foundValidHit ? Color.red : Color.green;
        Debug.DrawRay(rayStart, rayDirection * maxRayDistance, rayColor);
        
        if (foundValidHit)
        {
            // Вычисляем расстояние от начала луча до точки столкновения
            float hitDistance = validHit.distance;
            
            // Вычисляем текущую длину света в мировых координатах
            float currentLightLength = CalculateLightLength();
            
            // Вычисляем допустимое расстояние для роста
            float allowedDistance = hitDistance - currentLightLength - raycastOffset;
            
            // Если свет уже достиг или превзошел точку столкновения, останавливаем рост
            if (currentLightLength >= hitDistance - raycastOffset)
            {
                return 0f;
            }
            
            return Mathf.Min(desiredGrowth, Mathf.Max(0, allowedDistance));
        }

        return desiredGrowth;
    }

    private float CalculateLightLength()
    {
        // Вычисляем длину света на основе его формы
        // Предполагаем, что форма - это полигон с точками в локальных координатах
        // Находим максимальную X-координату среди всех точек формы
        
        float maxX = shape[0].x;
        for (int i = 1; i < shape.Length; i++)
        {
            if (shape[i].x > maxX)
            {
                maxX = shape[i].x;
            }
        }
        
        // Преобразуем локальную длину в мировую, учитывая масштаб
        return maxX * transform.lossyScale.x;
    }

    // Метод для сброса роста (например, при перезапуске уровня)
    public void ResetGrowth()
    {
        isGrowing = true;
        currentGrowthDistance = 0f;
    }

    // Метод для получения информации о текущем состоянии
    public bool IsGrowing => isGrowing;
    public float CurrentGrowthDistance => currentGrowthDistance;
}