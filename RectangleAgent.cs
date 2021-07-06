using GeometryFriends;
using GeometryFriends.AI;
using GeometryFriends.AI.Communication;
using GeometryFriends.AI.Interfaces;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace GeometryFriendsAgents
{
    public class RectangleAgent : AbstractRectangleAgent
    {
        //agent implementation specificiation
        private bool implementedAgent;
        private string agentName = "RandRect";

        //auxiliary variables for agent action
        private Moves currentAction;
        private List<Moves> possibleMoves;
        private List<Moves> possibleMovesHorizontal;
        private double lastMoveTime;
        private Random rnd;

        private bool doOnce = true;
        private bool done = false;

        private bool aboutToCatch = false;
        private int prevCollectibles = 0;

        private int TIME_PER_MOVE = 500;
        private char[,] map =  new char[720,1200];

        private readonly float MORPHING_BUFF = 50;
        readonly bool PRINT_ON =  false;

        private Moves prevMove = Moves.MOVE_RIGHT;
        private int steps = 10;
        private int stuckCounter = 0;
        private Moves stuckAction = Moves.NO_ACTION;
        private RectangleRepresentation stuckPosition;
        

        struct Node
        {
            public RectangleRepresentation state; // agent representation
            public double f; //utility
            public int steps; // steps from the start
            public Moves move; // move that lead to that state

            public Node(RectangleRepresentation r, double utility, int step,Moves mv)
            {
                state = r;
                f = utility;
                steps = step;
                move = mv;
            }
        }

        //Sensors Information
        private CountInformation numbersInfo;
        private RectangleRepresentation rectangleInfo;
        private CircleRepresentation circleInfo;
        private ObstacleRepresentation[] obstaclesInfo;
        private ObstacleRepresentation[] rectanglePlatformsInfo;
        private ObstacleRepresentation[] circlePlatformsInfo;
        private CollectibleRepresentation[] collectiblesInfo;

        private int nCollectiblesLeft;

        private List<AgentMessage> messages;

        //Area of the game screen
        protected Rectangle area;

        public RectangleAgent()
        {
            //Change flag if agent is not to be used
            implementedAgent = true;

            lastMoveTime = DateTime.Now.Second;
            currentAction = Moves.NO_ACTION;
            rnd = new Random();

            //prepare the possible moves  
            possibleMoves = new List<Moves>();
            possibleMoves.Add(Moves.MOVE_LEFT);
            possibleMoves.Add(Moves.MOVE_RIGHT);
            possibleMoves.Add(Moves.MORPH_UP);
            possibleMoves.Add(Moves.MORPH_DOWN);

            stuckPosition.X = 0;
            stuckPosition.Y = 0;

            //init local variables
            for (int i = 0; i < 720; i++)
            {
                for (int j = 0; j < 1200; j++)
                {
                    map[i, j] = '-';
                }
               
            }

            //messages exchange
            messages = new List<AgentMessage>();
        }

        //implements abstract rectangle interface: used to setup the initial information so that the agent has basic knowledge about the level
        public override void Setup(CountInformation nI, RectangleRepresentation rI, CircleRepresentation cI, ObstacleRepresentation[] oI, ObstacleRepresentation[] rPI, ObstacleRepresentation[] cPI, CollectibleRepresentation[] colI, Rectangle area, double timeLimit)
        {
            numbersInfo = nI;
            nCollectiblesLeft = nI.CollectiblesCount;
            rectangleInfo = rI;
            circleInfo = cI;
            obstaclesInfo = oI;
            rectanglePlatformsInfo = rPI;
            circlePlatformsInfo = cPI;
            collectiblesInfo = colI;
            this.area = area;

            //send a message to the rectangle informing that the circle setup is complete and show how to pass an attachment: a pen object
            messages.Add(new AgentMessage("Setup complete, testing to send an object as an attachment.", new Pen(Color.BlanchedAlmond)));

            //DebugSensorsInfo();
        }

        //implements abstract rectangle interface: registers updates from the agent's sensors that it is up to date with the latest environment information
        public override void SensorsUpdated(int nC, RectangleRepresentation rI, CircleRepresentation cI, CollectibleRepresentation[] colI)
        {
            nCollectiblesLeft = nC;

            rectangleInfo = rI;
            circleInfo = cI;
            //Array.Clear(collectiblesInfo, 0, collectiblesInfo.Length);
            collectiblesInfo = colI;
        }

        //implements abstract rectangle interface: signals if the agent is actually implemented or not
        public override bool ImplementedAgent()
        {
            return implementedAgent;
        }

        //implements abstract rectangle interface: provides the name of the agent to the agents manager in GeometryFriends
        public override string AgentName()
        {
            return agentName;
        }

        //Generates the map of the agent from the objects refernces
        private void CalculateMap(CollectibleRepresentation target)
        {
            for (int i = 0; i < 720; i++)
            {
                for (int j = 0; j < 1200; j++)
                {
                    map[i, j] = '-';
                }

            }
            foreach (ObstacleRepresentation o in obstaclesInfo)
            {         
                for(int i = (int)(o.Y - o.Height/2)-40; i < (o.Y + o.Height / 2)-40; i++)
                {
                    for (int j = (int)(o.X - o.Width / 2)-40; j < (o.X + o.Width / 2)-40; j++)
                    {
                       
                        if (i < 0)
                        {
                            i = 0;

                        }else if(i >= 720)
                        {
                            i = 719;
                            goto nextttt; 
                        }

                        if (j < 0)
                        {
                            j = 0;
                        }
                        else if (j >= 1200)
                        {
                            j = 1199;
                            goto next;
                        }
                        //Debug.WriteLine("( " + i +" , " + j + " )");
                        map[i, j] = '+';
                    }
                    next:;
                }
                nextttt:;
            }
            
            
            //Debug.WriteLine("-1");
            //RectangleRepresentation r = rectangleInfo;
            //int width = 10000 / (int)r.Height;

            //    for (int i = (int)(r.Y - r.Height / 2) - 40; i < (r.Y + r.Height / 2) - 40; i++)
            //    {
            //        for (int j = (int)(r.X - width / 2) - 40; j < (r.X + width / 2) - 40; j++)
            //        {

            //            if (i < 0)
            //            {
            //                i = 0;
            //            }
            //            else if (i >= 720)
            //            {
            //                i = 719;
            //                break;
            //            }

            //            if (j < 0)
            //            {
            //                j = 0;
            //            }
            //            else if (j >= 1200)
            //            {
            //                j = 1199;
            //                break;
            //            }
            //            //Debug.WriteLine("( " + i +" , " + j + " )");
            //            map[i, j] = '=';
            //        }
            //    }
            //Debug.WriteLine("-2");
            int line, column;
            int[] ar = new int[2];

            //foreach (CollectibleRepresentation o in collectiblesInfo)
            { 
            CollectibleRepresentation o = target;
            
                column = 1;
                line = 1;

                for (int i = (int)(o.Y) - 40-35; i < (o.Y) - 40 + 35; i++)
                {
                    for (int j = (int)(o.X) - 40-35; j < (o.X) - 40 + 35; j++)
                    {

                        if (i < 0)
                        {
                            i = 0;
                        }
                        else if (i >= 720)
                        {
                            i = 719;
                            goto next2;
                        }

                        if (j < 0)
                        {
                            j = 0;
                        }
                        else if (j >= 1200)
                        {
                            j = 1199;
                            goto next2;
                        }
                        //Debug.WriteLine("( " + i +" , " + j + " )");

                        ar[0] = line;
                        ar[1] = column;
                        if((ar.Max()-ar.Min()) == 35)
                        {
                            map[i, j] = '*';
                        }
                        if ((ar.Max() + ar.Min()) == 36)
                        {
                            map[i, j] = '*';
                        }
                        if ((ar.Max() + ar.Min()) == 105)
                        {
                            map[i, j] = '*';
                        }


                        line++;
                    }
                    line = 1;
                    column++;
                }
                next2:;
            }
        }

        //Prints the map of the agent
        private void PrintMap()
        {
            for(int i = 0; i < 720; i++)
            {
                for (int j = 0; j < 1200; j++)
                {
                    Debug.Write(map[i,j]);
                }
                Debug.WriteLine("||| " + i);
            }
        }

        // This function finds the closest target considering the order
        private CollectibleRepresentation Findtarget()
        {
            // find closest target that is above the agent
            double min = 100000;
            int minIndex = -1;
            int counter = 0;

            foreach(CollectibleRepresentation c in collectiblesInfo)
            {
                if( CalculateDistance(rectangleInfo,c) < min && c.Y < (rectangleInfo.Y ))
                {
                    minIndex = counter;
                    min = CalculateDistance(rectangleInfo, c);
                }
                counter++;
            }

            if(minIndex != -1)
            {
                return collectiblesInfo[minIndex];
            }

            counter = 0;
            foreach (CollectibleRepresentation c in collectiblesInfo)
            {
                if (CalculateDistance(rectangleInfo, c) < min )
                {
                    minIndex = counter;
                    min = CalculateDistance(rectangleInfo, c);
                }
                counter++;
            }

            return collectiblesInfo[minIndex];
        }

        //algorithm choosing the best action for the rectangle agent
        private void FindMoveAStar()
        {

            if(rectangleInfo.VelocityY > 10)
            {
                //if (PRINT_ON)
                    Debug.WriteLine("Falliing...............");
                //if(prevMove == Moves.MOVE_RIGHT)
                //{
                //    currentAction = Moves.MOVE_LEFT;
                //}
                //else
                //{
                //    currentAction = Moves.MOVE_RIGHT;
                //}
                currentAction = prevMove;
                TIME_PER_MOVE = 800;
                return;
            }
            //Debug.WriteLine("----+-1");
            
            //PrintMap();
            Debug.WriteLine("test0");
            //set target Collectible
            bool close;
            CollectibleRepresentation target = Findtarget(); // find closest target
            Debug.WriteLine("test1");
            CalculateMap(target);
            Debug.WriteLine("test2");
            if (CalculateDistance(rectangleInfo,target) > 250)
            {
                TIME_PER_MOVE = 450;
                close = false;
            }
            else
            {
                TIME_PER_MOVE = 300;
                close = true;
            }
            int StepsWeight = 1;
            

            if (prevCollectibles == collectiblesInfo.Length && aboutToCatch)
            {
                currentAction = Moves.MORPH_UP;
                Debug.WriteLine("Bugged so we morphup!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                aboutToCatch = false;
                return;
            }
            else
            {
                aboutToCatch = false;
            }
            prevCollectibles = collectiblesInfo.Length;
            Debug.WriteLine("target point: " + target.ToString());

            RectangleRepresentation currentState = rectangleInfo,tmpState;

            List <Node> nodes = new List<Node>();
            List<Node> nodesClosed = new List<Node>();

            Node n = new Node(currentState,0,0,Moves.NO_ACTION);
            nodes.Add(n);
            
            int indexMin,counter,iterator=0,oldHeight;
            double fMin, newF;
            bool legitimate;
            //Debug.WriteLine("----1");
            //Keep searching till agent reaches target or it rreaches the maximum number of iterations
            while (nodes.Count() != 0 && iterator <500)
            {
                //exterminate duplicates
                //nodes = nodes.Distinct().ToList();

                iterator ++;
                steps = iterator;

                indexMin = -1;
                fMin = 1000000;
                counter = 0;

                Debug.WriteLine("----+"+iterator);

                foreach (Node tmpNode in nodes)
                {
                    if (tmpNode.f < fMin)
                    {
                        indexMin = counter;
                        fMin = tmpNode.f;
                    }
                    counter++;
                }

                n = nodes[indexMin]; // !!!!!!may need malloc or new
                currentState = n.state;
                nodes.RemoveAt(indexMin);
                if(PRINT_ON)
                    Debug.WriteLine("Node: " + n.state.ToString() +" f: " + n.f + " steps: " + n.steps + " number of nodes  left in list : " + nodes.Count + " Initial Move: "+ n.move );

                //***********Generate Kids of node*************

        //--------------------------MOVE_LEFT 
                tmpState = currentState;
               
                legitimate = true;
                if((int)tmpState.X - 5000 / ((int)tmpState.Height)  - 27 < 40)
                {
                    goto move_right;
                }
                tmpState.X -= 27;
                //Debug.WriteLine("Left");
                //check if it is possible to move there ,or if the agent moved to empty space(using map)
                tmpState = SimulatePhysics(currentState, tmpState, Moves.MOVE_LEFT);
                if (tmpState.X == currentState.X)
                {
                    goto move_right;
                }
                
                //Check if overlapping the target(using map)
                if (OverlappingTarget(tmpState))
                {
                    if(n.steps == 0)
                    {
                        n.move = Moves.MOVE_LEFT;
                        aboutToCatch = true;
                    }
                    Debug.WriteLine("reached destination A* : " + target.ToString());
                    currentAction = n.move;
                    return;
                }
                
                //Calculate new f(n) = g(n) + h(n)
                newF = CalculateDistance(tmpState, target) + n.steps * StepsWeight;

                foreach (Node tmpNode in nodes)
                {
                    // if this state doesn't already exists in open list
                    //if(!(CalculateDistance(tmpState,tmpNode.state) < 10 ) || !(tmpState.Height-tmpNode.state.Height < 10) || !(tmpNode.f < newF))
                    //Debug.WriteLine(" Open Left Distance: " + CalculateDistance(tmpState, tmpNode.state) + " f differencee: " + (tmpNode.f - newF));
                    if (CalculateDistance(tmpState,tmpNode.state) < 10 && tmpNode.f < newF)
                    {
                        legitimate = false;
                        //Debug.WriteLine(" already exists  open");
                        goto move_right;
                    }
                    else
                    {
                        legitimate = true;
                    }
                }


                foreach (Node tmpNode in nodesClosed)
                {
                    // if this state doesn't already exists in open list
                    //Debug.WriteLine(" Closed Left Distance: " + CalculateDistance(tmpState, tmpNode.state) + " f differencee: " + (tmpNode.f - newF) );
                    if(CalculateDistance(tmpState,tmpNode.state) < 10 && tmpNode.f < newF)
                    {
                        legitimate = false;
                        //Debug.WriteLine(" already exists  closed");
                        goto move_right;
                    }
                    else
                    {
                        legitimate = true;
                    }
                }

                //Add kid to actions list
                if (legitimate)
                {
                    if (n.steps == 0)
                    {
                        nodes.Add(new Node(tmpState, newF, n.steps + 1, Moves.MOVE_LEFT));
                    }
                    else
                    {
                        nodes.Add(new Node(tmpState, newF, n.steps + 1, n.move));
                    }
                }



                //--------------------------MOVE_RIGHT
                move_right:;
                tmpState = currentState;
                legitimate = true;
                //Debug.WriteLine("Right");
                if ((int)tmpState.X - 5000 / ((int)tmpState.Height) + 27 > 1200)
                {
                    goto morph_up;
                }
                tmpState.X += 27;
                

                tmpState = SimulatePhysics(currentState, tmpState,Moves.MOVE_RIGHT);
                if(tmpState.X == currentState.X)
                {
                    goto morph_up;
                }

                if (tmpState.Height == 0)
                {
                    if (n.steps == 0)
                    {
                        n.move = Moves.MOVE_RIGHT;
                    }
                    Debug.WriteLine("reached destination A* through fall: " + target.ToString());
                    currentAction = n.move;
                    return;
                }

                //Check if overlapping the target(using map)
                if (OverlappingTarget(tmpState))
                {
                    if (n.steps == 0)
                    {
                        n.move = Moves.MOVE_RIGHT;
                        aboutToCatch = true;
                    }
                    Debug.WriteLine("reached destination A* : " + target.ToString());
                    currentAction = n.move;
                    return;
                }

                //Calculate new f(n) = g(n) + h(n)
                newF = CalculateDistance(tmpState, target) + n.steps * StepsWeight;

                foreach (Node tmpNode in nodes)
                {
                    // if this state doesn't already exists in open list
                    if (CalculateDistance(tmpState, tmpNode.state) < 10 && tmpNode.f < newF)
                    {
                        legitimate = false;
                        //Debug.WriteLine(" already exists  open");
                        goto morph_up;
                    }
                    else
                    {
                        legitimate = true;
                    }
                }


                foreach (Node tmpNode in nodesClosed)
                {
                    // if this state doesn't already exists in open list
                    if (CalculateDistance(tmpState, tmpNode.state) < 10 && tmpNode.f < newF)
                    {
                        legitimate = false;
                        //Debug.WriteLine(" already exists  closed");
                        goto morph_up;
                    }
                    else
                    {
                        legitimate = true;
                    }
                }

                //Add kid to actions list
                if (legitimate)
                {
                    if (n.steps == 0)
                    {
                        nodes.Add(new Node(tmpState, newF, n.steps + 1, Moves.MOVE_RIGHT));
                    }
                    else
                    {
                        nodes.Add(new Node(tmpState, newF, n.steps + 1, n.move));
                    }
                }

                //--------------------------MORPH_UP
                morph_up:;
                tmpState = currentState;
                if (tmpState.Height > 190)
                {
                    goto morph_down;
                }
                legitimate = true;
                oldHeight = (int)tmpState.Height;
                //if (close)
                //{
                //    tmpState.Height += 35;
                //}
                //else
                //{
                //    tmpState.Height += 100;
                //}
                tmpState.Height = 200;
                //Debug.WriteLine("Up");
                if (tmpState.Height > 200)
                {
                    tmpState.Height = 200;
                }else if(tmpState.Height < 50)
                {
                    tmpState.Height = 50;
                }

                tmpState.Y -= (tmpState.Height - oldHeight) / 2;

                //check if it is possible to move there ,or if the agent moved to empty space(using map)
                // CHECK ROOF

                tmpState = SimulatePhysics(currentState, tmpState, Moves.MORPH_UP);
                //if (tmpState.Y == currentState.Y)
                //{
                //    goto morph_down;
                //}

                //Check if overlapping the target(using map)
                if (OverlappingTarget(tmpState) || OverlappingTarget(tmpState, currentState))
                {
                    if (n.steps == 0)
                    {
                        n.move = Moves.MORPH_UP;
                        aboutToCatch = true;
                    }
                    Debug.WriteLine("reached destination A* : " + target.ToString());
                    
                    currentAction = n.move;
                    return;
                }

                if (OverlapsMorphUp(currentState))
                {
                    goto morph_down;
                }

                //Calculate new f(n) = g(n) + h(n)
                if (n.steps == 0)
                {
                    newF = CalculateDistance(tmpState, target) + n.steps * StepsWeight;
                }
                else
                {
                    newF = CalculateDistance(tmpState, target) + n.steps * StepsWeight;
                }
                

                foreach (Node tmpNode in nodes)
                {
                    // if this state doesn't already exists in open list
                    if (CalculateDistance(tmpState, tmpNode.state) < 10 && tmpNode.f < newF)
                    {
                        legitimate = false;
                        //Debug.WriteLine(" already exists  open");
                        goto morph_down;
                    }
                    else
                    {
                        legitimate = true;
                    }
                }


                foreach (Node tmpNode in nodesClosed)
                {
                    // if this state doesn't already exists in open list
                    if (CalculateDistance(tmpState, tmpNode.state) < 10 && tmpNode.f < newF)
                    {
                        legitimate = false;
                        //Debug.WriteLine(" already exists  closed");
                        goto morph_down;
                    }
                    else
                    {
                        legitimate = true;
                    }
                }

                //Add kid to actions list
                if (legitimate)
                {
                    if (n.steps == 0)
                    {
                        nodes.Add(new Node(tmpState, newF, n.steps + 1, Moves.MORPH_UP));
                    }
                    else
                    {
                        nodes.Add(new Node(tmpState, newF, n.steps + 1, n.move));
                    }
                }
                //--------------------------MORPH_DOWN 
                morph_down:;
                tmpState = currentState;
                if (tmpState.Height < 60)
                {
                    goto after_moves;
                }
                legitimate = true;
                oldHeight = (int)tmpState.Height;
                //if (close)
                //{
                //    tmpState.Height -= 35;
                //}
                //else
                //{
                //    tmpState.Height -= 75;
                //}
                //Debug.WriteLine("Down");
                tmpState.Height = 50;
                if (tmpState.Height > 200)
                {
                    tmpState.Height = 200;
                }
                else if (tmpState.Height < 50)
                {
                    tmpState.Height = 50;
                }

                tmpState.Y += (oldHeight - tmpState.Height) / 2;

                //check if it is possible to move there ,or if the agent moved to empty space(using map)
                // CHECK ROOF
                //TBI

                //Check if overlapping the target(using map)
                if (OverlappingTarget(tmpState))
                {
                    if (n.steps == 0)
                    {
                        n.move = Moves.MORPH_DOWN;
                        aboutToCatch = true;
                    }
                    Debug.WriteLine("reached destination A* : " + target.ToString()); 
                    currentAction = n.move;
                    return;
                }

                //Calculate new f(n) = g(n) + h(n)
                newF = CalculateDistance(tmpState, target) + n.steps * StepsWeight ;


                foreach (Node tmpNode in nodes)
                {
                    // if this state doesn't already exists in open list
                    if (CalculateDistance(tmpState, tmpNode.state) < 10 && tmpNode.f < newF)
                    {
                        legitimate = false;
                        //Debug.WriteLine(" already exists  open");
                        goto after_moves;
                    }
                    else
                    {
                        legitimate = true;
                    }
                }


                foreach (Node tmpNode in nodesClosed)
                {
                    // if this state doesn't already exists in open list
                    if (CalculateDistance(tmpState, tmpNode.state) < 10 && tmpNode.f < newF)
                    {
                        legitimate = false;
                        //Debug.WriteLine(" already exists  closed");
                        goto after_moves;
                    }
                    else
                    {
                        legitimate = true;
                    }
                }

                //Add kid to actions list
                if (legitimate)
                {
                    if (n.steps == 0)
                    {
                        nodes.Add(new Node(tmpState, newF, n.steps + 1, Moves.MORPH_DOWN));
                    }
                    else
                    {
                        nodes.Add(new Node(tmpState, newF, n.steps + 1, n.move));
                    }
                }
                

                after_moves:;
                nodesClosed.Add(n);
            }

            //if cannot find a path at the given time just do a random action
            possibleMovesHorizontal = new List<Moves>();
            possibleMovesHorizontal.Add(Moves.MOVE_LEFT);
            possibleMovesHorizontal.Add(Moves.MOVE_RIGHT);

            int r = rnd.Next(possibleMovesHorizontal.Count);

            currentAction = prevMove;
            if (currentAction == Moves.NO_ACTION)
            {
                currentAction = possibleMovesHorizontal[r];
            }

            

            //send a message to the circle agent telling what action it chose
            messages.Add(new AgentMessage("Going to :" + currentAction));
        }

        //Calculate Euclidean Distance to target
        public double CalculateDistance(RectangleRepresentation r,CollectibleRepresentation target)
        {
            return Math.Sqrt(Math.Pow(r.X-target.X,2)+ Math.Pow(r.Y - target.Y, 2)) ;
            //return 10 * Math.Abs(r.Y - target.Y) + 1 * Math.Abs(r.X - target.X);
        }

        //Calculate Euclidean Distance to target
        public double CalculateDistance(RectangleRepresentation r, RectangleRepresentation target)
        {
            return Math.Sqrt(Math.Pow(r.X - target.X, 2) + Math.Pow(r.Y - target.Y, 2));
        }

        //Check if agent is overlapping any collectible
        public bool OverlappingTarget(RectangleRepresentation r)
        {
            int width = 10000 / (int)r.Height;
            int count = 0;

            for (int i = (int)(r.Y - r.Height / 2) - 40; i < (r.Y + r.Height / 2) - 40; i++)
            {
                for (int j = (int)(r.X - width / 2) - 40; j < (r.X + width / 2) - 40; j++)
                {

                    if (i < 0)
                    {
                        i = 0;
                    }
                    else if (i >= 720)
                    {
                        i = 719;
                        goto after2;
                    }

                    if (j < 0)
                    {
                        j = 0;
                    }
                    else if (j >= 1200)
                    {
                        j = 1199;
                        goto after;
                    }
                   //Debug.Write(map[i, j]);
                   if (map[i, j] == '*')
                    {
                        count++;
                    }
                    
                }
                after:;
            }
            after2:;
            //Debug.WriteLine("Overlap number: " + count);
            if(count > 5)
            {
                return true;
            }
            return false;
        }

        
        //Check if agent is overlapping any obstacle while morphing up
        public bool OverlapsMorphUp(RectangleRepresentation r)
        {
            int width = 10000 / (int)r.Height;
            int count = 0;

            for (int i = (int)(r.Y - r.Height / 2) - 40 - 100; i < (int)(r.Y - r.Height / 2) - 40; i++)
            {
                for (int j = (int)(r.X - width / 2) - 40; j < (r.X + width / 2) - 40; j++)
                {

                    if (i < 0)
                    {
                        return true;
                        i = 0;
                    }
                    else if (i >= 720)
                    {
                        i = 719;
                        goto after2;
                    }

                    if (j < 0)
                    {
                        j = 0;
                    }
                    else if (j >= 1200)
                    {
                        j = 1199;
                        goto after;
                    }
                    //Debug.Write(map[i, j]);
                    if (map[i, j] == '+')
                    {
                        count++;
                    }

                }
                after:;
            }
            after2:;
            //Debug.WriteLine("Overlap number: " + count);
            if (count > 5)
            {
                return true;
            }
            return false;
        }
        //Check if agent is overlapping any collectible when falling
        public bool OverlappingTarget(RectangleRepresentation r, RectangleRepresentation rBefore)
        {
            int width = 10000 / (int)r.Height;
            int count = 0;


            for (int i = (int)(rBefore.Y - rBefore.Height / 2) - 40; i < (r.Y + r.Height / 2) - 40; i++)
            {
                for (int j = (int)(r.X - width / 2) - 40; j < (r.X + width / 2) - 40; j++)
                {

                    if (i < 0)
                    {
                        i = 0;
                    }
                    else if (i >= 720)
                    {
                        i = 719;
                        goto after2;
                    }

                    if (j < 0)
                    {
                        j = 0;
                    }
                    else if (j >= 1200)
                    {
                        j = 1199;
                        goto after;
                    }
                    //Debug.Write(map[i, j]);
                    if (map[i, j] == '*')
                    {
                        count++;
                    }

                }
                after:;
            }
            after2:;
            //Debug.WriteLine("Overlap number: " + count);
            if (count > 5)
            {
                return true;
            }
            return false;
        }

        //Check if agent is overlapping any obstacle
        public bool OverlappingObstacle(RectangleRepresentation r)
        {
            int width = 10000 / (int)r.Height;
            int count = 0;

            for (int i = (int)(r.Y - r.Height / 2) - 40; i < (r.Y) - 40; i++)
            {
                for (int j = (int)(r.X - width / 2) - 40; j < (r.X + width / 2) - 40; j++)
                {

                    if (i < 0)
                    {
                        i = 0;
                    }
                    else if (i >= 720)
                    {
                        i = 719;
                        goto after2;
                    }

                    if (j < 0)
                    {
                        j = 0;
                    }
                    else if (j >= 1200)
                    {
                        j = 1199;
                        goto after;
                    }
                    //Debug.Write(map[i, j]);
                    if (map[i, j] == '+')
                    {
                        count++;
                    }

                }
                after:;
            }
            after2:;
            //Debug.WriteLine("Overlap number: " + count);
            if (count > 5)
            {
                return true;
            }
            return false;
        }
        //implements abstract rectangle interface: GeometryFriends agents manager gets the current action intended to be actuated in the enviroment for this agent
        public override Moves GetAction()
        {
            return currentAction;
        }

        //this function tries to simulate physics when dropping a rectangle in position r
        public RectangleRepresentation SimulatePhysics(RectangleRepresentation rBefore, RectangleRepresentation r,Moves move)
        {
            RectangleRepresentation rNew = r;
            

            int heightB = (int)rBefore.Height;
            int widthB = 10000 / (int)rBefore.Height;

            int height = (int)r.Height;
            int width = 10000 / (int)r.Height;

            int countY = 0, i, j;
            int margin = 5;
            //Debug.WriteLine("test1");
            //when moving right and there is no hole in the ground
            if(move == Moves.MOVE_RIGHT)
            {
                if (PRINT_ON)
                    Debug.WriteLine("Physics right");
                j = (int)r.Y + (int)r.Height / 2 + 20 - 40;
                //Debug.WriteLine("Limits[ " + ((int)r.X - width / 2 - margin) + " , " + ((int)r.X + width / 2 + margin) + "]");
                for (i = (int)r.X - width/2 - margin - 40; i < (int)r.X + width/2 + margin - 40; i++)
                {   
                    if(i > 1199 || i < 0)
                    {
                        break;
                    }
                    
                    if (j > 719 || j < 0)
                    {
                        break;
                    }
                    if (map[j, i] == '-')
                    {
                        countY++;
                    }
                    //else
                    //{
                    //    break;
                    //}



                }
                if (PRINT_ON)
                    Debug.WriteLine(" Right gap is: " + countY +" x,y: " + ((int)r.X - width / 2 - margin) + "," + j);
                // big hole, cannot let agent pass it
                if (PRINT_ON)
                    Debug.WriteLine(" Overlapping obstacle " + OverlappingObstacle(r).ToString());
                if ((countY > width / 2 - 15 && countY < 90)|| OverlappingObstacle(r) )
                {
                    rNew = rBefore;
                }else if (countY >= 90 && r.Height <= 60)
                {
                    //simulate falling diagonal
                    int y = (int)r.Y -40;
                    int x;
                    for( x = (int)r.X - 40; x < 1099 - 40; x++)
                    {
                        if (y > 719 || y < 0)
                        {
                            break;
                        }
                        if (map[y,x] == '+')
                        {
                            break;
                        }
                        y++;
                    }
                    r.X = x - 27 + 40;
                    r.Y = y - 27 + 40;
                    rNew = r;
                }
               
            }

            if (move == Moves.MOVE_LEFT)
            {
                if (PRINT_ON)
                    Debug.WriteLine("Physics left");
                countY = 0;
                int countYRightest = 0;
                bool flag = false;

                j = (int)r.Y + (int)r.Height / 2 + 20 - 40;
                for (i = (int)r.X - width / 2 - margin - 40; i < (int)r.X + width / 2 + margin - 40; i++)
                {
                    if (i > 1199 || i < 0)
                    {
                        break;
                    }

                    if (j > 719 || j < 0)
                    {
                        break;
                    }
                    if (map[j, i] == '-')
                    {
                        countY++;
                        flag = true;
                    }
                    else if (flag)
                    {
                        countYRightest = countY;
                        countY = 0;
                        flag = false;
                    }


                }
                //if (flag)
                //{
                //    countYRightest = countY;
                //}
                if (PRINT_ON)
                    Debug.WriteLine(" Left gap is: " + countYRightest + " x,y: " + ((int)r.X - width / 2 - margin) + "," + j);
                if (PRINT_ON)
                    Debug.WriteLine(" Overlapping obstacle " + OverlappingObstacle(r).ToString());
                // big hole, cannot let agent pass it
                if ((countYRightest > width / 2 - 15 && countYRightest < 90) || OverlappingObstacle(r))
                {
                    rNew = rBefore;
                }
                else if (countYRightest >= 90 && r.Height <= 60)
                {
                    //simulate falling diagonal
                    int y = (int)r.Y - 40;
                    int x;
                    for (x = (int)r.X - 40; x < 1099 - 40; x--)
                    {
                        if (y > 719 || y < 0)
                        {
                            break;
                        }
                        if (map[y, x] == '+')
                        {
                            break;
                        }
                        y++;
                    }
                    r.X = x - 27 + 40;
                    r.Y = y - 27 + 40;
                    rNew = r;
                }

            }

            if (move == Moves.MORPH_UP)
            {
                if (PRINT_ON)
                    Debug.WriteLine("Physics morhp up");
                countY = 0;
                int gap_start = 0;
                //may need to change 2*margin to 4x or more
                //Debug.WriteLine("Limits[ " + ((int)r.X - width / 2 + 4 * margin) + " , " + ((int)r.X + width / 2 - 4 * margin) + "]");
                j = (int)r.Y + (int)r.Height / 2 + 20 - 40;
                for (i = (int)r.X - 30 - 40; i < (int)r.X + 30 - 40; i++)
                {
                    if (i > 1199 || i < 0)
                    {
                        break;
                    }

                    if (j > 719 || j < 0)
                    {
                        break;
                    }
                    if (map[j, i] == '-')
                    {
                        if (countY == 0)
                        {
                            gap_start = i;
                        }
                        countY++;
                    }


                }
                if (PRINT_ON)
                    Debug.WriteLine(" Morph gap is: " + countY + " x,y: " + ((int)r.X - width / 2 - margin) + "," + j);
                // big hole, can slip through it
                if (countY > width / 2  - 2 )
                {
                    //change y new " simulate falling"
                    for(j = j ; j < 719; j++)
                    {
                        if (map[j,( (int)r.X - 40)] == '+')
                        {
                            break;
                        }
                    }
                    if (PRINT_ON)
                        Debug.WriteLine("New Y: " + (j - (int)r.Height / 2));

                    r.Y = (j - (int)r.Height / 2) + 40;
                    rNew = r;
                }

            }

            return rNew;
        }

        //implements abstract rectangle interface: updates the agent state logic and predictions
        public override void Update(TimeSpan elapsedGameTime)
        {
            //private double msCounter = 0;
            //Debug.WriteLine(DateTime.Now.Millisecond);
            if (lastMoveTime == 60)
                lastMoveTime = 0;

            
            
            if ((lastMoveTime) <= (DateTime.Now.Second) && (lastMoveTime < 60))
            {
                if (!(DateTime.Now.Second == 59))
                {
                    if(lastMoveTime%1 == 0 && !done)
                    {
                        //Debug.WriteLine("==+++ " + lastMoveTime);
                        if(DateTime.Now.Millisecond < TIME_PER_MOVE && doOnce)
                        {
                            doOnce = false;
                            Debug.WriteLine("||--||----||----||----||----||----||----||--||");
                            DebugSensorsInfo();
                            //PrintMap();
                            Debug.WriteLine(lastMoveTime);
                            Debug.WriteLine("================");
                            FindMoveAStar();
                            if(currentAction == Moves.MORPH_DOWN || currentAction == Moves.MORPH_UP)
                            {
                                TIME_PER_MOVE = 900;
                            }
                            else
                            {
                                prevMove = currentAction;
                                if( steps > 50 )
                                {
                                    TIME_PER_MOVE += (steps / 50) * 40;
                                }
                            }
                            if(Math.Abs(stuckPosition.X - rectangleInfo.X ) < 5 && Math.Abs(stuckPosition.Y - rectangleInfo.Y) < 5)
                            {
                                Debug.WriteLine("Stuck counter: " + stuckCounter);
                                stuckCounter++;
                                if (stuckCounter > 1)
                                {
                                    int r = rnd.Next(possibleMoves.Count);
                                    currentAction = possibleMoves[r];
                                }
                            }
                            else
                            {
                                stuckCounter = 0;

                            }
                            stuckPosition = rectangleInfo;
                            Debug.WriteLine("================");
                            DebugSensorsInfo();
                            Debug.WriteLine("----------------");
                            //CalculateMap();
                            //PrintMap();
                        }
                        else if (DateTime.Now.Millisecond > TIME_PER_MOVE)
                        {
                            Debug.WriteLine("+----++----++----++----++----+");
                            doOnce = true;
                            done = true;

                            currentAction = Moves.NO_ACTION;
                            //lastMoveTime = lastMoveTime + 1;
                        }

                    }
                    else
                    {
                        //Debug.WriteLine(lastMoveTime + " STAYYYYYYY");
                        if (lastMoveTime != DateTime.Now.Second)
                        {
                            Debug.WriteLine("#################");
                            done = false;
                            doOnce = true;
                            lastMoveTime +=1;
                        }
                        
                        currentAction = Moves.NO_ACTION;
                    }
                    
                }
                else
                {
                    //DebugSensorsInfo();
                    done = false;
                    doOnce = true;
                    lastMoveTime = 60;
                }
                    
            }
        }

        //typically used console debugging used in previous implementations of GeometryFriends
        protected void DebugSensorsInfo()
        {
            Debug.WriteLine(numbersInfo.ToString());
            Debug.WriteLine(nCollectiblesLeft);

            Debug.WriteLine(rectangleInfo.ToString());

            Debug.WriteLine("Current move: " + currentAction.ToString());

            foreach (ObstacleRepresentation i in obstaclesInfo)
            {
                Debug.WriteLine(i.ToString("Obstacle"));
            }

            foreach (ObstacleRepresentation i in rectanglePlatformsInfo)
            {
                Debug.WriteLine(i.ToString("Rectangle Platform"));
            }

            foreach (ObstacleRepresentation i in circlePlatformsInfo)
            {
                Debug.WriteLine(i.ToString("Circle Platform"));
            }

            foreach (CollectibleRepresentation i in collectiblesInfo)
            {
                Debug.WriteLine(i.ToString());
            }
        }

        //implements abstract rectangle interface: signals the agent the end of the current level
        public override void EndGame(int collectiblesCaught, int timeElapsed)
        {
            Debug.WriteLine("RECTANGLE - Collectibles caught = " + collectiblesCaught + ", Time elapsed - " + timeElapsed);
        }

        //implememts abstract agent interface: send messages to the circle agent
        public override List<GeometryFriends.AI.Communication.AgentMessage> GetAgentMessages()
        {
            List<AgentMessage> toSent = new List<AgentMessage>(messages);
            messages.Clear();
            return toSent;
        }

        //implememts abstract agent interface: receives messages from the circle agent
        public override void HandleAgentMessages(List<GeometryFriends.AI.Communication.AgentMessage> newMessages)
        {
            foreach (AgentMessage item in newMessages)
            {
                Debug.WriteLine("Rectangle: received message from circle: " + item.Message);
                if (item.Attachment != null)
                {
                    Debug.WriteLine("Received message has attachment: " + item.Attachment.ToString());
                    if (item.Attachment.GetType() == typeof(Pen))
                    {
                        Debug.WriteLine("The attachment is a pen, let's see its color: " + ((Pen)item.Attachment).Color.ToString());
                    }
                }
            }
        }
    }
}