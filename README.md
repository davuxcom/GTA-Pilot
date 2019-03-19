# GTA Pilot 
![GTA Pilot running on Xbox One](./res/hero1.gif)

This is a research project; the goal is to intercept the Xbox One streaming app and control the game.

I use Frida to inject a Javascript hook into XboxApp.exe to read and write the controller state, and EmguCV to interpret the game visuals.

![GTA Pilot running on Xbox One](./res/analyzer.gif)

There is a simple autoflight system, a basic flight director, modern primary flight/navigation displays, a virtual Inertial Reference System based on compass heading and airspeed.

I am unable to model the relationship between thrust and momentum, so there is sideslip error and we open the map to get a known position.  For this reason the Franklin avatar must be used as we expect to find the green hangar building.

### Requirements:
- Two 1920x1200 displays, system DPI set to 100%
- Xbox Controller connected via USB
- GTA V
- Franklin avatar must have access to Los Santos Airport

## Getting Started:
![GTA starting position at Runway 3](./res/ls_rw3_start.png)

A starting position must be set in the game.  Create a save point in the Luxor or Shamal airplane, in the blast zone for RW3 at Los Santos airport.  Other airplanes will not work.

![GTA starting position at Runway 3](./res/settings.png)

Ensure that these options are off under Settings / Camera:
- Head Bobbing: Off
- Ragdoll: Off

![Buckingham Luxor airplane](./res/streaming_quality.png)

- Start the Xbox app, proceed to the starting position.
- Ensure that streaming quality is set to 'Very High.'  Keep the app in fullscreen mode.
- Start GTA Pilot.  Set focus to the Xbox streaming window, and observe the view move down as it locks.
- Press SELECT/CAMERA button on the controller to activate the flight plan and begin the flight.

## More information

### Autopilot Modes:
- HDG SEL: Heading Select, turn to and hold a heading
- VS: Vertical speed, maintain a vertical speed ignoring other factors (i.e. airspeed)
- ALT HOLD: Climb or Descend and maintain and altitude
- LNAV: Internally uses HDG SEL to navigate to track the navigation line.
- IAS: Maintain speed using thrust lever control.