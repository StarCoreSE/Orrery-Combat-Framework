using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Orrery.HeartModule.Server.GridTargeting
{
    internal class GridTargetingManager
    {
        private static GridTargetingManager _;
        private HashSet<GridTargeting> _gridTargetings = new HashSet<GridTargeting>();

        public static GridTargeting GetGridTargeting(IMyCubeGrid grid) => _._gridTargetings.First(targeting => targeting.Grid == grid);

        public GridTargetingManager()
        {
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += OnEntityRemove;

            _ = this;
        }

        public void Close()
        {
            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove -= OnEntityRemove;

            _ = null;
        }

        private int _ticks;
        public void Update()
        {
            foreach (var targeting in _gridTargetings)
                targeting.Update();

            if (_ticks++ % 10 == 0)
                Update10();
        }

        private void Update10()
        {
            // TODO: Spread this over multiple ticks
            foreach (var targeting in _gridTargetings)
                targeting.Update10();
        }

        private void OnEntityAdd(IMyEntity entity)
        {
            IMyCubeGrid grid = entity as IMyCubeGrid;
            if (grid == null)
                return;
            _gridTargetings.Add(new GridTargeting(grid));
        }

        private void OnEntityRemove(IMyEntity entity)
        {
            IMyCubeGrid grid = entity as IMyCubeGrid;
            if (grid == null)
                return;
            _gridTargetings.Remove(_gridTargetings.FirstOrDefault(targeting => targeting.Grid == grid));
        }
    }
}
