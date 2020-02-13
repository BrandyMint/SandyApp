[DOCUMENTATION & INSTRUCTIONAL INFORMATION]
[Floaty Balloon - Physics Controller]
####################

This package consists of;
1) A .cs script that controls the movement of a balloon - this script is located in the 'Scripts' folder (Balloon_Ctrl.cs)
2) A prefab in the 'Resources' folder - add this to a scene for instant balloon that will follow your mouse and float-up when left-clicked.
3) A Scene included in the package (Example.unity) for an example of default usage.
_____________________________________________________________________________
#############################################################################

[Balloon_Ctrl.cs USAGE]
##############
@)Balloon_Ctrl.cs
	-Attach this script to a GameObject
	-Configure desired parameters in the inspector window - defaults already set. (see config below)
	-Click Play and see the magic happen.
	
[Balloon_Ctrl.cs INSPECTOR CONFIGURATIONS]
#######################
Within the inspector window, after attaching this script to a GameObject, you can customize a multitude of parameters of the script no coding required - see below...

#(CONFIG)
~(float) Sensitivity : Sets the sensitivity on X-axis (accelerometer sensitivity on mobile)
~(float) Up Force : Sets the amount of force to add on the Y-axis to allow floating (activated via left-click/touch - higher is faster)
~(bool) No Right Click : If false, X-axis following requires right-click during left-click. If true, X-axis following does not require right-click during left-click. (does not affect mobile)

