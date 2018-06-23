using System;
using System.Diagnostics;

namespace GTAPilot
{
    class FlightDataComputer
    {
        private ModeControlPanel _mcp;
        private FlightController _control;
        private FlightPlan _flightPlan;

        double DesiredPitch;
        double DesiredRoll;
        double DesiredHeading;
        double DesiredSpeed;
        double DesiredAltitude;

        public PID _pitch_pid;
        public PID _roll_pid;
        public PID _airspeed_pid;

        public FlightDataComputer(ModeControlPanel mcp, FlightController control, string flightPlanFile)
        {
            _control = control;
            _mcp = mcp;
            _mcp.PropertyChanged += MCP_PropertyChanged;

            _roll_pid = new PID
            {
                Gains = FlightComputerConfig.Roll.Gain,
                PV = FlightComputerConfig.Roll.PV,
                OV = FlightComputerConfig.Roll.OV,
            };

            _pitch_pid = new PID
            {
                Gains = FlightComputerConfig.Pitch.Gain,
                PV = FlightComputerConfig.Pitch.PV,
                OV = FlightComputerConfig.Pitch.OV,
            };

            _airspeed_pid = new PID
            {
                Gains = FlightComputerConfig.Speed.Gain,
                PV = FlightComputerConfig.Speed.PV,
                OV = FlightComputerConfig.Speed.OV,
            };

            if (!string.IsNullOrWhiteSpace(flightPlanFile))
            {
                _flightPlan = new FlightPlan(flightPlanFile);
            }
        }

        private void MCP_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_mcp.VSHold) when (_mcp.VSHold):
                    DesiredPitch = Timeline.Pitch;
                    _mcp.VS = (int)DesiredPitch;
                    _mcp.AltitudeHold = false;
                    _pitch_pid.ClearError();
                    Trace.WriteLine($"A/P: Pitch: {DesiredPitch}");
                    break;
                case nameof(_mcp.BankHold) when (_mcp.BankHold):
                    if (_mcp.BankHold)
                    {
                        DesiredRoll = 0;
                        _mcp.Bank = 0;
                        _roll_pid.ClearError();
                        Trace.WriteLine($"A/P: Roll: {DesiredRoll}");
                    }
                    else
                    {
                        DesiredRoll = double.NaN;
                    }

                    break;
                case nameof(_mcp.HeadingHold) when (_mcp.HeadingHold):
                    DesiredHeading = Timeline.Heading;
                    _mcp.HDG = (int)DesiredHeading;
                    _mcp.BankHold = false;
                    Trace.WriteLine($"A/P: Heading: {DesiredHeading}");
                    break;
                case nameof(_mcp.SpeedHold) when (_mcp.SpeedHold):
                    DesiredSpeed = Timeline.Speed;
                    _mcp.IAS = (int)DesiredSpeed;
                    _airspeed_pid.ClearError();
                    Trace.WriteLine($"A/P: Speed: {DesiredSpeed}");
                    break;

                case nameof(_mcp.AltitudeHold) when (_mcp.AltitudeHold):
                    DesiredAltitude = Timeline.Altitude;
                    _mcp.ALT = (int)DesiredAltitude;
                    _mcp.VSHold = false;

                    Trace.WriteLine($"A/P: Altitude: {DesiredAltitude}");
                    break;

                case nameof(_mcp.HDG):

                    if (_mcp.HDG < 0)
                    {
                        _mcp.HDG = 360 - _mcp.HDG;
                    }

                    DesiredHeading = _mcp.HDG;
                    break;
                case nameof(_mcp.VS):
                    DesiredPitch = _mcp.VS;
                    break;
                case nameof(_mcp.ALT):
                    DesiredAltitude = _mcp.ALT;
                    break;
                case nameof(_mcp.IAS):
                    DesiredSpeed = _mcp.IAS;
                    break;
                case nameof(_mcp.Bank):
                    DesiredRoll = _mcp.Bank;
                    break;
            }
        }

        double Handle_Roll(double power)
        {
            power = RemoveDeadZone(power, 4000, 10000);
            _control.SetRoll(power);
            return power;
        }

        double Handle_Pitch(double power)
        {
            power = RemoveDeadZone(power, 4000, 12000);
            power = -1 * power;
            _control.SetPitch(power);
            return power;
        }

        double Handle_Throttle(double throttle)
        {
            _control.SetThrottle(throttle);
            return throttle;
        }

        internal void OnRollDataSampled(int id)
        {
            if (_mcp.BankHold | _mcp.HeadingHold)
            {
                if (!double.IsNaN(Timeline.Data[id].Roll.Value))
                {
                    if (_mcp.HeadingHold)
                    {
                        var d = Math2.DiffAngles(Timeline.Heading, DesiredHeading);
                        var sign = Math.Sign(d);
                        var ad = Math.Abs(d);
                        if (ad > 4)
                        {
                            var roll_angle = Math.Min(ad, 25);
                            var newRoll = _mcp.Bank = (int)(-1 * sign * roll_angle);

                            if (DesiredRoll > newRoll)
                            {
                                DesiredRoll--;
                            }
                            else
                            {
                                DesiredRoll++;
                            }

                        }
                        else
                        {
                            DesiredRoll = 0;
                        }
                    }

                    Timeline.Data[id].Roll.OutputValue = Handle_Roll(
                        _roll_pid.Compute(0, DesiredRoll - Timeline.Data[id].Roll.Value, ComputeDTForFrameId(id, (f) => f.Roll.Value)));
                }
                Timeline.Data[id].Roll.SetpointValue = DesiredRoll;
            }
        }

        internal void OnPitchDataSampled(int id)
        {
            if (_mcp.VSHold || _mcp.AltitudeHold)
            {
                if (_mcp.AltitudeHold)
                {
                    var dx = DesiredAltitude - Timeline.Altitude;

                    var desiredPitch = dx / 10;

                    if (desiredPitch > 10) desiredPitch = 10;
                    if (desiredPitch < -10) desiredPitch = -10;

                    DesiredPitch = desiredPitch;
                }

                if (!double.IsNaN(Timeline.Data[id].Pitch.Value))
                {
                    Timeline.Data[id].Pitch.OutputValue = Handle_Pitch(
                        _pitch_pid.Compute(0, DesiredPitch - Timeline.Data[id].Pitch.Value, ComputeDTForFrameId(id, (f) => f.Pitch.Value)));
                }
                Timeline.Data[id].Pitch.SetpointValue = DesiredPitch;
            }
        }

        internal void OnSpeedDataSampled(int id)
        {
            if (_mcp.SpeedHold)
            {
                if (!double.IsNaN(Timeline.Data[id].Speed.Value))
                {
                    Timeline.Data[id].Speed.OutputValue = Handle_Throttle(
                        _airspeed_pid.Compute(Timeline.Speed, DesiredSpeed, ComputeDTForFrameId(id, (f) => f.Speed.Value)));
                }
                Timeline.Data[id].Speed.SetpointValue = DesiredSpeed;
            }
        }

        internal void OnAltidudeDataSampled(int id)
        {
            if (_mcp.AltitudeHold)
            {
                Timeline.Data[id].Heading.SetpointValue = DesiredAltitude;
            }
        }

        internal void OnCompassDataSampled(int id)
        {
            if (_mcp.HeadingHold)
            {
                var val = Timeline.Data[id].Heading.Value;
                if (!double.IsNaN(val))
                {
                    var diff = Math2.DiffAngles(val, DesiredHeading);
                    var aDiff = Math.Abs(diff);
                    if (aDiff < 4 && aDiff > 2)
                    {
                        aDiff = Math.Min(aDiff, 50) / 4;

                        if (diff < 0)
                        {
                            _control.SetRightRudder(aDiff);
                            Timeline.Data[id].Heading.OutputValue = -1 * aDiff;
                        }
                        else
                        {
                            _control.SetLeftRudder(aDiff);
                            Timeline.Data[id].Heading.OutputValue = aDiff;
                        }
                    }
                    else
                    {
                       // Timeline.Data[id].Heading.OutputValue = 0;
                    }
                }
                Timeline.Data[id].Heading.SetpointValue = DesiredHeading;
            }
        }

        private double ComputeDTForFrameId(int id, Func<TimelineFrame, double> finder)
        {
            double dT = 0;

            var lastGoodFrame = Timeline.LatestFrame(finder, id);
            if (lastGoodFrame != null)
            {
                dT = Timeline.Data[id].Seconds - lastGoodFrame.Seconds;
            }
            return dT;
        }

        double RemoveDeadZone(double power, double deadzone = 4000, double max = 12000)
        {
            if (power > 0)
            {
                power = Math2.MapValue(0, 12000, deadzone, max, power);
            }
            else if (power < 0)
            {
                power = Math2.MapValue(-1 * 12000, 0, -1 * max, -1 * deadzone, power);
            }
            return power;
        }

    }
}
