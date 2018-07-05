using System;
using System.Threading;

namespace GTAPilot
{
    class FlightNavigator
    {
        public bool IsOnGlidePath => _plan.CurrentIndex == _plan.Points.Count - 2 || _plan.CurrentIndex == _plan.Points.Count - 1;
        public bool IsOnGlideToThreshold => _plan.CurrentIndex == _plan.Points.Count - 2;
        public bool IsOnGlidePath90 => IsOnGlidePath && OnGlidePathPercent() < 0.5;

        public double DistanceFromTargetLine => Math2.GetDistanceFromLine(Timeline.CurrentLocation, _plan.TargetLine);

        private FlightPlan _plan;
        private ModeControlPanel _mcp;

        private double OnGlidePathPercent()
        {
            var approach_dist = Math2.GetDistance(_plan.Points[_plan.CurrentIndex - 1], _plan.Points[_plan.CurrentIndex]);
            var dist_from_threshold = Math2.GetDistance(Timeline.CurrentLocation, _plan.Points[_plan.CurrentIndex]);
            var percent_done = dist_from_threshold / approach_dist;

            if (percent_done > 1) percent_done = 1;
            return percent_done;
        }

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
        bool isAt75PercentGlide = false;
        bool isAt4PercentGlide = false;
        bool isAt3PercentGlide = false;
        bool isFlare = false;
        private void Update()
        {
            var didAdvanceWaypoint = _plan.UpdateLocation();

            if (didAdvanceWaypoint && _plan.CurrentIndex == 2)
            {
                SystemManager.Instance.App.Controller.Press(Interop.XINPUT_GAMEPAD_BUTTONS.LEFT_THUMB, 10);
            }

            // One waypoint before top of G/P
            if (didAdvanceWaypoint && _plan.CurrentIndex == _plan.Points.Count - 3)
            {
                _mcp.IAS = 80;
                glidePathTopAlt = _mcp.ALT;
            }

            if (didAdvanceWaypoint && IsOnGlideToThreshold)
            {
                isAt75PercentGlide = false;
                isAt4PercentGlide = false;
                isAt3PercentGlide = false;
                isFlare = false;
                Timeline.UpdateLocationFromMenu();
            }

            if (IsOnGlideToThreshold)
            {
                var percent_done = OnGlidePathPercent();

                if (percent_done < .8 && !isAt75PercentGlide)
                {
                    isAt75PercentGlide = true;

                    Timeline.UpdateLocationFromMenu();
                }
                if (percent_done < .5 && !isAt4PercentGlide)
                {
                    isAt4PercentGlide = true;

                    Timeline.UpdateLocationFromMenu();
                }
                if (percent_done < .3 && !isAt3PercentGlide)
                {
                    isAt3PercentGlide = true;
                    SystemManager.Instance.App.Controller.Press(Interop.XINPUT_GAMEPAD_BUTTONS.LEFT_THUMB, 10);
                }

                _mcp.ALT = Math.Round(Math2.MapValue(0, 1, _plan.Destination.Elevation, glidePathTopAlt, percent_done));
            }

            // Flare
            if (_plan.CurrentIndex == _plan.Points.Count - 1)
            {
                if (!isFlare && Timeline.AltitudeAvg < _plan.Destination.Elevation + 10)
                {
                    isFlare = true;
                    _mcp.IAS = 0;
                    _mcp.VSHold = true;
                    _mcp.VS = 20;
                }
            }

            // Disconnect
            if (_mcp.IASHold && _plan.CurrentIndex == _plan.Points.Count - 1
                && Timeline.Speed < 20)
            {
                if (_mcp.LNAV)
                {
                    Timeline.ResetGameFromSavePointByMenu();
                }

                _mcp.IASHold = false;
                _mcp.VSHold = false;
                _mcp.LNAV = false;
            }

            if (_mcp.LNAV)
            {
                var targetHdg = _plan.TargetHeading;

                if (_plan.CurrentIndex > 0)
                {

                    var tightHoldLine = (Timeline.Altitude < 300) ||
                                        (_plan.CurrentIndex >= _plan.Points.Count - 2);
                    var heading_cap = 10;

                    var distanceFromTargetLine = (tightHoldLine ? 1.25 : 0.5) * DistanceFromTargetLine;
                    distanceFromTargetLine = Math.Max(Math.Min(heading_cap, distanceFromTargetLine), -1 * heading_cap);

                    targetHdg -= distanceFromTargetLine;
                }
                _mcp.HDG = targetHdg;
            }
        }
    }
}
