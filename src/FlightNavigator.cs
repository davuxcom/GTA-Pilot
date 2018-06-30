using System;
using System.Threading;

namespace GTAPilot
{
    class FlightNavigator
    {
        private FlightPlan _plan;
        private ModeControlPanel _mcp;

        public FlightNavigator(ModeControlPanel mcp, FlightPlan plan)
        {
            _mcp = mcp;
            _plan = plan;
        }

        public void Begin()
        {
            var t = new Thread(NavThreadProc);
            t.IsBackground = true;
            t.Start();
        }

        private void NavThreadProc()
        {
            while (true)
            {
                Update();

                Thread.Sleep(1000 / 60);
            }
        }

        double glidePathTopAlt = double.NaN;
        private void Update()
        {
            var didAdvanceWaypoint = _plan.UpdateLocation();

            // One waypoint before top of G/P
            if (didAdvanceWaypoint && _plan.CurrentIndex == _plan.Points.Count - 3)
            {
                _mcp.IAS = 80;
                glidePathTopAlt = _mcp.ALT;
            }

            // G/P
            if (_plan.CurrentIndex == _plan.Points.Count - 2)
            {
                var approach_dist = Math2.GetDistance(_plan.Points[_plan.CurrentIndex - 1], _plan.Points[_plan.CurrentIndex]);
                var dist_from_threshold = Math2.GetDistance(Timeline.CurrentLocation, _plan.Points[_plan.CurrentIndex]);
                var percent_done = dist_from_threshold / approach_dist;

                if (percent_done > 1) percent_done = 1;

                _mcp.ALT = Math.Round(Math2.MapValue(0, 1, _plan.Destination.Elevation, glidePathTopAlt, percent_done));
            }

            // Flare
            if (didAdvanceWaypoint && _plan.CurrentIndex == _plan.Points.Count - 1)
            {
                _mcp.IAS = 0;
                _mcp.VSHold = true;
                _mcp.VS = 14;
            }

            // Disconnect
            if (_mcp.IASHold && _plan.CurrentIndex == _plan.Points.Count - 1
                && Timeline.Speed < 40)
            {
                _mcp.IASHold = false;
                _mcp.VSHold = false;
                _mcp.LNAV = false;
            }

            if (_mcp.LNAV)
            {
                var targetHdg = _plan.TargetHeading;

                var tightHoldLine = (Timeline.Altitude < 300) ||
                                    (_plan.CurrentIndex >= _plan.Points.Count - 2);
                var heading_cap = 15;
                var distanceFromTargetLine = (tightHoldLine ? 2 : 0.5) * Math2.GetDistanceFromLine(Timeline.CurrentLocation, _plan.TargetLine);
                distanceFromTargetLine = Math.Max(Math.Min(heading_cap, distanceFromTargetLine), -1 * heading_cap);

                targetHdg -= distanceFromTargetLine;

                _mcp.HDG = targetHdg;
            }
        }
    }
}
