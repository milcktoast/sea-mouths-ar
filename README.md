# Sea Mouths AR

> Unity + ARKit


## Concept

Draw sea creature mouths on surfaces, with each creature's mouth cavity appearing
to recede into the surface it was created upon.


## Interface

A user is presented with a floating cursor which appears fixed in space and
_sticks_ to surfaces by responding to point cloud data. Tapping the screen indicates
a selection, creating a vertex anchor in place of the cursor. After creating three
vertex anchors, a sea creature mouth is generated using the geometry of these vertices.

