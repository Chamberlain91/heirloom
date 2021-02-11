using System.Collections.Generic;

using DragonBones;

namespace Heirloom.Examples.Anim2D.Anim2D
{
    public sealed class AnimArmature : IArmatureProxy
    {
        private readonly AnimEventDispatcher _events = new AnimEventDispatcher();

        public Armature Armature { get; internal set; }

        public Animation Animation => Armature?.animation;

        internal List<AnimSlot> Slots { get; } = new List<AnimSlot>();

        public void DBInit(Armature armature)
        {
            Armature = armature;
        }

        public void DBClear()
        {
            Armature = null;
        }

        public void DBUpdate()
        {
            // the threeJS example added/removed debug drawing here
        }

        #region Event Dispatcher

        public bool HasDBEventListener(string type)
        {
            return _events.HasDBEventListener(type);
        }

        public void DispatchDBEvent(string type, EventObject eventObject)
        {
            _events.DispatchDBEvent(type, eventObject);
        }

        public void AddDBEventListener(string type, ListenerDelegate<EventObject> listener)
        {
            _events.AddDBEventListener(type, listener);
        }

        public void RemoveDBEventListener(string type, ListenerDelegate<EventObject> listener)
        {
            _events.RemoveDBEventListener(type, listener);
        }

        #endregion

        public void Dispose(bool disposeProxy)
        {
            Armature?.Dispose();
            Armature = null;
        }
    }
}
