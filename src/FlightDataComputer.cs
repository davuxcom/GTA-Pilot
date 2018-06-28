using GTAPilot.Interop;
using System;
using System.Diagnostics;

namespace GTAPilot
{
    class FlightDataComputer
    {
        private ModeControlPanel _mcp;
        private XboxController _control;
        private FlightPlan _plan;
        private double _desiredPitch;
        private double _desiredRoll;
        private double _desiredHeading;
        private double _desiredSpeed;
        private double _desiredAltitude;
        private PID _pitchPid;
        private PID _rollPid;
        private PID _speedPid;

        public FlightDataComputer(ModeControlPanel mcp, XboxController control, FlightPlan plan)
        {
            _plan = plan;
            _control = control;
            _mcp = mcp;
            _mcp.PropertyChanged += MCP_PropertyChanged;

            _rollPid = new PID
            {
                Gains = FlightComputerConfig.Roll.Gain,
                PV = FlightComputerConfig.Roll.PV,
                OV = FlightComputerConfig.Roll.OV,
            };

            _pitchPid = new PID
            {
                Gains = FlightComputerConfig.Pitch.Gain,
                PV = FlightComputerConfig.Pitch.PV,
                OV = FlightComputerConfig.Pitch.OV,
            };

            _speedPid = new PID
            {
                Gains = FlightComputerConfig.Speed.Gain,
                PV = FlightComputerConfig.Speed.PV,
                OV = FlightComputerConfig.Speed.OV,
            };
        }

        private void MCP_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_mcp.VSHold) when (_mcp.VSHold):
                    _desiredPitch = Timeline.Pitch;
                    _mcp.VS = (int)_desiredPitch;
                    _mcp.AltitudeHold = false;
                    _pitchPid.ClearError();
                    Trace.WriteLine($"A/P: Pitch: {_desiredPitch}");
                    break;
                case nameof(_mcp.BankHold) when (_mcp.BankHold):
                    _desiredRoll = 0;
                    _mcp.Bank = 0;
                    _mcp.HeadingHold = false;
                    _mcp.LNAV = false;
                    _rollPid.ClearError();
                    Trace.WriteLine($"A/P: Roll: {_desiredRoll}");
                    break;
                case nameof(_mcp.LNAV) when (_mcp.LNAV):
                    _mcp.HeadingHold = false;
                    _mcp.BankHold = false;
                    _rollPid.ClearError();
                    Trace.WriteLine($"A/P: LNAV: On");
                    break;
                case nameof(_mcp.HeadingHold) when (_mcp.HeadingHold):
                    _desiredHeading = Timeline.Heading;
                    _mcp.HDG = (int)_desiredHeading;
                    _mcp.BankHold = false;
                    _mcp.LNAV = false;
                    Trace.WriteLine($"A/P: Heading: {_desiredHeading}");
                    break;
                case nameof(_mcp.IASHold) when (_mcp.IASHold):
                    _speedPid.ClearError();
                    Trace.WriteLine($"A/P: Speed: {_desiredSpeed}");
                    break;

                case nameof(_mcp.AltitudeHold) when (_mcp.AltitudeHold):
                    _mcp.VSHold = false;
                    Trace.WriteLine($"A/P: Altitude: {_desiredAltitude}");
                    break;

                case nameof(_mcp.HDG):
                    _desiredHeading = _mcp.HDG;
                    break;
                case nameof(_mcp.VS):
                    _desiredPitch = _mcp.VS;
                    break;
                case nameof(_mcp.ALT):
                    _desiredAltitude = _mcp.ALT;
                    break;
                case nameof(_mcp.IAS):
                    _desiredSpeed = _mcp.IAS;
                    break;
            }
        }

        double Handle_Roll(double power)
        {
            _control.Set(XINPUT_GAMEPAD_AXIS.LEFT_THUMB_X, (int)RemoveDeadZone(power, 4000, FlightComputerConfig.MAX_AXIS_VALUE));
            return power;
        }

        double Handle_Pitch(double power)
        {
            // Trim:
            {
                power += Math.Abs(Timeline.RollAvg) * 30;
            }

            power = -1 * power;
            _control.Set(XINPUT_GAMEPAD_AXIS.LEFT_THUMB_Y, (int)RemoveDeadZone(power, 4000, FlightComputerConfig.MAX_AXIS_VALUE));
            return power;
        }

        double Handle_Throttle(double throttle)
        {
            if (throttle > 235)
            {
                _control.Set(XINPUT_GAMEPAD_AXIS.RIGHT_TRIGGER, (int)throttle - 235);
                _control.Set(XINPUT_GAMEPAD_AXIS.LEFT_TRIGGER, 0);
            }
            else
            {
                _control.Set(XINPUT_GAMEPAD_AXIS.LEFT_TRIGGER, 235 - (int)throttle);
                _control.Set(XINPUT_GAMEPAD_AXIS.RIGHT_TRIGGER, 0);
            }

            return throttle;
        }

        internal void OnRollDataSampled(int id)
        {
            if (double.IsNaN(Timeline.Heading)) return;

            if (_plan != null)
            {
                var didAdvanceWaypoint = _plan.UpdateLocation();

                if (didAdvanceWaypoint && _plan.CurrentIndex == _plan.Points.Count - 7)
                {
                    _mcp.ALT = 600;
                    _mcp.IAS = 80;
                }

                if (didAdvanceWaypoint && _plan.CurrentIndex == _plan.Points.Count - 6)
                {
                    _mcp.ALT = 500;
                }

                if (didAdvanceWaypoint && _plan.CurrentIndex == _plan.Points.Count - 5)
                {
                    _mcp.ALT = 400;
                }

                if (didAdvanceWaypoint && _plan.CurrentIndex == _plan.Points.Count - 4)
                {
                    _mcp.ALT = 300;
                }

                if (didAdvanceWaypoint && _plan.CurrentIndex == _plan.Points.Count - 3)
                {
                    _mcp.ALT = 200;
                }

                if (didAdvanceWaypoint && _plan.CurrentIndex == _plan.Points.Count - 2)
                {
                    _mcp.ALT = 100;
                }

                if (didAdvanceWaypoint && _plan.CurrentIndex == _plan.Points.Count - 1)
                {
                    _mcp.IAS = 40;
                    _mcp.VSHold = true;
                    _mcp.VS = 14;
                }
            }

            if (_mcp.BankHold | _mcp.HeadingHold | _mcp.LNAV)
            {
                if (!double.IsNaN(Timeline.Data[id].Roll.Value))
                {
                    if (_mcp.HeadingHold | _mcp.LNAV)
                    {
                        if (_mcp.LNAV)
                        {
                            _desiredHeading = _plan.TargetHeading;
                            _mcp.HDG = _desiredHeading;
                        }

                        var d = Math2.DiffAngles(Timeline.Heading, _desiredHeading);
                        var sign = Math.Sign(d);
                        var ad = Math.Abs(d);

                        var roll_angle = Math.Min(ad, 20);
                        var newRoll = (int)(-1 * sign * roll_angle);

                        if (_desiredRoll > newRoll)
                        {
                            _desiredRoll -= 0.25;
                        }
                        else
                        {
                            _desiredRoll += 0.25;
                        }
                    }

                    Timeline.Data[id].Roll.OutputValue = Handle_Roll(
                        _rollPid.Compute(0, _desiredRoll - Timeline.Data[id].Roll.Value, GetTimeBetweenThisFrameAndLastGoodFrame(id, (f) => f.Roll.Value)));
                }
                Timeline.Data[id].Roll.SetpointValue = _desiredRoll;
            }
        }

        internal void OnPitchDataSampled(int id)
        {
            if (_mcp.VSHold || _mcp.AltitudeHold)
            {
                if (_mcp.AltitudeHold)
                {
                    var dx = _desiredAltitude - Timeline.Altitude;

                    var desiredPitch = dx / 10;

                    if (desiredPitch > 20) desiredPitch = 20;
                    if (desiredPitch < -10) desiredPitch = -10;

                    _desiredPitch = desiredPitch;
                    _mcp.VS = _desiredPitch;
                }

                if (!double.IsNaN(Timeline.Data[id].Pitch.Value))
                {
                    Timeline.Data[id].Pitch.OutputValue = Handle_Pitch(
                        _pitchPid.Compute(0, _desiredPitch - Timeline.Data[id].Pitch.Value, GetTimeBetweenThisFrameAndLastGoodFrame(id, (f) => f.Pitch.Value)));
                }
                Timeline.Data[id].Pitch.SetpointValue = _desiredPitch;
            }

            _control.Flush();
        }

        internal void OnSpeedDataSampled(int id)
        {
            if (_mcp.IASHold)
            {
                if (!double.IsNaN(Timeline.Data[id].Speed.Value))
                {
                    Timeline.Data[id].Speed.OutputValue = Handle_Throttle(
                        _speedPid.Compute(Timeline.Speed, _desiredSpeed, GetTimeBetweenThisFrameAndLastGoodFrame(id, (f) => f.Speed.Value)));
                }
                Timeline.Data[id].Speed.SetpointValue = _desiredSpeed;
            }
        }

        internal void OnAltidudeDataSampled(int id)
        {
            if (_mcp.AltitudeHold)
            {
                Timeline.Data[id].Heading.SetpointValue = _desiredAltitude;
            }
        }

        internal void OnCompassDataSampled(int id)
        {
            if (_mcp.HeadingHold | _mcp.LNAV)
            {
                var val = Timeline.Data[id].Heading.Value;
                if (!double.IsNaN(val) && Timeline.Speed > 10)
                {
                    var internalDesiredHeading = _desiredHeading;

                    // If on a Flight Plan, stick to the line.
                    if (_plan != null)
                    {
                        var heading_cap = 20;
                        var distanceFromTargetLine = 1.5 * Math2.GetDistanceFromLine(Timeline.CurrentLocation, _plan.TargetLine);
                        distanceFromTargetLine = Math.Max(Math.Min(heading_cap, distanceFromTargetLine), -1 * heading_cap);

                        internalDesiredHeading -= distanceFromTargetLine;
                    }

                    var diff = Math2.DiffAngles(val, internalDesiredHeading);
                    var aDiff = Math.Abs(diff);

                    if (aDiff > 1)
                    {
                        aDiff = Math.Min(aDiff / 8, 10);

                        if (diff < 0)
                        {
                            _control.Press(XINPUT_GAMEPAD_BUTTONS.RIGHT_SHOULDER, (int)aDiff);
                            Timeline.Data[id].Heading.OutputValue = -1 * aDiff;
                        }
                        else
                        {
                            _control.Press(XINPUT_GAMEPAD_BUTTONS.LEFT_SHOULDER, (int)aDiff);
                            Timeline.Data[id].Heading.OutputValue = aDiff;
                        }
                    }
                }
                Timeline.Data[id].Heading.SetpointValue = _desiredHeading;
            }
        }

        private double GetTimeBetweenThisFrameAndLastGoodFrame(int thisFrameId, Func<TimelineFrame, double> finder)
        {
            var lastGoodFrame = Timeline.LatestFrame(finder, thisFrameId);
            if (lastGoodFrame != null)
            {
                return Timeline.Data[thisFrameId].Seconds - lastGoodFrame.Seconds;
            }
            return 0;
        }

        double RemoveDeadZone(double power, double deadzone, double max)
        {
            if (power > 0)
            {
                power = Math2.MapValue(0, FlightComputerConfig.MAX_AXIS_VALUE, deadzone, max, power);
            }
            else if (power < 0)
            {
                power = Math2.MapValue(FlightComputerConfig.MIN_AXIS_VALUE, 0, -1 * max, -1 * deadzone, power);
            }
            return power;
        }
    }
}
