# frame3Sharp
Open Source (MIT License) C# library for building 3D Tools in Unity.

Questions? Contact Ryan Schmidt [@rms80](http://www.twitter.com/rms80) / [gradientspace](http://www.gradientspace.com)

**NOTE: Please read Usage and Setup instructions below to configure your project.**

# What is this??

Unity is a great platform for games, but 3D tools are not games. Even the most basic 3D tools have much more complex, structured state. You might need different "modes" for different tools or operations, complex 3D user interface elements with state-dependent behavior, Undo/Redo, an easily serializable structured datamodel, and so on. 

However, although every 3D tool has some kind of architecture that does these things, you won't easily find one you can use to build your own tools. Keeping this kind of "3D tool shell" independent from the actual tool you are building is requires a lot of effort. Application functionality tends to bleed over into the infrastructure, to the point where the system is so tightly coupled that re-using the scaffolding becomes implausible.

I've done this 4 times myself. And I don't want to do it again, ever. So, the purpose of **frame3Sharp** is an attempt to provide the "cad shell" infrastucture independent of any particular tool. I am building it on Unity to isolate the CAD-level aspects from the rendering/display subsystem (another area where coupling tends to creep in). Ultimately the Unity-specific elements will be abstracted, so that the system architecture could be ported to other rendering platforms (game engines, three.js, etc). 

I am developing frame3Sharp to use in [gradientspace](https://www.gradientspace.com) products, first and foremost. However I do think that with the rise of distributed manufacturing - from mass customization to on-site medical device fabrication - there is a growing need for focused design tools that solve specific problems. My hope is that frame3Sharp will make this easy, or at least easier.


# Overview

Frame3Sharp (F3) concerns itself with the management and manipulation of a 3D scene that contains a set of objects and interface elements, as well as a 2.5D "heads-up display"-type overlay of additional interface elements. 

### Objects in the 3D Scene

At the topmost level there is an **FContext**, you can think of this as the "universe" in which everything exists. An FContext contains an **FScene**, which then contains **SceneObject**s and **SceneUIElement**s. The idea here is that a SceneObject is permanent - a cube, a sphere, etc - while a SceneUIElement is transient. So a 3D transformation gizmo would be a SceneUIElement, as would a clickable button that was in the 3D scene.

A number of SceneObject implementations are built-in. There are basic primitives like **SphereSO** and **BoxSO**, as well as 3D curves like **PolyCurveSO** and **PolyTubeSO**. **PivotSO** provides a way to represent 3D coordinate frames explicitly in the scene as manipulable objects. **MeshSO** wraps a unity Mesh, and provides functions for dynamic updates. 

**DMeshSO** wraps a [geometry3Sharp](https://github.com/gradientspace/geometry3Sharp) **DMesh3** dynamic mesh, which can be arbitrarily large, and provides a rich set of operations for remeshing, deformation, and so on. DMeshSO automatically decomposes large DMesh3 meshes into a set of unity Mesh objects (limited to 64k triangles) for rendering. DMeshSO also provides a spatial data structure which supports efficient queries like ray-intersection and nearest-point on the entire mesh.

### User Interface Cockpit / HUD

The FContext also contains a **Cockpit**, which manages the "near-field" user interface, which could be a 3D HUD in VR, or a set of layered 2D widgets in a desktop app. It is useful to think of this as 2.5D, and many of the objects work this way, with X/Y positioning (although in VR this might be X/Y on a cylinder or sphere) and Z as distance from the camera. The Cockpit elements are also **SceneUIElement** objects. The FContext maintains a stack of Cockpits, so you can "push" a new interface at any time. 

### 3D widgets and Gizmos

A common paradigm in 3D tools is that when you select something, you get a gizmo that lets you do something with it - translate, scale, resize, etc. The **TransformManager** in **FContext** handles this. Gizmos are classes that implement **ITransformGizmo**, and once registered with TransformManager, will be automatically managed when the FScene selection-set changes. The built-in **AxisTransformGizmo** implements a standard move/rotate/scale gizmo, including transforming groups, rotation around arbitrary pivot points, etc. This gizmo is composed of **Widget** sub-elements, the idea is that these Widgets can be re-assembled into other Gizmos.

### Tools

Another standard aspect of 3D tools is...the Tools. The FContext has a **ToolManager** which keeps track of which **ITool** is active. The set of possible tools must be registered with ToolManager at startup. Generally a Tool is modal, so when a Tool is active it takes control of the interaction to some extent. For example the provided **DrawPrimitivesTool** overrides the standard left mouse button behavior to draw primitives on left-click-drag. A separate Tool can be active on each input device, so if you are using VR hand controllers you can have a Tool in each hand.

Only a few Tools are provided, and writing a new Tool is a bit complicated. But we are working on abstractions, such as **BaseSingleClickTool** which allows you to easily implement a standard click-object-to-apply type of Tool with minimal code.

### Undo/Redo/History

**FScene** exposes a **ChangeHistory** object which implements standard undo/redo behavior via **IChangeOp** objects that apply/revert changes to the scene. A set of standard change operations are provided to enable undo/redo of add/delete objects, change materials, editing of primitive parameters, and 3D transformations. This is built in to the existing Tools and Gizmos, so for basic interfaces Undo is effectively "free".


### Cursors

If you're using a mouse, you generally have a 2D cursor. In Touch-based interfaces, you also have a cursor, although it behaves a bit differently. And finally in a VR app you might still want to use the mouse (gasp!), or a gamepad to control an on-screen cursor. FContext abstracts these different options via an **ICursorController**, which tracks the cursor and defines things like how the 2D cursor location maps to a 3D ray into the scene. **SystemMouseCursorController**, **TouchMouseCursorController**, and **VRMouseCursorController** provide implementations for the common cases.

### 3D Spatial-Input Controllers

VR controllers like the Vive wands and Oculus Touch break the standard 3D tool paradigm in many ways. The most immediate is that you can have two simultaneous "cursors", attached to tracked hand controllers. When using VR, and such a device is active, FContext provides a **SpatialInputController** class that tracks each hand and a 3D cursor/ray in the scene. Much of F3 already supports these devices, such as having two tools active at the same time. 

### Behavior System

One of the most complicated aspects of 3D tools is managing the huge input-device state machine that is built up over time. Even a relatively basic CAD tool has lots of different interactions - camera controls, selection, moving things around, drawing and editing tools, etc. Often this degenerates into a spaghetti of checks and branching.

F3 attempts (attempts!) to simplify this with its Behaviour system. FContext has a top-level **InputBehaviorSet** which collects **InputBehavior** objects from the scene, cockpit, and active tools, and determines how each input event should be handled. Any input events are converted to **InputState** events, which provide both abstract and low-level access to input device buttons, joysticks, postions, etc. 

An InputBehavior might be something as simple as a **MouseMultiSelectBehavior**, which selects objects on click, or as complex as an entire 3D camera control system. InputBehaviors can be written separately for different devices, for example **TouchViewManipBehavior** and **MouseViewManipBehavior**, and **SpatialDeviceViewManipBehavior** each provide camera controls for the relevant input type. F3 handles this all internally, you just have to write and register your InputBehaviors.

The *behaviors* folder contains a variety of standard behaviors, and some useful abstractions such as **MouseClickDragSuperBehavior**, which allows you to implement click-release vs click-drag actions as separate InputBehaviors and them compose them.


### UI Elements

Anything can be turned into an interactive user interface element by implementing **SceneUIElement**. Interactive 3D elements in the scene, like Gizmos, implement this. So do the 2.5D HUD UI elements in the Cockpit, which are in fact also full 3D objects, just positioned relative to the eye rather than the scene. As a result they can easily be used in 2D desktop apps, VR interfaces, and placed directly in the 3D scene.

We are implementing a standard set of UI elements - **HUDLabel**, **HUDButton**, **HUDTextEntry**, and so on, as well as containers such as **HUDPanel** and **HUDElementList**. A box-model layout system is in-progress. To display Text, Unity TextMesh objects are used, and optionally [TextMeshPro](https://www.assetstore.unity3d.com/en/#!/content/17662) support can be enabled for higher quality text rendering. 

The Unity 2D canvas system is *not* used by these UI elements. All elements are standard GameObjects. 

### Serialization

The set of current SceneObjects can be stored by **SceneSerializer** to an **IOutputStream** (**XMLOutputStream** is provided), or restored via **IInputStream**. You can support store/restore of custom SO types by registering serialize and build functions with the **SORegistry** stored in the Scene.

**SceneMeshImporter** supports reading in mesh files at runtime as MeshSO objects (DMeshSO support coming soon). Any format that [geometry3Sharp](https://github.com/gradientspace/geometry3Sharp) supports can be read, and for OBJ files, materials and texture maps will also be loaded. Similarly **SceneMeshExporter** can export any SOs that contains Meshes.


### TODO

animation, plugins, util, unity interop










# Project Setup

This codebase is intended to be included inside the **/Assets** folder of a Unity project. In terms of github usage, I use this project as a git submodule, so the structure/etc is setup to make that easy. But you can also just download it as a zip and drop it into Assets/frame3Sharp. 

**However, f3Sharp is not a standalone component, it has multiple dependencies which you must install separately**. Follow the instructions below. Alternately you might find it easier to just fork the [**frame3SharpSampleApp**](https://github.com/gradientspace/frame3SharpSampleApp) repository, which is a working Unity f3Sharp project *Note that this project does reference several submodules, which you need to make sure you also check out (most git clients will not automatically check out submodules).*

### geometry3Sharp

frame3Sharp depends on the **geometry3Sharp** repository: https://github.com/gradientspace/geometry3Sharp. This needs to be added to your Unity project, somewhere. Currently you must add this in source, a Unity package is eventually forthcoming.

### DOTween

Currently frame3Sharp references the **DOTWeen Unity package** [link](http://dotween.demigiant.com/). Also available in the Unity Asset Store, for free. Used for animations in some places. Removable with minor effort, if you prefer. *This dependency will be removed eventually*

### gsUnityVR

The default assumption is that you are building a VR App. In this case you will also need the **gsUnityVR** repository: https://github.com/gradientspace/gsUnityVR. This goes side-by-side with frame3Sharp. *This is a separate repository because it is quite large and is problematic to include if you are **not** building a VR App*.

If you are **not building a VR App**, you must do the following:

1) Add **F3_NO_VR_SUPPORT** to your *Scripting Define Symbols* in the *Player Settings*. This enables some some dummy C# classes that provide the gsUnityVR API without actually including all the VR bits (many classes in f3Sharp reference the VR API and automatically switch between VR and non-VR modes at runtime). 
2) Unzip */frame3Sharp/gsUnityVR.zip*. This creates an empty gsUnityVR subdirectory. This is necessary because the frame3Sharp assembly .asmdef file needs to have gsUnityVR as a dependency for it to work. When you unzip this file it creates a dummy assembly, which will avoid missing-reference errors.

### TextMeshPro

f3Sharp includes various objects that create 3D text. These text components currently can work with both standard Unity Text, and TextMeshPro text, which looks nicer and works better. TextMeshPro used to not be a free component, so currently this is optional. You must enable TextMeshPro mode by adding **F3_ENABLE_TEXT_MESH_PRO** to the *Scripting Define Symbols* in *Player Settings*.



# Scene Setup

f3Sharp depends on various things being set up your scene. The best way to figure this out is to look at the samples in  [**frame3SharpSampleApp**](https://github.com/gradientspace/frame3SharpSampleApp).

### Camera

The main camera must have tag **MainCamera**.

### Layers

f3Sharp uses several Layers, which must existing in your project. You can add these as any layer number, they are found by string name, but they must be in the following order:

* 3DWidgetOverlay
* HUDOverlay
* UIOverlay
* CursorOverlay






# Plugins

frame3Sharp includes several small external libraries to provide access to OS functionality inside Unity (which is otherwise not available). These are pre-compiled and included as DLLs/bundles/etc in the \Plugins folder

- [**tinyfiledialogs**](https://sourceforge.net/projects/tinyfiledialogs/) is a cross-platform library with zlib license that can show native file open and save dialogs.



# Documentation / etc

*will come in time!*

