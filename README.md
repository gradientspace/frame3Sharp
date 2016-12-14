# frame3Sharp
Open Source (MIT License) C# library for building CAD tools in Unity.

Questions? Contact Ryan Schmidt [@rms80](http://www.twitter.com/rms80) / [gradientspace](http://www.gradientspace.com)

# What is this??

Unity is a great platform for games, but CAD tools are not games. Even the most basic CAD tools have much more complex, structured state. You might need different "modes" for different tools or operations, complex 3D user interface elements with state-dependent behavior, Undo/Redo, an easily serializable structured datamodel, and so on. 

However, although every CAD tool has some kind of architecture that does these things, you won't easily find one you can use to build your own tools. Keeping this kind of "CAD shell" independent from the actual tool you are building is requires a lot of effort. Application functionality tends to bleed over into the infrastructure, to the point where the system is so tightly coupled that re-using the scaffolding becomes implausible.

I've done this 4 times myself. And I don't want to do it again, ever. So, the purpose of **frame3Sharp** is an attempt to provide the "cad shell" infrastucture independent of any particular tool. I am building it on Unity to isolate the CAD-level aspects from the rendering/display subsystem (another area where coupling tends to creep in). Ultimately the Unity-specific elements will be abstracted, so that the system architecture could be ported to other rendering platforms (game engines, three.js, etc). 

I am developing frame3Sharp to use in [gradientspace](https://www.gradientspace.com) products, first and foremost. However I do think that with the rise of distributed manufacturing - from mass customization to on-site medical device fabrication - there is a growing need for focused design tools that solve specific problems. My home is that frame3Sharp will make this easy, or at least easier.

# Features / Capabilities

*we'll get there...*


# Usage

I use this project exclusively as a git submodule, so the structure/etc is set up to make that easy. You just add frame3Sharp as a submodule with a local path inside the **/Assets** folder of your Unity project directory. 

Currently this project is not usable outside of Unity. Someday!!


# Dependencies

- **geometry3Sharp** [github](https://github.com/gradientspace/geometry3Sharp). This C# codebase should be included in your project as code (or possibly a compiled DLL, but that is currently untested)

- **DOTWeen Unity package** [link](http://dotween.demigiant.com/). Also available in the Unity Asset Store, for free. Used for animations in some places. Removable with minor effort, if you prefer. *This dependency will likely be removed in the near future*

- **Oculus Utilities for Unity 5** [link](https://developer3.oculus.com/downloads/). Because frame3Sharp is designed to support VR, there a few classes that must access the OVRInput to enable use of Oculus Touch controllers. Currently these files are not included, so you must install this component in your project, or delete any references to **OVRInput** in the code (affects 3 files, which are not used if you are not using VR). 


# Documentation / etc

*will come in time!*

