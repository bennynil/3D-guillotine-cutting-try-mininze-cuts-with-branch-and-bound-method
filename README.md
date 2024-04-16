# heuristic-and-branch-and-bound-method-to-minimize-cuts-take-in-3D-guillotine-cutting-problem
This is a C# project aimed at minimizing cuts in a 3D guillotine cutting problem and generating a txt file containing the cutting plan. Initially, it was just a school assignment, but I decided to incorporate the branch and bound method to achieve better results. Please note that the solution may not be optimal, as some options remain unexplored, such as the order of demand boxes and rotation of cutting.

This algorithm operates as follows:
1.read csv file to build a list of required boxes.

2.Sort all required box dimensions from largest to smallest.

3.Arrange all required boxes in non-decreasing order of dimensions.

4.Proceed with cutting in the specified order 
4-1.select boxes from non-required ones with the maximum number of similar dimensions and the largest difference in dimensions for cutting. 
4-2.During cutting, prioritize selecting boxes with similar dimensions. If there are no boxes with similar dimensions available, choose the box with the largest difference in dimensions for cutting.
4-3.step 4-2 will at most generate 3 non-required boxes.

5.Use the branch and bound method to select non-required boxes for cutting (following the cutting method described in step three).
output a txt file for cutting plan.

You can set the file path in the program. There is also a folder named "cutBoxesTest" in the project where you can place it in drive D for testing. 
Please note that processing might take a considerable amount of time. 
Currently, the project only supports one initial box and cannot be stopped during branch and bound.

This project is a work-in-progress, and I plan to improve it further if I have some free time. hope it would be helpful for others facing a similar problem.


