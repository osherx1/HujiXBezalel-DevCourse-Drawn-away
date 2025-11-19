using System;
using System.IO;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

namespace Drawing.Utilities
{
    // Attach this to a GameObject to capture raw input events from the New Input System
    // and write them to Console and a log file. Useful for diagnosing tablet/pen issues.
    public class InputRawTrace : MonoBehaviour
    {
        [Header("Output")]
        [SerializeField] private bool logToConsole = false;
        [SerializeField] private bool writeToFile = true;
        [SerializeField] private string fileName = "input_trace.log";

        private string _filePath;
        private StreamWriter _writer;

#if ENABLE_INPUT_SYSTEM
        private void OnEnable()
        {
            if (writeToFile)
            {
                _filePath = Path.Combine(Application.persistentDataPath, fileName);
                try
                {
                    _writer = new StreamWriter(_filePath, append: false) { AutoFlush = true };
                    _writer.WriteLine($"[InputRawTrace] Start {DateTime.Now:O}");
                    _writer.WriteLine($"persistentDataPath: {Application.persistentDataPath}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"InputRawTrace: Failed to open log file. {e.Message}", this);
                    _writer = null;
                }
            }
            InputSystem.onEvent += OnEvent;
        }

        private void OnDisable()
        {
            InputSystem.onEvent -= OnEvent;
            try { _writer?.Flush(); _writer?.Dispose(); } catch { /* ignore */ }
            _writer = null;
        }

        private void OnEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (!eventPtr.valid || device == null)
                return;

            // Filter to relevant devices
            if (!(device is Pen || device is Mouse || device is Touchscreen))
                return;

            string msg = BuildSummary(eventPtr, device);
            // if (logToConsole)
            //     Debug.Log(msg, this);
            if (_writer != null)
                _writer.WriteLine(msg);
        }

        private static string BuildSummary(InputEventPtr evt, InputDevice dev)
        {
            string header = $"[InputRawTrace] t={evt.time:F3} type={evt.type} dev={dev.layout}/{dev.displayName} id={dev.deviceId}";

            try
            {
                if (dev is Pen pen)
                {
                    Vector2 pos = SafeRead(() => pen.position.ReadValue());
                    float pressure = SafeRead(() => pen.pressure.ReadValue());
                    bool tip = SafeRead(() => pen.tip.isPressed);
                    bool inRange = SafeRead(() => pen.inRange != null && pen.inRange.isPressed);
                    return $"{header} pen pos={pos} pressure={pressure:F2} tip={tip} inRange={inRange}";
                }
                if (dev is Mouse mouse)
                {
                    Vector2 pos = SafeRead(() => mouse.position.ReadValue());
                    bool left = SafeRead(() => mouse.leftButton.isPressed);
                    return $"{header} mouse pos={pos} left={left}";
                }
                if (dev is Touchscreen ts)
                {
                    if (ts.touches.Count > 0)
                    {
                        var t = ts.touches[0];
                        Vector2 pos = SafeRead(() => t.position.ReadValue());
                        bool pressed = SafeRead(() => t.press.isPressed);
                        return $"{header} touch pos={pos} pressed={pressed}";
                    }
                    return $"{header} touch (no active touches)";
                }
            }
            catch (Exception e)
            {
                return $"{header} error={e.Message}";
            }

            return header;
        }

        private static T SafeRead<T>(Func<T> read)
        {
            try { return read(); } catch { return default; }
        }
#else
        private void OnEnable()
        {
            Debug.LogWarning("InputRawTrace requires the New Input System to be enabled.", this);
        }
#endif
    }
}
