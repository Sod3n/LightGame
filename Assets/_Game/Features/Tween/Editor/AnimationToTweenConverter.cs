#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Collections.Generic;
using System.Linq;

public class AnimationToTweenConverter : EditorWindow
{
    private AnimationClip animationClip;
    private string generatedCode = "";
    private Vector2 scrollPos;
    
    [MenuItem("Tools/Animation to DOTween Converter")]
    static void ShowWindow()
    {
        GetWindow<AnimationToTweenConverter>("Anim→Tween");
    }
    
    void OnGUI()
    {
        animationClip = EditorGUILayout.ObjectField("Animation Clip", animationClip, typeof(AnimationClip), false) as AnimationClip;
        
        if (GUILayout.Button("Generate Code"))
        {
            generatedCode = GenerateTweenCode(animationClip);
        }
        
        EditorGUILayout.LabelField("Generated Code:", EditorStyles.boldLabel);
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.TextArea(generatedCode, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
        
        if (GUILayout.Button("Copy to Clipboard"))
        {
            EditorGUIUtility.systemCopyBuffer = generatedCode;
        }
    }
    
    string GetObjectName(string path)
    {
        if (string.IsNullOrEmpty(path)) return "Root";
        string[] parts = path.Split('/');
        return parts[parts.Length - 1];
    }

    string SanitizeVariableName(string name)
    {
        name = name.Replace(" ", "");
        name = name.Replace("-", "");
        name = name.Replace("(", "");
        name = name.Replace(")", "");
        
        if (!string.IsNullOrEmpty(name))
        {
            name = char.ToLower(name[0]) + name.Substring(1);
        }
        
        return name;
    }
    
    string GenerateTweenCode(AnimationClip clip)
    {
        if (clip == null) return "// Select an animation clip first";
        
        var sb = new StringBuilder();
        var bindings = AnimationUtility.GetCurveBindings(clip);
        var objectMap = new Dictionary<string, ObjectInfo>();
        var propertyGroups = new Dictionary<string, List<CurveInfo>>();
        var curveFields = new List<string>(); // Track curve field declarations
        
        // Add generation header
        sb.AppendLine("// ========================================");
        sb.AppendLine("// AUTO-GENERATED CODE - DO NOT MODIFY");
        sb.AppendLine($"// Generated from: {clip.name}");
        sb.AppendLine($"// Generation time: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("// ========================================");
        sb.AppendLine();
        
        // First pass: collect all unique objects and group curves
        foreach (var binding in bindings)
        {
            if (!objectMap.ContainsKey(binding.path))
            {
                string objectName = GetObjectName(binding.path);
                string varName = SanitizeVariableName(string.IsNullOrEmpty(binding.path) ? "target" : objectName);
                
                string finalVarName = varName;
                int counter = 1;
                while (objectMap.Values.Any(o => o.VarName == finalVarName))
                {
                    finalVarName = $"{varName}{counter}";
                    counter++;
                }
                
                objectMap[binding.path] = new ObjectInfo
                {
                    Path = binding.path,
                    ObjectName = objectName,
                    VarName = finalVarName
                };
            }
            
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            string objectVarName = objectMap[binding.path].VarName;
            string groupKey = GetPropertyGroupKey(binding, objectVarName);
            
            if (!propertyGroups.ContainsKey(groupKey))
            {
                propertyGroups[groupKey] = new List<CurveInfo>();
            }
            
            propertyGroups[groupKey].Add(new CurveInfo
            {
                Binding = binding,
                Curve = curve,
                ObjectVarName = objectVarName
            });
        }
        
        // Generate SerializedFields for GameObjects
        foreach (var objInfo in objectMap.Values)
        {
            string comment = string.IsNullOrEmpty(objInfo.Path) ? "Root" : $"{objInfo.Path}";
            sb.AppendLine($"[SerializeField] private GameObject {objInfo.VarName}; // {comment}");
        }
        
        sb.AppendLine();
        
        // Generate curve fields (will be populated during tween generation)
        var curveFieldsPlaceholder = sb.Length; // Remember where to insert curve fields
        
        sb.AppendLine("public override Tween Tween { get; set; }");
        sb.AppendLine();
        sb.AppendLine("public override Tween CreateTween()");
        sb.AppendLine("{");
        
        // Null checks
        sb.AppendLine("    // Null checks");
        foreach (var objInfo in objectMap.Values)
        {
            sb.AppendLine($"    if ({objInfo.VarName} == null) {{ Debug.LogError(\"{objInfo.VarName} is not assigned in {clip.name}!\"); return DOTween.Sequence(); }}");
        }
        sb.AppendLine();
        
        sb.AppendLine("    var sequence = DOTween.Sequence();");
        sb.AppendLine($"    float duration = {clip.length}f;");
        sb.AppendLine();
        
        // Generate tweens for each property group
        foreach (var group in propertyGroups)
        {
            var result = GenerateTweenForPropertyGroup(group.Value, clip.length);
            if (!string.IsNullOrEmpty(result.tweenCode))
            {
                sb.AppendLine(result.tweenCode);
                curveFields.AddRange(result.curveDeclarations);
            }
        }
        
        sb.AppendLine();
        sb.AppendLine("    return sequence;");
        sb.AppendLine("}");
        
        // Insert curve field declarations at the placeholder position
        if (curveFields.Count > 0)
        {
            var curveSb = new StringBuilder();
            foreach (var curveField in curveFields)
            {
                curveSb.AppendLine(curveField);
            }
            curveSb.AppendLine();
            sb.Insert(curveFieldsPlaceholder, curveSb.ToString());
        }
        
        return sb.ToString();
    }

    string GetPropertyGroupKey(EditorCurveBinding binding, string objectVar)
    {
        string prop = binding.propertyName;
        
        if (prop.StartsWith("m_LocalPosition.")) return $"{objectVar}_localPos";
        if (prop.StartsWith("m_Position.")) return $"{objectVar}_worldPos";
        if (prop.StartsWith("m_LocalScale.")) return $"{objectVar}_scale";
        if (prop.StartsWith("localEulerAnglesRaw.")) return $"{objectVar}_rotation";
        if (prop.StartsWith("m_AnchoredPosition.")) return $"{objectVar}_anchoredPos";
        if (prop.StartsWith("m_SizeDelta.")) return $"{objectVar}_sizeDelta";
        if (prop.StartsWith("m_Color.")) return $"{objectVar}_color_{binding.type.Name}";
        
        return $"{objectVar}_{prop}";
    }

    (string tweenCode, List<string> curveDeclarations) GenerateTweenForPropertyGroup(List<CurveInfo> curves, float duration)
    {
        if (curves.Count == 0) return ("", new List<string>());
        
        var first = curves[0];
        string prop = first.Binding.propertyName;
        string objectVar = first.ObjectVarName;
        string componentType = first.Binding.type.Name;
        
        // GAMEOBJECT - Active State
        if (componentType == "GameObject" && prop == "m_IsActive")
        {
            var curve = curves[0].Curve;
            bool initialState = curve.Evaluate(0f) > 0.5f;
            var transitions = new List<(float time, bool state)>();
            transitions.Add((0f, initialState));
            
            for (int i = 0; i < curve.keys.Length - 1; i++)
            {
                bool val1 = curve.keys[i].value > 0.5f;
                bool val2 = curve.keys[i + 1].value > 0.5f;
                
                if (val1 != val2)
                {
                    transitions.Add((curve.keys[i + 1].time, val2));
                }
            }
            
            var sb = new StringBuilder();
            foreach (var (time, state) in transitions)
            {
                sb.AppendLine($"    sequence.InsertCallback({time}f, () => {objectVar}.SetActive({state.ToString().ToLower()}));");
            }
            
            return (sb.ToString(), new List<string>());
        }
        
        // VECTOR3 PROPERTIES
        if (prop.StartsWith("m_LocalPosition."))
            return GenerateVector3Tween(curves, objectVar, "localPosition", 
                $"{objectVar}.transform.localPosition", duration);
        
        if (prop.StartsWith("m_Position."))
            return GenerateVector3Tween(curves, objectVar, "position",
                $"{objectVar}.transform.position", duration);
        
        if (prop.StartsWith("m_LocalScale."))
            return GenerateVector3Tween(curves, objectVar, "localScale",
                $"{objectVar}.transform.localScale", duration);
        
        if (prop.StartsWith("localEulerAnglesRaw."))
            return GenerateVector3Tween(curves, objectVar, "localEulerAngles",
                $"{objectVar}.transform.localEulerAngles", duration);
        
        // VECTOR2 PROPERTIES
        if (prop.StartsWith("m_AnchoredPosition."))
            return GenerateVector2Tween(curves, objectVar, "anchoredPosition",
                $"{objectVar}.GetComponent<RectTransform>().anchoredPosition", duration);
        
        if (prop.StartsWith("m_SizeDelta."))
            return GenerateVector2Tween(curves, objectVar, "sizeDelta",
                $"{objectVar}.GetComponent<RectTransform>().sizeDelta", duration);
        
        // COLOR PROPERTIES
        if (prop.StartsWith("m_Color."))
        {
            string componentAccess = "";
            if (componentType == "Image")
                componentAccess = $"{objectVar}.GetComponent<UnityEngine.UI.Image>().color";
            else if (componentType == "SpriteRenderer")
                componentAccess = $"{objectVar}.GetComponent<SpriteRenderer>().color";
            else if (componentType == "Light")
                componentAccess = $"{objectVar}.GetComponent<Light>().color";
            
            if (!string.IsNullOrEmpty(componentAccess))
                return GenerateColorTween(curves, objectVar, componentAccess, componentType, duration);
        }
        
        // SINGLE FLOAT PROPERTIES
        return GenerateSingleFloatTween(first, duration);
    }

    (string tweenCode, List<string> curveDeclarations) GenerateVector3Tween(List<CurveInfo> curves, string objectVar, string propName, string accessor, float duration)
    {
        var xCurve = curves.FirstOrDefault(c => c.Binding.propertyName.EndsWith(".x"))?.Curve;
        var yCurve = curves.FirstOrDefault(c => c.Binding.propertyName.EndsWith(".y"))?.Curve;
        var zCurve = curves.FirstOrDefault(c => c.Binding.propertyName.EndsWith(".z"))?.Curve;
        
        var curveDeclarations = new List<string>();
        var sb = new StringBuilder();
        
        sb.AppendLine($"    // Animate {propName}");
        sb.AppendLine($"    sequence.Join(DOVirtual.Float(0f, 1f, duration, t => {{");
        sb.AppendLine($"        float time = t * duration;");
        
        string xVal = xCurve != null ? $"xCurve_{objectVar}_{propName}.Evaluate(time)" : $"{accessor}.x";
        string yVal = yCurve != null ? $"yCurve_{objectVar}_{propName}.Evaluate(time)" : $"{accessor}.y";
        string zVal = zCurve != null ? $"zCurve_{objectVar}_{propName}.Evaluate(time)" : $"{accessor}.z";
        
        sb.AppendLine($"        {accessor} = new Vector3({xVal}, {yVal}, {zVal});");
        sb.AppendLine($"    }}));");
        
        if (xCurve != null)
            curveDeclarations.Add($"[SerializeField] private AnimationCurve xCurve_{objectVar}_{propName} = {AnimationCurveToCode(xCurve)};");
        if (yCurve != null)
            curveDeclarations.Add($"[SerializeField] private AnimationCurve yCurve_{objectVar}_{propName} = {AnimationCurveToCode(yCurve)};");
        if (zCurve != null)
            curveDeclarations.Add($"[SerializeField] private AnimationCurve zCurve_{objectVar}_{propName} = {AnimationCurveToCode(zCurve)};");
        
        return (sb.ToString(), curveDeclarations);
    }

    (string tweenCode, List<string> curveDeclarations) GenerateVector2Tween(List<CurveInfo> curves, string objectVar, string propName, string accessor, float duration)
    {
        var xCurve = curves.FirstOrDefault(c => c.Binding.propertyName.EndsWith(".x"))?.Curve;
        var yCurve = curves.FirstOrDefault(c => c.Binding.propertyName.EndsWith(".y"))?.Curve;
        
        var curveDeclarations = new List<string>();
        var sb = new StringBuilder();
        
        sb.AppendLine($"    // Animate {propName}");
        sb.AppendLine($"    sequence.Join(DOVirtual.Float(0f, 1f, duration, t => {{");
        sb.AppendLine($"        float time = t * duration;");
        
        string xVal = xCurve != null ? $"xCurve_{objectVar}_{propName}.Evaluate(time)" : $"{accessor}.x";
        string yVal = yCurve != null ? $"yCurve_{objectVar}_{propName}.Evaluate(time)" : $"{accessor}.y";
        
        sb.AppendLine($"        {accessor} = new Vector2({xVal}, {yVal});");
        sb.AppendLine($"    }}));");
        
        if (xCurve != null)
            curveDeclarations.Add($"[SerializeField] private AnimationCurve xCurve_{objectVar}_{propName} = {AnimationCurveToCode(xCurve)};");
        if (yCurve != null)
            curveDeclarations.Add($"[SerializeField] private AnimationCurve yCurve_{objectVar}_{propName} = {AnimationCurveToCode(yCurve)};");
        
        return (sb.ToString(), curveDeclarations);
    }

    (string tweenCode, List<string> curveDeclarations) GenerateColorTween(List<CurveInfo> curves, string objectVar, string accessor, string componentType, float duration)
    {
        var rCurve = curves.FirstOrDefault(c => c.Binding.propertyName.EndsWith(".r"))?.Curve;
        var gCurve = curves.FirstOrDefault(c => c.Binding.propertyName.EndsWith(".g"))?.Curve;
        var bCurve = curves.FirstOrDefault(c => c.Binding.propertyName.EndsWith(".b"))?.Curve;
        var aCurve = curves.FirstOrDefault(c => c.Binding.propertyName.EndsWith(".a"))?.Curve;
        
        var curveDeclarations = new List<string>();
        var sb = new StringBuilder();
        
        sb.AppendLine($"    // Animate color");
        sb.AppendLine($"    sequence.Join(DOVirtual.Float(0f, 1f, duration, t => {{");
        sb.AppendLine($"        float time = t * duration;");
        
        string rVal = rCurve != null ? $"rCurve_{objectVar}_color.Evaluate(time)" : $"{accessor}.r";
        string gVal = gCurve != null ? $"gCurve_{objectVar}_color.Evaluate(time)" : $"{accessor}.g";
        string bVal = bCurve != null ? $"bCurve_{objectVar}_color.Evaluate(time)" : $"{accessor}.b";
        string aVal = aCurve != null ? $"aCurve_{objectVar}_color.Evaluate(time)" : $"{accessor}.a";
        
        sb.AppendLine($"        {accessor} = new Color({rVal}, {gVal}, {bVal}, {aVal});");
        sb.AppendLine($"    }}));");
        
        if (rCurve != null)
            curveDeclarations.Add($"[SerializeField] private AnimationCurve rCurve_{objectVar}_color = {AnimationCurveToCode(rCurve)};");
        if (gCurve != null)
            curveDeclarations.Add($"[SerializeField] private AnimationCurve gCurve_{objectVar}_color = {AnimationCurveToCode(gCurve)};");
        if (bCurve != null)
            curveDeclarations.Add($"[SerializeField] private AnimationCurve bCurve_{objectVar}_color = {AnimationCurveToCode(bCurve)};");
        if (aCurve != null)
            curveDeclarations.Add($"[SerializeField] private AnimationCurve aCurve_{objectVar}_color = {AnimationCurveToCode(aCurve)};");
        
        return (sb.ToString(), curveDeclarations);
    }

    (string tweenCode, List<string> curveDeclarations) GenerateSingleFloatTween(CurveInfo curveInfo, float duration)
    {
        string objectVar = curveInfo.ObjectVarName;
        string prop = curveInfo.Binding.propertyName;
        string componentType = curveInfo.Binding.type.Name;
        string curveName = $"curve_{objectVar}_{SanitizeVariableName(prop)}";
        
        var curveDeclarations = new List<string>();
        curveDeclarations.Add($"[SerializeField] private AnimationCurve {curveName} = {AnimationCurveToCode(curveInfo.Curve)};");
        
        if (componentType == "CanvasGroup" && prop == "m_Alpha")
        {
            return ($"    sequence.Join(DOVirtual.Float(0f, 1f, duration, t => {objectVar}.GetComponent<CanvasGroup>().alpha = {curveName}.Evaluate(t * duration)));", 
                    curveDeclarations);
        }
        
        if (componentType == "Image" && prop == "m_FillAmount")
        {
            return ($"    sequence.Join(DOVirtual.Float(0f, 1f, duration, t => {objectVar}.GetComponent<UnityEngine.UI.Image>().fillAmount = {curveName}.Evaluate(t * duration)));", 
                    curveDeclarations);
        }
        
        return ($"    // TODO: Handle {componentType}.{prop}", new List<string>());
    }
    
    string FormatFloatValue(float value)
    {
        if (float.IsPositiveInfinity(value))
            return "Mathf.Infinity";
        if (float.IsNegativeInfinity(value))
            return "-Mathf.Infinity";
        if (float.IsNaN(value))
            return "float.NaN";
        
        return $"{value}f";
    }
    
    string AnimationCurveToCode(AnimationCurve curve)
    {
        var sb = new StringBuilder();
        sb.Append("new AnimationCurve(");
        
        for (int i = 0; i < curve.keys.Length; i++)
        {
            var key = curve.keys[i];
            if (i > 0) sb.Append(", ");
            
            string time = FormatFloatValue(key.time);
            string value = FormatFloatValue(key.value);
            string inTangent = FormatFloatValue(key.inTangent);
            string outTangent = FormatFloatValue(key.outTangent);
            
            sb.Append($"new Keyframe({time}, {value}, {inTangent}, {outTangent})");
        }
        
        sb.Append(")");
        return sb.ToString();
    }
    
    // Helper classes
    class ObjectInfo
    {
        public string Path;
        public string ObjectName;
        public string VarName;
    }
    
    class CurveInfo
    {
        public EditorCurveBinding Binding;
        public AnimationCurve Curve;
        public string ObjectVarName;
    }
}
#endif