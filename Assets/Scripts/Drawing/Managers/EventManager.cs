using System;

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
        public event Action OnGameFinished;
        
        public event Action<bool> OnSlowMotionChanged;
        public event Action<bool> OnGamePausedChanged;

        /// <summary>
        /// Triggers the slow motion state.
        /// </summary>
        /// <param name="active">True to enter slow motion, False to return to normal.</param>
        public void TriggerSlowMotion(bool active)
        {
            OnSlowMotionChanged?.Invoke(active);
        }
        
        
        public void TriggerGamePaused(bool active) 
        {
            OnGamePausedChanged?.Invoke(active);
        }
        public void TriggerEraserActive() => OnEraserActive?.Invoke();
        
        public event Action OnEraserInactive;
        public void TriggerEraserInactive() => OnEraserInactive?.Invoke();
        
        public void TriggerConfigButtonSelected(object senderButton) 
        {
            OnConfigButtonSelected?.Invoke(senderButton);
        }

        public void TriggerGameFinished()
        {
            OnGameFinished?.Invoke();
        }
    }
}