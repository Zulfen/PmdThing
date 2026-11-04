using PmdThing.Components.Graphics;
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
        

        public void SetTarget(PmdContainer container)
        {
            SetTarget(container.GetComponent<Transform>());
        }

        public void SetTarget(Transform transform)
        {
            var tiledWorld = GetBaseContainerComponent<TiledWorld, TiledWorldRenderer>();

            _target.Changed -= tiledWorld.Camera_OffsetChanged;
            _target.IsTarget = false;

            _target = transform;

            _target.IsTarget = true;
            _target.Changed += tiledWorld.Camera_OffsetChanged;
        }

        public override void Cleanup()
        {
        }

    }
}
