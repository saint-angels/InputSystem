# Review


# How I approached the problem
When writing code for my Circle Interaction, I've used the existing MouseVisualizer scene, because it provided nice visuals of the mouse movements.

I've found an Objective-C code for circle gesture recognition for iOS, and used it as the basis for my interaction logic.

At first, I did continuous recognition in the stream of inputs, because it was simpler to set up.
After that, I figured out how to use a simple binding modifier.
With the modifier, mouse movements would start recording only when the mouse button was pressed, and circle recognition would execute only after mouse button release.


# Obstacles
The documentation was the main obstacle in my way. 
I think the docs do a good job at explaining how to use the Input System in general and how its systems work individually. But when you need to connect these systems in a custom way, it can be very hard to figure out.

The documentation just doesn't tell you how to use methods/fields in sufficient detail.
And the example code samples are often too brief.


# How I would improve the solution
- make gesture recognition tests
- add gesture code comments/documentation
- hook up the interaction to a gesture visualizer, to see recorded mouse positions
- support different input methods, including gamepad sticks 
- write documentation that highlights all usage scenarios
- find a better way to process an input binding with a modifier 
Now my interaction handles 2 control types(mouse positions and a mouse click) in a hacky way:  
if (context.control.valueType == typeof(System.Single))
