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

        private void Update()
        {
            var didAdvanceWaypoint = _plan.UpdateLocation();

            if (didAdvanceWaypoint && _plan.CurrentIndex == _plan.Points.Count - 11)
            {
                _mcp.ALT = 500;
                _mcp.IAS = 80;
            }

            if (didAdvanceWaypoint && _plan.CurrentIndex == _plan.Points.Count - 9)
            {
                _mcp.ALT = 425;
            }

            if (didAdvanceWaypoint && _plan.CurrentIndex == _plan.Points.Count - 8)
            {
                _mcp.ALT = 375;
            }

            if (didAdvanceWaypoint && _plan.CurrentIndex == _plan.Points.Count - 7)
            {
                _mcp.ALT = 325;
            }

            if (didAdvanceWaypoint && _plan.CurrentIndex == _plan.Points.Count - 6)
            {
                _mcp.ALT = 275;
            }

            if (didAdvanceWaypoint && _plan.CurrentIndex == _plan.Points.Count - 5)
            {
                _mcp.ALT = 225;
            }

            if (didAdvanceWaypoint && _plan.CurrentIndex == _plan.Points.Count - 4)
            {
                _mcp.ALT = 175;
            }

            if (didAdvanceWaypoint && _plan.CurrentIndex == _plan.Points.Count - 3)
            {
                _mcp.ALT = 120;
            }

            if (didAdvanceWaypoint && _plan.CurrentIndex == _plan.Points.Count - 2)
            {
                _mcp.ALT = 85;
            }

            if (didAdvanceWaypoint && _plan.CurrentIndex == _plan.Points.Count - 1)
            {
                _mcp.IAS = 40;
                _mcp.VSHold = true;
                _mcp.VS = 14;
            }

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

                var tightHoldLine = (Timeline.Altitude < 300);
                var heading_cap = 20;
                var distanceFromTargetLine = (tightHoldLine ? 4 : 0.5) * Math2.GetDistanceFromLine(Timeline.CurrentLocation, _plan.TargetLine);
                distanceFromTargetLine = Math.Max(Math.Min(heading_cap, distanceFromTargetLine), -1 * heading_cap);

                targetHdg -= distanceFromTargetLine;

                _mcp.HDG = targetHdg;
            }
        }
    }
}
