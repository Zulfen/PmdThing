using GlmSharp;
using PmdThing.Containers;
using PmdThing.Core;

namespace PmdThing.Components
{
    public class Camera : PmdComponent
    {

        private Transform _target;

        public Camera(PmdComponentConfig config) : base(config)
        {
            _target = new Transform(config, 0, 0);
            var window = Framework.Graphics.GetMainWindow();
        }
        

        public void SetTarget(PmdContainer container, Action<vec2, vec2>? changedListener = null)
        {
            SetTarget(container.GetComponent<Transform>());
        }

        public void SetTarget(Transform transform, Action<vec2, vec2>? changedListener = null)
        {
            var tiledWorld = GetBaseContainerComponent<TiledWorld, TiledWorldRenderer>();

            if (changedListener != null)
            {
                _target.Changed -= changedListener;
            }
            _target.Changed -= tiledWorld.Camera_OffsetChanged;


            _target.IsTarget = false;
            _target = transform;
            _target.IsTarget = true;


            if (changedListener != null)
            {
                _target.Changed += changedListener;
            }
            _target.Changed += tiledWorld.Camera_OffsetChanged;
        }

        public override void Cleanup()
        {
        }

    }
}
