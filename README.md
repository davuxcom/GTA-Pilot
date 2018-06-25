GTA Pilot project: Fly airplanes in GTA V without touching the controls.

This is a research project I started some time ago.  The goal is to use the Xbox One streaming app to view the screen and intercept the controller input.

We use Frida to inject a Javascript hook into XboxApp.exe, and EmguCV to interpret the screen.

To control the movements of the airplane, there is a flight planner and auto-pilot modes for vertical and lateral movement.

Control outputs are dictated by simple PID with the a deadzone removed and a maximum bound set.

A/P Modes:
- HDG SEL: Heading Select, turn to and hold a heading
- VS: Vertical speed, maintain a vertical speed ignoring other factors (i.e. airspeed)
- ALT HOLD: Climb or Descend and maintain and altitude
- LNAV: Use HDG SEL to navigate to next waypoint
- IAS: Maintain speed using thrust lever control.

These modes approximate a 1950's style Auto pilot system, similar to the Boeing 707.

v1 TODOs:
- Auto-find XboxApp screen

TODOs:
- Switch to nuget for EmguCV (and upgrade past 3.0.1)
- Upgrade SharpDX to latest
- Frida nuget package?

avTODOS:
- Determine stall speed

Code Quality:
- Lots and lots of cleanup, most of the indicator code came from the prorotype and is a mess.

Requirements:
- 1920x1200 Screen
- Additional screen for GTAPilot window (1920x1200)
- 100% System DPI
- Xbox Controller plugged in to PC
- Xbox One GTA V (Xbox 360 doesn't have streaming and thus can't be used)
- XboxApp must be streaming quality set to Very High
- Franklin must have access to Los Santos Airport
- Must be in the Buckingham Luxor or Buckingham Shamal airplane, other types have differnet indicator positions.
- Save point starting at LS RW3

Getting Started:
- Configure Xbox app for streaming, connect controller to PC
- Start GTA and proceed to LS RW3 start position.
- Start GTAPilot app

The view will lock and you are ready to go.

Knowledgebase:
- The attitude indicator is backwards, possibly the non-Western variant.
- The attitude indicator pitch doesn't have enough pixels to use for interpretation data, but the VS indicator seems to be wired identically.  Ideally we could devise a test to change the pitch angle and VS independently (think high or low AOA), but I haven't been able to prove this yet.
- the VS indicator doesn't match the ALT indicator, suspect the ALT is not correct and VS is.  Devise a test for this (stopwatch + constant climb/VS)
- There are no flaps but LT=235 will give you maximum spoilers

Glossery:
- A/P: Auto-pilot
- LNAV: Lateral Navigation [mode]

