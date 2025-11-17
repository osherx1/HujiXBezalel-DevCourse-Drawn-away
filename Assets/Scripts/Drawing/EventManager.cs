using System;

namespace Drawing
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
        public void TriggerEraserActive() => OnEraserActive?.Invoke();
        
        public event Action OnEraserInactive;
        public void TriggerEraserInactive() => OnEraserInactive?.Invoke();
    }
}