# Background reading:
- https://web.archive.org/web/20181129043411/https://www.grc.nasa.gov/www/k-12/rocket/rktcontrl.html
- https://gafferongames.com/post/physics_in_3d/

# File Structure
## MissileGuidanceControlSystem.cs
To achieve a spirally rocket feel, combines two thruster types:
1. A powerful, naive "ideal" fixed main propulsion thruster
2. Many weak vernier thrusters at awkward orientations.

The verniers have "sensors" attached to them which conceptually represent the thrusters' pose
known to the guidance system. On the Unity prefab, these sensors are placed at slightly
perturbed poses (translation & rotation) which introduces systematic bias into the guidance
system. Physically, this means uncontrolled angular acceleration (spin) and lateral forces.

From a VFX perspective, the verniers introduce nonlinearities into the guidance system. These
nonlinearities cause the missile to "spiral" around chaotically. Introducing multiple verniers
is equivalent to introducing multiple nonlinearities into the system (many stacked chaotic
spirally patterns, if you will).

## Missile.cs
Of course, a smart missile guidance system would correct for these uncontrolled biases (i.e.
closed feedback loop). Instead, I encourage spirally behavior by only recomputing vernier
thruster contributions periodically. We could also simulate control system latency to achieve
similar behavior (though that might be harder to reason about).

To encourage missile convergence to destination acceleration and velocity are lerped toward
the goal direction. The lerp factor increases over missile lifetime. This achieves the feel of
missiles starting cartoonishly chaotic, eventually successfully homing onto their target.