using System.Collections.Generic;
using System.Linq;
using Light_and_controller.Scripts.Systems;
using UnityEngine;
using UnityEngine.Events;

namespace Light_and_controller.Scripts.Components
{
    [RequireComponent(typeof(Collider2D))]
    public class LightDetector : MonoBehaviour
   {
       [SerializeField] private UnityEvent<bool> onChangeState = new UnityEvent<bool>();
       [SerializeField] private UnityEvent<bool> onChangeStateInverse = new UnityEvent<bool>();

       // ToForce - extension
       // enum: Weak, Strong light
       
       public HashSet<GameObject> lightSprings = new();
       
       // Track light sources by type
       private Dictionary<LightType, HashSet<GameObject>> _lightSourcesByType = new Dictionary<LightType, HashSet<GameObject>>
       {
           { LightType.Default, new HashSet<GameObject>() },
           { LightType.LevelChange, new HashSet<GameObject>() }
       };

       // Track the active trigger for LevelChange light type
       private GameObject _activeLevelChangeTrigger;

       // Public accessors for events
       public UnityEvent<bool> OnChangeState => onChangeState;
       public UnityEvent<bool> OnChangeStateInverse => onChangeStateInverse;

       private bool _lastStateIsInLight;
       private bool _isInLight;
       private Dictionary<LightType, bool> _lastStateByType = new Dictionary<LightType, bool>
       {
           { LightType.Default, false },
           { LightType.LevelChange, false }
       };
   
       private void Update()
       {
           _isInLight = lightSprings.Count > 0;
           
           // Check each light type separately
           foreach (var lightType in _lightSourcesByType.Keys)
           {
               bool isInLightOfType = _lightSourcesByType[lightType].Count > 0;
               
               if (isInLightOfType != _lastStateByType[lightType])
               {
                   Light_and_controller.Scripts.SceneName? targetScene = null;

                   // If this is a LevelChange light type, get the target scene from the trigger
                   if (lightType == LightType.LevelChange && _activeLevelChangeTrigger != null)
                   {
                       var trigger = _activeLevelChangeTrigger.GetComponent<Trigger>();
                       if (trigger != null)
                       {
                           if (trigger.UseNextScene)
                           {
                               // Get next scene from LevelOrder
                               targetScene = Light_and_controller.Scripts.GD.LevelOrder?.GetNextScene();
                           }
                           else
                           {
                               // Use the specified target scene
                               targetScene = trigger.TargetScene;
                           }
                       }
                   }

                   EventBus.Publish(gameObject, new LightChangeEvent(isInLightOfType, lightType, targetScene));
                   _lastStateByType[lightType] = isInLightOfType;
               }
           }
           
           // Publish general light change event
           if(_isInLight == _lastStateIsInLight) return;
           EventBus.Publish(gameObject, new LightChangeEvent(_isInLight, null));
           onChangeState?.Invoke(_isInLight);
           onChangeStateInverse?.Invoke(!_isInLight);
           _lastStateIsInLight = _isInLight;
       }
       
       /// <summary>
       /// Add a light source with its type
       /// </summary>
       public void AddLightSource(GameObject lightSource, LightType lightType)
       {
           lightSprings.Add(lightSource);
           _lightSourcesByType[lightType].Add(lightSource);

           // Track the active LevelChange trigger
           if (lightType == LightType.LevelChange)
           {
               _activeLevelChangeTrigger = lightSource;
           }
       }
       
       /// <summary>
       /// Remove a light source with its type
       /// </summary>
       public void RemoveLightSource(GameObject lightSource, LightType lightType)
       {
           lightSprings.Remove(lightSource);
           _lightSourcesByType[lightType].Remove(lightSource);

           // Clear the active LevelChange trigger if it's being removed
           if (lightType == LightType.LevelChange && _activeLevelChangeTrigger == lightSource)
           {
               _activeLevelChangeTrigger = null;
           }
       }
       
       /// <summary>
       /// Check if detector is in light of a specific type
       /// </summary>
       public bool IsInLightOfType(LightType lightType)
       {
           return _lightSourcesByType[lightType].Count > 0;
       }
       
       public bool LightBlockCheck(Vector3 targetPosition, Rigidbody2D lightRigidbody) 
       {
//           Debug.Log(lightRigidbody);
           
           Vector3 direction = targetPosition - transform.position;
   
           var mask = LayerMask.GetMask("LightSource", "Ground");
           var filter = new ContactFilter2D();
           filter.SetLayerMask(mask);
           filter.useTriggers = true;
           var results = new List<RaycastHit2D>();
           Physics2D.Raycast(transform.position, direction.normalized, filter, results, direction.magnitude);
           var hit = results.FirstOrDefault(x => !x.collider.transform.CompareTag("PassLight"));
           if(hit.collider == null) return false;
           Debug.DrawLine(transform.position, hit.transform.position);
           return hit.collider.CompareTag("Light");
       }
   }
}