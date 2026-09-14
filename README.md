# Multiplayer Cycling Simulator (Unity)

A multiplayer cycling simulation built in Unity, featuring real-time Bluetooth Low Energy (BLE) smart trainer telemetry, spline-based movement mechanics, and client-authoritative multiplayer networking.

## Core Engineering Systems

* **Bluetooth LE Integration (`FreeWindowsBike.cs`, `BluetoothManager.cs`)**: Interfaces with native Windows BLE DLL wrappers (`BleWinrtDll.dll`) to scan, connect, and stream live power wattage and cadence data directly from smart trainers using standard cycling service protocols[cite: 2, 3, 6].
* **Multiplayer Architecture (`BikeNetworkSetup.cs`, `ClientNetworkTransform.cs`)**: Utilizes Unity Netcode for GameObjects to handle client-authoritative transforms, delay-initialized Cinemachine tracking cameras, and dynamic player instantiation along spline paths[cite: 1, 4].
* **Ghost Telemetry System (`GhostRecorder.cs`, `GhostManager.cs`, `GhostCountdown.cs`)**: Records precise positional spline frames and speed metrics into structured JSON files, enabling local ghost data persistence and real-time race replays[cite: 8, 9, 10].
* **Bicycle Physics Evolution (`BicycleController.cs`)**: Initially engineered a full, hyper-realistic physical bike simulation that proved too complex for the arcade-style gameplay loop, shifting its role to strictly drive procedural leaning and movement mechanics.
* **Spline Navigation (`ZwiftSegment.cs`)**: Handles track mapping and path calculations along the course.
* **Suspension & Damping (`SuspensionManager.cs`)**: Controls the mechanical dampening and physical response over varying terrain.
* **Camera & View Management (`BicycleCamera.cs`, `TPSCamSwitch.cs`, `PerfectMouseLook.cs`)**: Directs dynamic third-person camera angles, smooth transitions, and mouse tracking during gameplay.
* **Input Handling (`MobileButtonHandler.cs`)**: Processes touch and button inputs for mobile or auxiliary device controls.
* **Animation & Rigging (`CyclistAnimController.cs`, `ProceduralIKHandler.cs`, `RagdollJointConfig.cs`, `RagdollJointImitation.cs`)**: Drives procedural inverse kinematics, cyclist pedal animations, and dynamic ragdoll reactions.
* **Audio System (`BicycleSounds.cs`)**: Manages real-time audio triggers for rolling resistance, speed, and environment feedback.
* **Modular Code Structure**: Cleanly separated systems handling physics calculations, user interface input management, audio states, and camera switching.
```[cite: 1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
