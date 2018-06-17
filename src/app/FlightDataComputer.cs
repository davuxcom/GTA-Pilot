using System;
using System.Diagnostics;

namespace GTAPilot
{
    class FlightDataComputer
    {
        private ModeControlPanel _mcp;
        private FlightController _control;

        double DesiredPitch;
        double DesiredRoll;
        double DesiredHeading;
        double DesiredSpeed;
        double DesiredAltitude;

        public PID _pitch_pid;
        public PID _roll_pid;
        public PID _airspeed_pid;
        public PID _heading_pid;

        public FlightDataComputer(ModeControlPanel mcp, FlightController control)
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

            _heading_pid = new PID
            {
                Gains = FlightComputerConfig.Yaw.Gain,
                PV = FlightComputerConfig.Yaw.PV,
                OV = FlightComputerConfig.Yaw.OV,
            };

            _airspeed_pid = new PID
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
                    DesiredPitch = 3;
                    _mcp.VS = (int)DesiredPitch;
                    _mcp.AltitudeHold = false;
                    _pitch_pid.ClearError();
                    Trace.WriteLine($"A/P: Pitch: {DesiredPitch}");
                    break;
                case nameof(_mcp.BankHold) when (_mcp.BankHold):
                    if (_mcp.BankHold)
                    {
                        DesiredRoll = 0;
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
                    _heading_pid.ClearError();
                    // _mcp.BankHold = false;
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
            }
        }

        double Handle_Roll(double power)
        {
            power -= short.MaxValue;

            //  var pp = Math.Round((double)((power) / (short.MaxValue)) * 100, 2);
            //  power = RemoveDeadZone(power, FlightComputerConfig.Roll_DeadZone, FlightComputerConfig.Roll_Max);
            _control.SetRoll(power);
            return power + short.MaxValue;
        }

        double Handle_Pitch(double power)
        {
            power -= short.MaxValue;
            power *= -1;

            // var rollAngle = Math.Abs(Timeline.Roll);
            // if (rollAngle > 18)
            //  {
            //      power += FlightComputerConfig.RollTrim;
            //  }

            //  double max = FlightComputerConfig.Pitch_Max;
            // var pp = Math.Round((double)((power) / (short.MaxValue)) * 100, 2);

            //  power = RemoveDeadZone(power, FlightComputerConfig.Pitch_DeadZone, max);

            _control.SetPitch(power);
            return power + short.MaxValue;
        }

        double Handle_Get_Compass()
        {

            var val = Math.Round(Timeline.Heading);
            var target = DesiredHeading;

            if (val == target) return 360;

            double right_turn = 0;
            double left_turn = 0;

            var a = val;
            var b = target;
            var diff = ((a - b + 180 + 360) % 360) - 180;

            if (diff < 0)
            {
                right_turn = diff;
                left_turn = 360 - diff;
            }
            else
            {
                left_turn = diff;
                right_turn = 360 - diff;
            }

            double ret_normal = 0;
            double ret = 0;
            if (right_turn < left_turn)
            {
                ret_normal = Math.Abs(right_turn);
                ret = 360 + Math.Abs(right_turn);
            }
            else
            {
                ret_normal = -1 * Math.Abs(left_turn);
                ret = 360 - Math.Abs(left_turn);
            }

            // if (!double.IsNaN(DesiredHeading))
            {
                // _panel.Compass.InputValues.Add(new Indicator.IndicatorValueData { Tick = _panel.Ticks, Value = ret_normal });
            }

            if (double.IsNaN(DesiredHeading))
            {
                return 360;
            }

            return (int)Math.Round(ret);
        }

        double Handle_Compass(double power)
        {
            var v = Math.Round(power);
            double vx = 0;

            if (v > 50)
            {
                v -= 50;
                vx = v;
                _control.SetLeftRudder(v);
                return 1;
            }
            else if (v < 50)
            {
                v = 50 - v;
                vx = v * -1;

                _control.SetRightRudder(v);
                return -1;
            }
            return 0;
        }

        double Handle_Throttle(double throttle)
        {
            _control.SetThrottle(throttle);
            return throttle;
        }

        internal void OnRollDataSampled(int id)
        {
            if (_mcp.BankHold)
            {
                if (!double.IsNaN(Timeline.Data[id].Roll.Value))
                {
                    Timeline.Data[id].Roll.OutputValue = Handle_Roll(_roll_pid.Compute(Timeline.Roll + FlightComputerConfig.Roll.PV_Skew,
                        double.IsNaN(DesiredRoll) ? FlightComputerConfig.Roll.PV.Mid : DesiredRoll + FlightComputerConfig.Roll.PV.Mid,
                        ComputeDTForFrameId(id, (f) => f.Roll.Value)));
                }
                Timeline.Data[id].Roll.SetpointValue = DesiredRoll;
            }
            else if (_mcp.HeadingHold)
            {
                // TODO: HDG turns
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
                    Timeline.Data[id].Pitch.OutputValue = Handle_Pitch(_pitch_pid.Compute(Timeline.Pitch + FlightComputerConfig.Pitch.PV_Skew,
                    double.IsNaN(DesiredPitch) ? FlightComputerConfig.Pitch.PV.Mid : DesiredPitch + FlightComputerConfig.Pitch.PV.Mid,
                    ComputeDTForFrameId(id, (f) => f.Pitch.Value)));
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
                    Timeline.Data[id].Speed.OutputValue = Handle_Throttle(_airspeed_pid.Compute(Timeline.Speed + FlightComputerConfig.Speed.PV_Skew,
                    DesiredSpeed,
                    ComputeDTForFrameId(id, (f) => f.Speed.Value)));
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
                if (!double.IsNaN(Timeline.Data[id].Heading.Value))
                {
                    Timeline.Data[id].Heading.OutputValue = Handle_Compass(_heading_pid.Compute(Handle_Get_Compass(), DesiredHeading,
                    ComputeDTForFrameId(id, (f) => f.Heading.Value)));
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

        double RemoveDeadZone(double power, double deadzone, double max)
        {
            if (power > 0)
            {
                power = Math2.MapValue(0, short.MaxValue, deadzone, max, power);
            }
            else if (power < 0)
            {
                power = Math2.MapValue(-1 * short.MaxValue, 0, -1 * max, -1 * deadzone, power);
            }
            return power;
        }

        double GetScaledValue(double value, double scaleValue)
        {
            if (value > 0)
            {
                value = GetScaledValue(value, short.MaxValue, scaleValue);
            }
            else if (value < 0)
            {
                value = -1 * GetScaledValue(Math.Abs(value), short.MaxValue, scaleValue);
            }
            return value;
        }

        double GetScaledValue(double value, double scale, double pow)
        {
            return Math.Pow(value / scale, pow) * scale;
        }
    }
}
