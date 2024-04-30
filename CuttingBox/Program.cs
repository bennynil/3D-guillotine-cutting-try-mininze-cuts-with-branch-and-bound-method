using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.IO;

class Program
{

    static List<string> solutionsStrings = new List<string>();
    static string filePath = "D:\\cutBoxesTest\\testData.csv";
    static string solutionPath = "D:\\cutBoxesTest\\anss.txt";
    static bool useBranchAndBound = true;

    static void Main(string[] args)
    {
        // read csv file
        List<int> length = new List<int>();
        List<int> width = new List<int>();
        List<int> height = new List<int>();
        List<int> quantity = new List<int>();
        int tg = 1;
        bool startbox = false;
        List<Box> boxes = new List<Box>();



        
        try
        {
            using(StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {

                    if (!containNum(line))
                    {
                        continue;
                    }
                    string[] parts = line.Split(',');
                    if (!startbox)
                    {
                        Box initialBin = new Box(new int[] { int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]) });

                        //add initial bin(only one)
                        boxes.Add(initialBin);
                        startbox = true; 
                    }
                    else
                    {
                        length.Add(int.Parse(parts[0]));
                        width.Add(int.Parse(parts[1]));
                        height.Add(int.Parse(parts[2]));
                        quantity.Add(int.Parse(parts[3]));
                        tg++;
                    }


                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("bug");
        }

        



        List<Box> needs = new List<Box>();

        
        //add require boxes to list
        for (int i = 0; i < tg; i++)
        {
            for (int j = 0; j < quantity[i]; j++)
            {
                int[] dimen = new int[] { length[i], width[i], height[i] };
                Array.Sort(dimen);
                Array.Reverse(dimen);
                Box newBoxes = new Box(dimen);
                needs.Add(newBoxes);
            }
        }
        //sort dimesnsions to non_decreasing order
        needs = needs.OrderByDescending(z => z.threeDimension[0])
                     .ThenByDescending(z => z.threeDimension[1])
                     .ThenByDescending(z => z.threeDimension[2])
                     .ToList();

        //try solving the problem using First fit non_decreasing order
        int preCutBox = 0;
        List<Box> result = new List<Box>();
        List<Box> needsTemp = needs.ConvertAll(box => box.Copy());
        List<Box> boxesTemp = boxes.ConvertAll(box => box.Copy());
        List<solutionLog> bestSolution = new List<solutionLog>();


        int wastev = 0;
        List<Box> waste = new List<Box>();


        for(int index = 0; index < needsTemp.Count; index++)
        {
            preCutBox = Cut(needsTemp[index], preCutBox, result, boxesTemp, bestSolution, ref index, needsTemp, waste, ref wastev);





        }

        Console.WriteLine(preCutBox);

        Console.WriteLine(wastev);
        int bestCut = preCutBox;

        if (useBranchAndBound)
        {
            List<solutionLog> initSol = new List<solutionLog>();
            //use branch and bound to decide which box is choose to generate require box
            int cutBox = RecursiveCut(boxes, 0, new List<Box>(), ref bestCut, needs, 0, initSol, ref bestSolution, new List<Box>(), ref waste,ref wastev, 0);
        }

        



        Console.WriteLine("Best number of cuts: " + bestCut);

        int totalVolume = boxes.Sum(box => box.threeDimension[0] * box.threeDimension[1] * box.threeDimension[2]);
        Console.WriteLine("Total volume: " + totalVolume);

        //check if the solution is valid;
        for(int i = 0; i < bestSolution.Count; i++)
        {
            if(i > 0)
            {
                if(!BoxListContainAlmostEqual(bestSolution[i - 1].boxes, bestSolution[i].from))
                {
                    Console.WriteLine("invalid solution ?");
                }
            }
        }

        foreach (solutionLog sl in bestSolution)
        {
            Console.WriteLine(sl.ToString());

            solutionsStrings.Add(sl.ToString());
        }



        
        //write answer
        try
        {

            if (!File.Exists(solutionPath))
            {
                File.Create(solutionPath);
            }

            StreamWriter solutionWriter = new StreamWriter(solutionPath);


            solutionWriter.WriteLine("cut: " + bestCut);

            solutionWriter.WriteLine("step");

            foreach(string solution in solutionsStrings)
            {
                solutionWriter.WriteLine(solution);
            }
            solutionWriter.Close();

        }
        catch (Exception ex)
        {
            Console.WriteLine("write error");
        }
        
    }

    static int Cut(Box need, int cutBox, List<Box> result, List<Box> boxes, List<solutionLog> solution, ref int index, List<Box> needs, List<Box> waste, ref int wastev)
    {
        int bestFitIndex = -1;
        int bestFitRank = -1;
        int bestFitRankDis = -1;

        Box TempNeed = need.Copy();
        Box source;

        List<int> dimension = new List<int>();
        for (int i = 0; i < boxes.Count; i++)
        {
            if (boxes[i].BiggerThan(need))
            {
                int a = boxes[i].Rank(need);
                int b = boxes[i].RankDistance(need);
                
                if(a > bestFitRank)
                {
                    bestFitIndex = i;
                    bestFitRank = a;
                    bestFitRankDis = b;
                }
                else if(a == bestFitRank && b >= bestFitRankDis)
                {
                    bestFitIndex = i;
                    bestFitRank = a;
                    bestFitRankDis = b;
                }
            }
        }

        if(bestFitIndex != -1)
        {
            source = boxes[bestFitIndex].Copy();

            boxes.RemoveAt(bestFitIndex);
            result.Add(TempNeed.Copy());
        }
        else
        {
            waste.Add(need);
            wastev += need.getVolumn();
            return cutBox;
        }

        Box from = source.Copy();
        int cutInIndex = 0;
        string how = "";

        for (int j = 0; j < 3; j++)
        {
            int tempNum = 0;
            int bestMatchDimension = FindSmallestDistanceDimension(need, source, dimension);
            dimension.Add(bestMatchDimension);


            if (source.threeDimension[bestMatchDimension] != need.threeDimension[j])
            {
                Box newBox = new Box(source.threeDimension.Select(x => x).ToArray());
                tempNum = source.threeDimension[bestMatchDimension] - need.threeDimension[j];
                cutBox += 1;
                cutInIndex++;
                newBox.threeDimension[bestMatchDimension] = tempNum;
                Array.Sort(newBox.threeDimension);
                Array.Reverse(newBox.threeDimension);

                boxes.Add(newBox);
            }


            source.threeDimension[bestMatchDimension] = need.threeDimension[j];

            how += "\n" + source.ToString() + "\n";
            need.threeDimension[j] = 0;

            
        }

        Box to = source;
        solution.Add(new solutionLog(from, to, boxes.ConvertAll(x => x.Copy()), index, false, cutInIndex, how));

        if (need.threeDimension.Sum() == 0)
        {
            int q = 0;
            List<Box> needsTemp = new List<Box>();

            for (int x = index + 1; x < needs.Count; x++)
            {
                if (BoxListContainAlmostEqual(boxes, needs[x]))
                {
                    Box needTemp = needs[x].Copy();
                    BoxListRemove(boxes, needs[x]);
                    needsTemp.Add(needTemp);
                    needs.RemoveAt(x);
                    Box fromto = needTemp.Copy();
                    solution.Add(new solutionLog(fromto, fromto, boxes.ConvertAll(x => x.Copy()), x, true, 0, ""));
                    //Console.WriteLine("creat log at in : " + x);
                    result.Add(needTemp);
                    q++;
                }
            }

            needs.InsertRange(index + 1, needsTemp);

            index += q;

            return cutBox;
        }
        else
        {
            return int.MaxValue;
        }
    }

    static int RecursiveCut(List<Box> boxes, int cutBox, List<Box> result, ref int bestCut, List<Box> needs, int index, List<solutionLog> solution, ref List<solutionLog> bsts, List<Box> waste, ref List<Box> wasteSol, ref int wastevSol, int wastev)
    {

        if (!isBetterSol(cutBox, bestCut, wastev, wastevSol) || index >= needs.Count)
        {
            if(isBetterSol(cutBox, bestCut, wastev, wastevSol) && index == needs.Count)
            {
                bestCut = cutBox;

                wasteSol = waste;

                bsts = solution;

                Console.WriteLine("find new best solution with best cut in: " + bestCut + " min waste: " + wasteSol + "testpgg");
            }
            
            return cutBox;
        }

        int mincut = int.MaxValue;
        int cutBoxt = cutBox;
        bool hasBigger = false;

        for (int i = 0; i < boxes.Count; i++)
        {
            int indexTemp = index;
            Box need = needs[indexTemp].Copy();

            if (boxes[i].BiggerThan(need))
            {

                hasBigger = true;

                List<solutionLog> copySolution = solution.ConvertAll(s => s.Copy());
                cutBox = cutBoxt;
                List<Box> boxesTemp = boxes.ConvertAll(x => x.Copy());
                List<Box> resultTemp = result.ConvertAll(x => x.Copy());

                List<int> dimension = new List<int>();
                Box source = boxesTemp[i].Copy();

                boxesTemp.RemoveAt(i);

                Box from = source.Copy();

                int cutInIndex = 0;
                string how = "";
                //6 rotation cut plan;
                for (int j = 0; j < 3; j++)
                {
                    int tempNum = 0;
                    int bestMatchDimension = FindSmallestDistanceDimension(need, source, dimension);
                    dimension.Add(bestMatchDimension);
                    //Console.WriteLine("source before : " + source.ToString());
                    if (source.threeDimension[bestMatchDimension] != need.threeDimension[j])
                    {
                        Box newBox = new Box(source.threeDimension.Select(x => x).ToArray());
                        tempNum = source.threeDimension[bestMatchDimension] - need.threeDimension[j];
                        cutBox += 1;

                        cutInIndex += 1;
                        newBox.threeDimension[bestMatchDimension] = tempNum;
                        Array.Sort(newBox.threeDimension);
                        Array.Reverse(newBox.threeDimension);
                        //Console.WriteLine(newBox);
                        boxesTemp.Add(newBox);
                    }

                    source.threeDimension[bestMatchDimension] = need.threeDimension[j];
                    //Console.WriteLine("source after : " + source.ToString());

                    how += "\n" + source.ToString() + "\n";
                    need.threeDimension[j] = 0;
                }

                Box to = source;
                copySolution.Add(new solutionLog(from, to, boxesTemp.ConvertAll(x => x.Copy()), indexTemp, false, cutInIndex, how));

                //Console.WriteLine("creat log at in : " + index);

                if (need.threeDimension.Sum() == 0)
                {

                    resultTemp.Add(needs[indexTemp].Copy());

                    List<Box> needsTemp = new List<Box>();
                    int q = 0;
                    for (int x = indexTemp + 1; x < needs.Count; x++)
                    {
                        if (BoxListContainAlmostEqual(boxesTemp, needs[x]))
                        {
                            Box needTemp = needs[x].Copy();
                            BoxListRemove(boxesTemp, needs[x]);
                            needsTemp.Add(needTemp);
                            needs.RemoveAt(x);
                            Box fromto = needTemp.Copy();
                            copySolution.Add(new solutionLog(fromto, fromto, boxesTemp.ConvertAll(x => x.Copy()), x, true, 0, ""));
                            //Console.WriteLine("creat log at in : " + x);
                            resultTemp.Add(needTemp);
                            q++;
                        }
                    }

                    needs.InsertRange(indexTemp + 1, needsTemp);

                    indexTemp += q;
                }
                else
                {
                    return int.MaxValue;
                }

                if (cutBox < bestCut)
                {
                    boxesTemp = boxesTemp.OrderByDescending(x => x.threeDimension[0])
                                         .ThenByDescending(x => x.threeDimension[1])
                                         .ThenByDescending(x => x.threeDimension[2])
                                         .ToList();


                    int newCutBox = RecursiveCut(boxesTemp, cutBox, resultTemp, ref bestCut, needs, indexTemp + 1, copySolution, ref bsts, waste.ConvertAll(x => x.Copy()), ref wasteSol, ref wastevSol, wastev);
                    if (newCutBox <= mincut)
                    {
                        mincut = newCutBox;
                    }

                }
                else
                {
                    return int.MaxValue;
                }

                //Console.WriteLine("logCount: " + copySolution.Count);
            }
        }

        if (!hasBigger)
        {
            List<Box> _boxesTemp = boxes.ConvertAll(x => x.Copy());
            List<Box> _resultTemp = result.ConvertAll(x => x.Copy());
            List<solutionLog> _copySolution = solution.ConvertAll(s => s.Copy());

            List<Box> wasteCopy = waste.ConvertAll(x => x.Copy());
            int wasteNew = wastev += needs[index].getVolumn();

            int _newCutBox = RecursiveCut(_boxesTemp, cutBox, _resultTemp, ref bestCut, needs, index + 1, _copySolution, ref bsts, wasteCopy, ref wasteSol, ref wastevSol, wasteNew);
            if (_newCutBox <= mincut)
            {
                mincut = _newCutBox;
            }
        }
        
        return mincut;
    }

    static int FindSmallestDistanceDimension(Box need, Box source, List<int> source2)
    {
        bool m = false;
        int largestNeed = need.threeDimension.Max();
        int Val = int.MinValue;
        int bestDimension = -1;
        for (int i = 0; i < 3; i++)
        {
            if (!source2.Contains(i))
            {
                int distance = Math.Abs(largestNeed - source.threeDimension[i]);
                if(distance == 0)
                {
                    bestDimension = i;
                }
                else if (source.threeDimension[i] - largestNeed >= 0 && distance > Val)
                {
                    Val = distance;
                    bestDimension = i;
                }
            }
        }

        return bestDimension;
    }

    static bool BoxListContainAlmostEqual(List<Box> boxes, Box box)
    {
        foreach (Box obj in boxes)
        {
            if (box.EqualTo(obj))
            {
                return true;
            }
        }
        return false;
    }

    static void BoxListRemove(List<Box> boxes, Box box)
    {
        foreach (Box obj in boxes)
        {
            if (box.EqualTo(obj))
            {
                boxes.Remove(obj);
                return;
            }
        }
    }

    static bool containNum(string line)
    {
        //testing
        string[] numbe = new string[10] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };

        foreach(string obj in numbe)
        {
            if (line.Contains(obj))
            {
                return true;
            }
        }

        return false;
    }

    static bool isBetterSol(int nc, int bc, int nw, int bw)
    {
        if (nw < bw)
        {
            return true;
        }
        else if (nc < bc && nw == bw)
        {
            return true;
        }

        return false;
    }
}


class Box
{
    public int[] threeDimension;

    public Box(int[] _threedimension)
    {
        threeDimension = _threedimension;
    }

    public bool BiggerThan(Box box2)
    {
        return threeDimension[0] >= box2.threeDimension[0] &&
               threeDimension[1] >= box2.threeDimension[1] &&
               threeDimension[2] >= box2.threeDimension[2];
    }

    public bool EqualTo(Box box2)
    {
        return threeDimension[0] == box2.threeDimension[0] &&
               threeDimension[1] == box2.threeDimension[1] &&
               threeDimension[2] == box2.threeDimension[2];
    }

    public int Rank(Box box2)
    {
        int rank = 0;

        if(threeDimension[0] == box2.threeDimension[0]) { rank++; }
        if(threeDimension[1] == box2.threeDimension[1]) { rank++; }
        if(threeDimension[2] == box2.threeDimension[2]) { rank++; }

        return rank;
    }

    public int RankDistance(Box box2)
    {
        int rank = 0;
        Box need = box2.Copy();
        Box source = Copy();
        List<int> dimension = new List<int>();

        for (int j = 0; j < 3; j++)
        {
            int tempNum = 0;
            int bestMatchDimension = FindSmallestDistanceDimension(need, source, dimension);
            dimension.Add(bestMatchDimension);


            if (source.threeDimension[bestMatchDimension] != need.threeDimension[j])
            {
                Box newBox = new Box(source.threeDimension.Select(x => x).ToArray());
                tempNum = source.threeDimension[bestMatchDimension] - need.threeDimension[j];
                rank += tempNum;
                newBox.threeDimension[bestMatchDimension] = tempNum;
                Array.Sort(newBox.threeDimension);
                Array.Reverse(newBox.threeDimension);
            }


            source.threeDimension[bestMatchDimension] = need.threeDimension[j];
            need.threeDimension[j] = 0;


        }

        return rank;
    }

    static int FindSmallestDistanceDimension(Box need, Box source, List<int> source2)
    {
        bool m = false;
        int largestNeed = need.threeDimension.Max();
        int Val = int.MinValue;
        int bestDimension = -1;
        for (int i = 0; i < 3; i++)
        {
            if (!source2.Contains(i))
            {
                int distance = Math.Abs(largestNeed - source.threeDimension[i]);
                if (distance == 0)
                {
                    bestDimension = i;
                }
                else if (source.threeDimension[i] - largestNeed >= 0 && distance > Val)
                {
                    Val = distance;
                    bestDimension = i;
                }
            }
        }

        return bestDimension;
    }

    public Box Copy()
    {
        int[] _threeDimension = threeDimension.Select(x => x).ToArray();
        return new Box(_threeDimension);
    }

    public override string ToString()
    {
        return $"{threeDimension[0]}, {threeDimension[1]}, {threeDimension[2]}";
    }


    public int getVolumn()
    {
        return threeDimension[0] * threeDimension[1] * threeDimension[2];
    }
}

class solutionLog
{

    public Box from;
    public Box to;
    public string how;
    public List<Box> boxes;

    public int index;

    public bool fast;

    public int cutInIndex;
    public solutionLog(Box _from, Box _to, List<Box> _boxes, int _index, bool _fast, int _cutInIndex, string _howto)
    {
        from = _from;
        to = _to;
        boxes = _boxes;
        index = _index;
        fast = _fast;
        cutInIndex = _cutInIndex;
        how = _howto;
    }

    public string ToString()
    {
        string line = "from: " + from.ToString() + "\n" + "to: " + to.ToString() + "\n";
        if (how != "")
            line += "cut step: " + how;
        line += "boxes: " + "\n";


        foreach (Box box in boxes)
        {
            line += " " + box.ToString() + " " + "\n";
        }

        line += "index: " + index + "\n";

        line += "fast: " + fast+ "\n";

        line += "cutInIndex: " + cutInIndex + "\n";
        return line;
    }

    public solutionLog Copy()
    {
        return new solutionLog(from, to, boxes.ConvertAll(x => x.Copy()), index, fast, cutInIndex, how);
    }
}
