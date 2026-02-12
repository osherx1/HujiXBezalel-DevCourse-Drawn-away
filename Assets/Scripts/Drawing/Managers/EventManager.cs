using System;
using Utilities.Camera.CameraShake;

namespace Drawing.Managers
{
    public sealed class EventManager
    {
        private static readonly Lazy<EventManager> _instance = new Lazy<EventManager>(() => new EventManager());

        public static EventManager Instance => _instance.Value;

        // Private constructor prevents external instantiation
        private EventManager() { }

        // Example: add your event handling methods here
        // public event Action<MyEventArgs> OnSomething;
        // public void RaiseSomething(MyEventArgs args) => OnSomething?.Invoke(args);

        public event Action OnEraserActive;
        public event Action<object> OnConfigButtonSelected;
        public event Action<bool> OnDropPlayerToTheHole;
        public event Action OnGameFinished;
        public event Action OnBoardReset; // Triggered when ResetBoard clears all lines

        public event Action<bool> OnSlowMotionChanged;
        public event Action<bool> OnGamePausedChanged;
        public event Action<ShakeProfile> OnCameraShakeRequested;
        /// <summary>
        /// Triggers the slow motion state.
        /// </summary>
        /// <param name="active">True to enter slow motion, False to return to normal.</param>
        public void TriggerSlowMotion(bool active)
        {
            OnSlowMotionChanged?.Invoke(active);
        }


        public void TriggerGamePaused(bool isPaused)
        {
            OnGamePausedChanged?.Invoke(isPaused);
        }
    
        public void TriggerEraserActive() => OnEraserActive?.Invoke();

        public event Action OnEraserInactive;
        public void TriggerEraserInactive() => OnEraserInactive?.Invoke();

        public void TriggerConfigButtonSelected(object senderButton)
        {
            OnConfigButtonSelected?.Invoke(senderButton);
        }
        public void TriggerCameraShake(ShakeProfile profile)
        {
            OnCameraShakeRequested?.Invoke(profile);
        }

        public void TriggerGameFinished()
        {
            OnGameFinished?.Invoke();
        }

        /// <summary>
        /// Triggers when the board is reset (all lines are cleared).
        /// Analytics should preserve historical data even after lines are destroyed.
        /// </summary>
        public void TriggerBoardReset()
        {
            OnBoardReset?.Invoke();
        }

        public void TriggerDropPlayerToTheHole(bool b)
        {
            OnDropPlayerToTheHole?.Invoke(b);
        }

        public void TriggerPauseMenuOpen(bool b)
        {
            throw new NotImplementedException();
        }
    }
}