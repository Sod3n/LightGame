#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using DG.Tweening;
using System;
using System.Collections.Generic;

namespace Core._.UI
{
    [CustomEditor(typeof(TweenableBase), true)]
    public class TweenableEditor : Editor
    {
        private float previewTime = 0f;
        private bool isPreviewing = false;
        private bool isLooping = false;
        private bool playBackwards = false;
        private bool dontResetOnPlay = false;
        private double lastUpdateTime = 0;
        private List<Tween> playedTweens = new List<Tween>();
        
        void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Selection.selectionChanged += OnSelectionChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            Undo.undoRedoPerformed += OnUndoRedo;
            
            CleanupPreview();
        }
        
        void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Selection.selectionChanged -= OnSelectionChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            Undo.undoRedoPerformed -= OnUndoRedo;
            
            CleanupPreview();
        }
        
        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || 
                state == PlayModeStateChange.EnteredPlayMode)
            {
                CleanupPreview();
            }
        }
        
        void OnSelectionChanged()
        {
            CleanupPreview();
        }
        
        void OnBeforeAssemblyReload()
        {
            CleanupPreview();
        }
        
        void OnUndoRedo()
        {
            if (isPreviewing)
            {
                CleanupPreview();
            }
        }
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            TweenableBase tweenableTarget = (TweenableBase)target;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview Controls", EditorStyles.boldLabel);
            
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Preview is disabled in Play Mode.", MessageType.Info);
                return;
            }
            
            isLooping = EditorGUILayout.Toggle("Loop", isLooping);
            playBackwards = EditorGUILayout.Toggle("Play Backwards", playBackwards);
            dontResetOnPlay = EditorGUILayout.Toggle("Don't Reset on Play", dontResetOnPlay);
            
            if (dontResetOnPlay)
            {
                EditorGUILayout.HelpBox("Tween won't reset on Play. Use Reset button to rewind.", MessageType.Info);
            }
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button(isPreviewing ? "Stop" : "Play"))
            {
                if (isPreviewing) StopPreview();
                else StartPreview();
            }
            
            if (GUILayout.Button("Reset"))
            {
                ResetPreview();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Initialize tween if needed
            // Only reinitialize if tween doesn't exist
            if (tweenableTarget.Tween == null)
            {
                InitializeTween(tweenableTarget);
            }
            
            // Preview slider
            if (tweenableTarget.Tween != null)
            {
                EditorGUI.BeginChangeCheck();
                previewTime = EditorGUILayout.Slider("Preview Time", previewTime, 0f, tweenableTarget.Tween.Duration());
                if (EditorGUI.EndChangeCheck())
                {
                    try
                    {
                        tweenableTarget.Tween.Goto(previewTime);
                        SceneView.RepaintAll();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error during preview: {e.Message}");
                        CleanupPreview();
                    }
                }
            }
        }
        
        void InitializeTween(TweenableBase tweenableTarget)
        {
                if (!dontResetOnPlay){
                    tweenableTarget.Tween?.Kill();
                }
                var tween = tweenableTarget.CreateTween();
                if (tween == null) return;

                // Wrap in sequence with tiny delay to capture initial state
                if (tween is Sequence sequence)
                {
                    sequence.PrependInterval(0.001f);
                    tweenableTarget.Tween = sequence;
                }
                else
                {
                    var wrappedSequence = DOTween.Sequence();
                    wrappedSequence.PrependInterval(0.001f);
                    wrappedSequence.Append(tween);
                    tweenableTarget.Tween = wrappedSequence;
                }

                tweenableTarget.Tween.SetAutoKill(false);
                tweenableTarget.Tween.SetLoops(1);
                tweenableTarget.Tween.Pause();
                playedTweens.Add(tweenableTarget.Tween);
        }
        
        void StartPreview()
        {
            try
            {
                TweenableBase tweenableTarget = (TweenableBase)target;
                InitializeTween(tweenableTarget);
                
                isPreviewing = true;
                previewTime = playBackwards ? tweenableTarget.Tween.Duration() : 0f;
                lastUpdateTime = EditorApplication.timeSinceStartup;
                EditorApplication.update += UpdatePreview;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error starting preview: {e.Message}");
                CleanupPreview();
            }
        }
        
        void StopPreview()
        {
            isPreviewing = false;
            EditorApplication.update -= UpdatePreview;
            previewTime = 0f;
            if(!dontResetOnPlay) RewindAllTweens();
            Repaint();
        }
        
        void ResetPreview()
        {
            try
            {
                StopPreview();
                RewindAllTweens();
                SceneView.lastActiveSceneView?.Repaint();
                Repaint();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error resetting preview: {e.Message}");
                CleanupPreview();
            }
        }
        
        void UpdatePreview()
        {
            if (!isPreviewing) return;
            
            try
            {
                var tweenableTarget = (TweenableBase)target;
                var tween = tweenableTarget?.Tween;
                if (tween == null || !tween.IsActive()) return;
                
                double currentTime = EditorApplication.timeSinceStartup;
                float deltaTime = (float)(currentTime - lastUpdateTime);
                lastUpdateTime = currentTime;
                
                if (playBackwards)
                {
                    previewTime -= deltaTime;
                    if (previewTime < 0f)
                    {
                        if (isLooping)
                        {
                            previewTime = tween.Duration();
                        }
                        else
                        {
                            previewTime = 0f;
                            if (dontResetOnPlay)
                            {
                                StopPreview();
                                return;
                            }
                        }
                    }
                }
                else
                {
                    previewTime += deltaTime;
                    if (previewTime > tween.Duration())
                    {
                        if (isLooping)
                        {
                            previewTime = 0f;
                        }
                        else
                        {
                            previewTime = tween.Duration();
                            if (dontResetOnPlay)
                            {
                                StopPreview();
                                return;
                            }
                        }
                    }
                }
                
                tween.GotoWithCallbacks(previewTime);
                SceneView.lastActiveSceneView?.Repaint();
                Repaint();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during preview update: {e.Message}");
                CleanupPreview();
            }
        }
        
        void RevertTween()
        {
            try
            {
                TweenableBase tweenableTarget = (TweenableBase)target;
                if (tweenableTarget != null && tweenableTarget.Tween != null && tweenableTarget.Tween.IsActive())
                {
                    // Rewind reverts all tweened values to their starting state
                    tweenableTarget.Tween.Rewind();
                    previewTime = 0f;
                    SceneView.lastActiveSceneView?.Repaint();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reverting tween: {e.Message}");
            }
        }
        
        void RewindAllTweens()
        {
            try
            {
                foreach (var tween in playedTweens)
                {
                    if (tween != null && tween.IsActive())
                    {
                        tween.Rewind();
                    }
                }
                
                RevertTween();
                playedTweens.Clear();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error rewinding tweens: {e.Message}");
            }
        }
        
        void CleanupPreview()
        {
            try
            {
                isPreviewing = false;
                EditorApplication.update -= UpdatePreview;
                
                RewindAllTweens();
                
                TweenableBase tweenableTarget = (TweenableBase)target;
                if (tweenableTarget != null && tweenableTarget.Tween != null)
                {
                    tweenableTarget.Tween.Kill();
                    tweenableTarget.Tween = null;
                }
                
                previewTime = 0f;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during cleanup: {e.Message}");
            }
        }
    }
}
#endif