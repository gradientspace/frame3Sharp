# frame3Sharp
Open Source (MIT License) C# library for building 3D Tools in Unity.

Questions? Contact Ryan Schmidt [@rms80](http://www.twitter.com/rms80) / [gradientspace](http://www.gradientspace.com)

# What is this??

Unity is a great platform for games, but 3D tools are not games. Even the most basic 3D tools have much more complex, structured state. You might need different "modes" for different tools or operations, complex 3D user interface elements with state-dependent behavior, Undo/Redo, an easily serializable structured datamodel, and so on. 

However, although every 3D tool has some kind of architecture that does these things, you won't easily find one you can use to build your own tools. Keeping this kind of "3D tool shell" independent from the actual tool you are building is requires a lot of effort. Application functionality tends to bleed over into the infrastructure, to the point where the system is so tightly coupled that re-using the scaffolding becomes implausible.

I've done this 4 times myself. And I don't want to do it again, ever. So, the purpose of **frame3Sharp** is an attempt to provide the "cad shell" infrastucture independent of any particular tool. I am building it on Unity to isolate the CAD-level aspects from the rendering/display subsystem (another area where coupling tends to creep in). Ultimately the Unity-specific elements will be abstracted, so that the system architecture could be ported to other rendering platforms (game engines, three.js, etc). 

I am developing frame3Sharp to use in [gradientspace](https://www.gradientspace.com) products, first and foremost. However I do think that with the rise of distributed manufacturing - from mass customization to on-site medical device fabrication - there is a growing need for focused design tools that solve specific problems. My hope is that frame3Sharp will make this easy, or at least easier.


# Overview

Frame3Sharp (F3) concerns itself with the management and manipulation of a 3D scene that contains a set of objects and interface elements, as well as a 2.5D "heads-up display"-type overlay of additional interface elements. 

At the topmost level there is an **FContext**, you can think of this as the "universe" in which everything exists. An FContext contains an **FScene**, which then contains **SceneObject**s and **SceneUIElement**s. The idea here is that a SceneObject is permanent - a cube, a sphere, etc - while a SceneUIElement is transient. So a 3D transformation gizmo would be a SceneUIElement, as would a clickable button that was in the 3D scene.

The FContext also contains a **Cockpit**, which manages the "near-field" user interface, which could be a 3D HUD in VR, or a set of layered 2D widgets in a desktop app. It is useful to think of this as 2.5D, and many of the objects work this way, with X/Y positioning (although in VR this might be X/Y on a cylinder or sphere) and Z as distance from the camera. The Cockpit elements are also **SceneUIElement** objects. The FContext maintains a stack of Cockpits, so you can "push" a new interface at any time. 

A common paradigm in 3D tools is that when you select something, you get a gizmo that lets you do something with it - translate, scale, resize, etc. The **TransformManager** in **FContext** handles this. Gizmos are classes that implement **ITransformGizmo**, and once registered with TransformManager, will be automatically managed when the FScene selection-set changes. The built-in **AxisTransformGizmo** implements a standard move/rotate/scale gizmo, including transforming groups, rotation around arbitrary pivot points, etc. This gizmo is composed of **Widget** sub-elements, the idea is that these Widgets can be re-assembled into other Gizmos.

Another standard aspect of 3D tools is...the Tools. The FContext has a **ToolManager** which keeps track of which **ITool** is active. The set of possible tools must be registered with ToolManager at startup. Generally a Tool is modal, so when a Tool is active it takes control of the interaction to some extent. For example the provided **DrawPrimitivesTool** overrides the standard left mouse button behavior to draw primitives on left-click-drag. A separate Tool can be active on each input device, so if you are using VR hand controllers you can have a Tool in each hand.











# Features / Capabilities

*we'll get there...*


# Usage

I use this project exclusively as a git submodule, so the structure/etc is set up to make that easy. You just add frame3Sharp as a submodule with a local path inside the **/Assets** folder of your Unity project directory. 

Currently this project is not usable outside of Unity. Someday!!


# Plugins

frame3Sharp includes several small external libraries to provide access to OS functionality inside Unity (which is otherwise not available). These are pre-compiled and included as DLLs/bundles/etc in the \Plugins folder

- [**tinyfiledialogs**](https://sourceforge.net/projects/tinyfiledialogs/) is a cross-platform library with zlib license that can show native file open and save dialogs.


# Dependencies

frame3Sharp is built on several other libraries/components which are not included in this repo. These need to be added to your Unity project yourself. Or, you can start with one of the samples in the [frame3SharpSampleApp](https://github.com/gradientspace/frame3SharpSampleApp) repository, which does include these other projects.

- **geometry3Sharp** [github](https://github.com/gradientspace/geometry3Sharp). This C# codebase should be included in your project as code (or possibly a compiled DLL, but that is currently untested)

- **DOTWeen Unity package** [link](http://dotween.demigiant.com/). Also available in the Unity Asset Store, for free. Used for animations in some places. Removable with minor effort, if you prefer. *This dependency will likely be removed in the near future*

- **Oculus Utilities for Unity 5** [link](https://developer3.oculus.com/downloads/). Because frame3Sharp is designed to support VR, there a few classes that must access the OVRInput to enable use of Oculus Touch controllers. Currently these files are not included, so you must install this component in your project, or delete any references to **OVRInput** in the code (affects 3 files, which are not used if you are not using VR). 


# Documentation / etc

*will come in time!*

