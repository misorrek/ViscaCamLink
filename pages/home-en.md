---
layout: home
description: Camera control
lang: en
permalink: /en/
---

# ![ViscaCamLink](/assets/banner.png)

ViscaCamLink is a WPF desktop application that can control a [PTZ camera](https://de.m.wikipedia.org/wiki/PTZ-Kamera) on the same network using the VISCA protocol.

## Download

<p align="center" width="100%">
<a href="https://github.com/FreakyTorial/ViscaCamLink/releases/latest/download/ViscaCamLink-Installer-x64.msi"><img width="35%" src="{{ '/assets/button_download_windows.png' | relative_url }}"></a> <a href="https://github.com/FreakyTorial/ViscaCamLink/releases/latest/download/ViscaCamLink-Portable-x64.zip"><img width="35%" src="{{ '/assets/button_download_portable-en.png' | relative_url }}"></a>
</p>

## Features

### Presets

* Save and load up to ten positions (including zoom) as presets
* Global hotkey for each preset (currently assigned to numpad 0-9)

### Control

* Free movement in any direction (via button)
* 18-step adjustment of movement speed
* Reset to initial position

### Zoom

* Freely zooming in and out
* 7-step adjustment of zoom speed

### More

* User-specific saving of layout settings
* Full Windows scaling support

## Installation

### Portable 

Place the execution file (.exe) in any location where the executing user has permissions. Then start it - e.g. by double-clicking or using the context menu.

Since .NET 6 is required for the application, a prompt will appear if this is not yet pre-installed. Simply follow this prompt and restart the application if necessary ([Manual installation - .NET Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)).

After that ViscaCamLink is ready for use.

## Usage 

After the first start, the IP of the camera and the port for the VISCA protocol (if different from the default) must be typed in (How to get this information is described in the user manual of the camera).

After that, the connection can be established using the corresponding button in the address bar. Whether this was successful is shown in the status display below.

## Roadmap 

* Customizable key assignment
* Customizable name of presets
* Additional control with mouse
* Support of communication via serial port

## License

[Apache 2.0](LICENSE)

This application is based on the demo code [CameraControl](https://github.com/jskeet/DemoCode/tree/main/CameraControl) (by jskeet)

Icons from flaticon.com/uicons

