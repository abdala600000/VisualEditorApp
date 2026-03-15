using System;

namespace VisualEditor.Core.Messages
{
    /// <summary>
    /// A lightweight, high-performance static message bus replacing WeakReferenceMessenger.
    /// This acts as a central 'server' for broadcasting events across decoupled components.
    /// </summary>
    public static class MessageBus
    {
        public static event Action<ControlSelectedMessage>? ControlSelected;
        public static void Send(ControlSelectedMessage message) => ControlSelected?.Invoke(message);

        public static event Action<DesignChangedMessage>? DesignChanged;
        public static void Send(DesignChangedMessage message) => DesignChanged?.Invoke(message);

        public static event Action<DesignTreeUpdatedMessage>? DesignTreeUpdated;
        public static void Send(DesignTreeUpdatedMessage message) => DesignTreeUpdated?.Invoke(message);

        public static event Action<ProjectBuiltMessage>? ProjectBuilt;
        public static void Send(ProjectBuiltMessage message) => ProjectBuilt?.Invoke(message);

        public static event Action<BuildFinishedMessage>? BuildFinished;
        public static void Send(BuildFinishedMessage message) => BuildFinished?.Invoke(message);

        public static event Action<PropertyChangedMessage>? PropertyChanged;
        public static void Send(PropertyChangedMessage message) => PropertyChanged?.Invoke(message);

        public static event Action<SystemDiagnosticMessage>? SystemDiagnostic;
        public static void Send(SystemDiagnosticMessage message) => SystemDiagnostic?.Invoke(message);
    }
}
