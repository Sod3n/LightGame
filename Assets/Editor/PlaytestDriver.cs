using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

// Lets agents drive the editor from outside: reads Temp/playtest/cmd.txt, writes Temp/playtest/out.txt.
[InitializeOnLoad]
public static class PlaytestDriver
{
    static readonly string Dir = Path.GetFullPath("Temp/playtest");
    static readonly string CmdPath = Path.Combine(Dir, "cmd.txt");
    static readonly string OutPath = Path.Combine(Dir, "out.txt");
    static readonly string StatusPath = Path.Combine(Dir, "status.txt");
    static readonly List<string> Logs = new();
    static readonly Dictionary<Key, double> Held = new();
    static Keyboard _keyboard;
    static InputSettings.EditorInputBehaviorInPlayMode? _savedEditorBehavior;
    static InputSettings.BackgroundBehavior _savedBackground;
    static int _pendingSteps;
    static double _lastBeat;

    static PlaytestDriver()
    {
        Directory.CreateDirectory(Dir);
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        Application.logMessageReceivedThreaded -= OnLog;
        Application.logMessageReceivedThreaded += OnLog;
    }

    static void OnLog(string msg, string stack, LogType type)
    {
        if (type == LogType.Log || type == LogType.Warning) return;
        lock (Logs) Logs.Add($"[{type}] {msg}\n{string.Join("\n", stack.Split('\n').Take(6))}");
    }

    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingPlayMode) ReleaseKeyboard();
    }

    static void Tick()
    {
        if (Held.Count > 0 && Held.Values.Any(t => t <= EditorApplication.timeSinceStartup))
        {
            foreach (var k in Held.Where(p => p.Value <= EditorApplication.timeSinceStartup).Select(p => p.Key).ToList()) Held.Remove(k);
            PushKeys();
        }

        if (_pendingSteps > 0 && EditorApplication.isPlaying && EditorApplication.isPaused)
        {
            _pendingSteps--;
            EditorApplication.Step();
        }

        if (EditorApplication.timeSinceStartup - _lastBeat > 1)
        {
            _lastBeat = EditorApplication.timeSinceStartup;
            File.WriteAllText(StatusPath, Status() + "\n");
        }

        if (!File.Exists(CmdPath)) return;
        var lines = File.ReadAllLines(CmdPath);
        File.Delete(CmdPath);
        var sb = new StringBuilder();
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            var sp = line.IndexOf(' ');
            var cmd = sp < 0 ? line : line[..sp];
            var arg = sp < 0 ? "" : line[(sp + 1)..].Trim();
            sb.AppendLine($"> {line}");
            try { sb.AppendLine(Run(cmd, arg)); }
            catch (Exception e) { sb.AppendLine("ERROR " + (e is TargetInvocationException { InnerException: { } inner } ? inner : e)); }
        }
        File.AppendAllText(OutPath, sb.ToString());
    }

    static string Status()
    {
        var scenes = string.Join(",", Enumerable.Range(0, SceneManager.sceneCount).Select(i => SceneManager.GetSceneAt(i).name));
        var s = $"{DateTime.Now:HH:mm:ss} playing={EditorApplication.isPlaying} paused={EditorApplication.isPaused} compiling={EditorApplication.isCompiling} " +
                $"frame={Time.frameCount} timescale={Time.timeScale:0.##} scenes={scenes}";
        if (EditorApplication.isPlaying && UnityEngine.Object.FindFirstObjectByType<PlayerMain>() is { } player)
        {
            var p = player.transform.position;
            s += $" player=({p.x:F2},{p.y:F2}) state={player.CurrentState}";
        }
        if (Held.Count > 0) s += $" held={string.Join("+", Held.Keys)}";
        return s;
    }

    static string Run(string cmd, string arg)
    {
        switch (cmd)
        {
            case "play":
                if (EditorApplication.isPlaying) return "already playing";
                if (arg.Length > 0) OpenScene(arg);
                EditorApplication.EnterPlaymode();
                return "entering play mode from " + SceneManager.GetActiveScene().path;
            case "stop":
                EditorApplication.ExitPlaymode();
                return "exiting play mode";
            case "open":
                if (EditorApplication.isPlaying) throw new Exception("stop play mode first");
                return "opened " + OpenScene(arg);
            case "pause":
                EditorApplication.isPaused = arg != "0";
                return $"paused={EditorApplication.isPaused}";
            case "step":
                if (!EditorApplication.isPaused) throw new Exception("pause first");
                _pendingSteps += arg.Length > 0 ? int.Parse(arg) : 1;
                return $"stepping {_pendingSteps} frame(s)";
            case "timescale":
                Time.timeScale = ParseFloat(arg);
                return $"timescale={Time.timeScale}";
            case "press":
            {
                if (!EditorApplication.isPlaying) throw new Exception("not playing");
                var parts = arg.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var seconds = parts.Length > 1 ? ParseFloat(parts[1]) : 0.1f;
                var keys = parts[0].Split('+').Select(ParseKey).ToList();
                foreach (var k in keys) Held[k] = EditorApplication.timeSinceStartup + seconds;
                PushKeys();
                return $"holding {string.Join("+", keys)} for {seconds}s";
            }
            case "release":
                Held.Clear();
                if (_keyboard != null) PushKeys();
                return "released all keys";
            case "refresh":
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                return "refreshed";
            case "invoke":
            {
                var dot = arg.LastIndexOf('.');
                var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(arg[..dot])).FirstOrDefault(t => t != null)
                           ?? throw new Exception("type not found: " + arg[..dot]);
                var method = type.GetMethod(arg[(dot + 1)..], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                             ?? throw new Exception("static method not found: " + arg);
                return method.Invoke(null, null)?.ToString() ?? "invoked";
            }
            case "find":
                return string.Join("\n", AllActive().Where(g => g.name.IndexOf(arg, StringComparison.OrdinalIgnoreCase) >= 0)
                    .Take(60).Select(PathOf));
            case "dump":
            {
                var sb = new StringBuilder();
                Dump(Find(arg).transform, 0, sb);
                return sb.ToString();
            }
            case "inspect":
            {
                var at = arg.LastIndexOf('@');
                var go = Find(at < 0 ? arg : arg[..at]);
                var comps = go.GetComponents<Component>().Where(c => c != null);
                if (at >= 0) comps = comps.Where(c => c.GetType().Name.Equals(arg[(at + 1)..], StringComparison.OrdinalIgnoreCase));
                var sb = new StringBuilder();
                foreach (var c in comps) Inspect(c, sb);
                return sb.Length > 0 ? sb.ToString() : "no such component on " + PathOf(go);
            }
            case "select":
            {
                var go = Find(arg);
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
                return "selected " + PathOf(go);
            }
            case "setactive":
            {
                var i = arg.LastIndexOf(' ');
                var go = Find(arg[..i]);
                go.SetActive(arg[(i + 1)..] == "1");
                return $"{PathOf(go)} active={go.activeSelf}";
            }
            case "shot":
            {
                if (!EditorApplication.isPlaying) throw new Exception("shot captures the Game view and needs play mode");
                var (path, scale) = (arg, 2);
                var i = arg.LastIndexOf(' ');
                if (i > 0 && int.TryParse(arg[(i + 1)..], out var n)) (path, scale) = (arg[..i], n);
                ScreenCapture.CaptureScreenshot(path, scale);
                return "screenshot queued " + path;
            }
            case "logs":
                lock (Logs)
                {
                    var s = Logs.Count == 0 ? "(no errors)" : string.Join("\n---\n", Logs);
                    Logs.Clear();
                    return s;
                }
            case "status":
                return Status();
            default:
                return "unknown command";
        }
    }

    static string OpenScene(string nameOrPath)
    {
        var path = nameOrPath.EndsWith(".unity") ? nameOrPath
            : AssetDatabase.FindAssets("t:Scene " + nameOrPath).Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == nameOrPath)
              ?? throw new Exception("scene not found: " + nameOrPath);
        var dirty = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s => s.isDirty).Select(s => s.name).ToList();
        if (dirty.Count > 0) throw new Exception($"unsaved changes in {string.Join(", ", dirty)}; save or discard them in the editor first");
        EditorSceneManager.OpenScene(path);
        return path;
    }

    // Keys go to a dedicated virtual keyboard so the user's real one doesn't fight it, and
    // focus rules are lifted so input reaches the game while the editor is in the background.
    static void PushKeys()
    {
        if (_keyboard == null)
        {
            _keyboard = InputSystem.AddDevice<Keyboard>("PlaytestKeyboard");
            _savedEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            _savedBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        }
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Held.Keys.ToArray()));
    }

    static void ReleaseKeyboard()
    {
        Held.Clear();
        _pendingSteps = 0;
        if (_keyboard == null) return;
        InputSystem.RemoveDevice(_keyboard);
        _keyboard = null;
        if (_savedEditorBehavior is { } behavior) InputSystem.settings.editorInputBehaviorInPlayMode = behavior;
        InputSystem.settings.backgroundBehavior = _savedBackground;
        _savedEditorBehavior = null;
    }

    static Key ParseKey(string s) => s.ToLowerInvariant() switch
    {
        "left" => Key.LeftArrow,
        "right" => Key.RightArrow,
        "up" => Key.UpArrow,
        "down" => Key.DownArrow,
        "esc" => Key.Escape,
        "shift" => Key.LeftShift,
        "ctrl" => Key.LeftCtrl,
        _ when s.Length == 1 && char.IsDigit(s[0]) => Enum.Parse<Key>("Digit" + s),
        _ => Enum.TryParse<Key>(s, true, out var k) ? k : throw new Exception("unknown key: " + s),
    };

    static float ParseFloat(string s) => float.Parse(s, CultureInfo.InvariantCulture);

    static IEnumerable<GameObject> AllActive() =>
        Resources.FindObjectsOfTypeAll<Transform>()
            .Where(t => t.gameObject.scene.IsValid() && t.gameObject.activeInHierarchy && t.hideFlags == HideFlags.None)
            .Select(t => t.gameObject);

    // "path#n" picks the n-th match in hierarchy order, for duplicates like "Spikes/Spike#2".
    static GameObject Find(string pathOrName)
    {
        var index = -1;
        var hash = pathOrName.LastIndexOf('#');
        if (hash > 0 && int.TryParse(pathOrName[(hash + 1)..], out index)) pathOrName = pathOrName[..hash];
        var all = Resources.FindObjectsOfTypeAll<Transform>().Where(t => t.gameObject.scene.IsValid() && t.hideFlags == HideFlags.None).Select(t => t.gameObject).ToList();
        var hits = all.Where(g => PathOf(g) == pathOrName || PathOf(g).EndsWith("/" + pathOrName)).ToList();
        if (hits.Count > 1 && index < 0) hits = hits.Where(g => g.activeInHierarchy).ToList();
        if (index >= 0) hits = hits.OrderBy(SiblingKey).Skip(index).Take(1).ToList();
        if (hits.Count != 1) throw new Exception($"{hits.Count} matches for '{pathOrName}': {string.Join(" | ", hits.Take(5).Select(PathOf))}");
        return hits[0];
    }

    static string SiblingKey(GameObject g)
    {
        var s = "";
        for (var t = g.transform; t != null; t = t.parent) s = t.GetSiblingIndex().ToString("D4") + "/" + s;
        return g.scene.name + ":" + s;
    }

    static string PathOf(GameObject g)
    {
        var t = g.transform;
        var s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }

    static void Dump(Transform t, int depth, StringBuilder sb)
    {
        var go = t.gameObject;
        var p = t.position;
        var line = new StringBuilder($"{new string(' ', depth * 2)}{t.name}{(go.activeSelf ? "" : " (off)")}  pos=({p.x:F2},{p.y:F2})");
        var comps = go.GetComponents<Component>().Where(c => c != null && c is not Transform).ToList();
        if (comps.Count > 0) line.Append($"  [{string.Join(", ", comps.Select(c => c.GetType().Name))}]");
        if (go.GetComponent<SpriteRenderer>() is { } sr)
            line.Append($"  sprite={(sr.sprite ? sr.sprite.name : "null")} color={Hex(sr.color)}{(sr.flipX ? " flipX" : "")}");
        if (go.GetComponent<Animator>() is { runtimeAnimatorController: not null } anim && anim.isActiveAndEnabled && EditorApplication.isPlaying)
        {
            var clip = anim.GetCurrentAnimatorClipInfo(0).FirstOrDefault().clip;
            line.Append($"  anim={(clip ? clip.name : "none")}@{anim.GetCurrentAnimatorStateInfo(0).normalizedTime:F2}");
        }
        foreach (var c in comps)
        {
            var type = c.GetType();
            if (type.Name == "Light2D")
                line.Append($"  light={Hex((Color)type.GetProperty("color")!.GetValue(c))} intensity={type.GetProperty("intensity")!.GetValue(c)}");
            else if (type.Name is "TextMeshProUGUI" or "TextMeshPro")
                line.Append($"  text=\"{type.GetProperty("text")?.GetValue(c)}\"");
        }
        sb.AppendLine(line.ToString());
        if (depth < 6)
            foreach (Transform ch in t) Dump(ch, depth + 1, sb);
    }

    static void Inspect(Component c, StringBuilder sb)
    {
        sb.AppendLine($"[{c.GetType().Name}]");
        var so = new SerializedObject(c);
        var it = so.GetIterator();
        var enter = true;
        var count = 0;
        while (it.NextVisible(enter) && count++ < 150)
        {
            enter = it.depth < 1 && it.propertyType == SerializedPropertyType.Generic && !it.isArray;
            if (it.propertyPath == "m_Script") continue;
            sb.AppendLine($"  {it.propertyPath} = {Value(it)}");
        }
    }

    static string Value(SerializedProperty p) => p.propertyType switch
    {
        SerializedPropertyType.Integer => p.intValue.ToString(),
        SerializedPropertyType.Boolean => p.boolValue.ToString(),
        SerializedPropertyType.Float => p.floatValue.ToString("0.###", CultureInfo.InvariantCulture),
        SerializedPropertyType.String => $"\"{p.stringValue}\"",
        SerializedPropertyType.Color => Hex(p.colorValue),
        SerializedPropertyType.Enum => p.enumValueIndex >= 0 && p.enumValueIndex < p.enumDisplayNames.Length ? p.enumDisplayNames[p.enumValueIndex] : p.intValue.ToString(),
        SerializedPropertyType.ObjectReference => p.objectReferenceValue ? $"{p.objectReferenceValue.name} ({p.objectReferenceValue.GetType().Name})" : "null",
        SerializedPropertyType.Vector2 => p.vector2Value.ToString(),
        SerializedPropertyType.Vector3 => p.vector3Value.ToString(),
        SerializedPropertyType.LayerMask => p.intValue.ToString(),
        _ when p.isArray => $"[{p.arraySize} items]",
        _ => $"({p.type})",
    };

    static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGBA(c);
}
