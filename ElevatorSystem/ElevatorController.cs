namespace ElevatorSystem
{
    public class ElevatorController
    {

        private const int ElevatorsCount = 5;
        private const int FloorsCount = 20;
        public Elevator[] elevators =new Elevator[ElevatorsCount];

        public int[,] upRequest = new int[ElevatorsCount, FloorsCount];
        public int[,] downRequest = new int[ElevatorsCount, FloorsCount];
        //wait数组里储存的是电梯需要经过状态变换才能处理的请求
        public int[,] waitUpRequest = new int[ElevatorsCount,FloorsCount];
        public int[,] waitDownRequest = new int[ElevatorsCount, FloorsCount];

        public event Action<int, int> ElevatorMoved;

        public event Action<int, int> ElevatorRequestProcessed;

        public event Action<int, int> ElevatorExternalRequestProcessed;

        public event Action<int> OpenDoorPressed;
        public event Action<int> CloseDoorPressed;
        

        public ElevatorController()
        {
            for(int i=0;i<elevators.Length; i++) 
            {
                elevators[i] = new Elevator();
            }

            for(int i=0;i < ElevatorsCount;i++)
            {
                for(int j=0;j < FloorsCount;j++)
                {
                    upRequest[i, j] = 0;
                    downRequest[i, j] = 0;
                    waitUpRequest[i, j] = 0;
                    waitDownRequest[i, j] = 0;
                }
            }
            StartElevatorThreads();
        }

        public void TriggerOpenDoor(int elevatorIndex)
        {
            Console.WriteLine($"Open door button pressed for elevator {elevatorIndex + 1}");
            OpenDoorPressed?.Invoke(elevatorIndex);
        }

        public void TriggerCloseDoor(int elevatorIndex)
        {
            Console.WriteLine($"Close door button pressed for elevator {elevatorIndex + 1}");
            CloseDoorPressed?.Invoke(elevatorIndex);
        }


        private void StartElevatorThreads()
        {
            for(int i=0;i<ElevatorsCount;i++)
            {
                int index = i;
                Thread elevatorThread = new Thread(() => ElevatorRoutine(index))
                {
                    IsBackground = true
                };
                elevatorThread.Start();
            }
        }

        private void ElevatorRoutine(int elevatorIndex) 
        {
            while (true)
            {

                Thread.Sleep(500);
                pullUpward(elevatorIndex);
                Thread.Sleep(500);
                prepareForDown(elevatorIndex);
                Thread.Sleep(500);
                pushDownward(elevatorIndex);
                Thread.Sleep(500);
                prepareForUp(elevatorIndex);
            }
        }

        public int handleUpRequest(int floor)
        {
            int minDist = FloorsCount + 1;
            int elevatorIndex = ElevatorsCount;

            for(int i=0;i<ElevatorsCount;i++)
            {
                int currentFloor = elevators[i].getCurrentFloor();
                if (elevators[i].getStatus()==status.alarm)
                {
                    continue;
                }
                if (elevators[i].getStatus()==status.wait)
                {
                    if(Math.Abs(currentFloor-floor) < minDist)
                    {
                        minDist=Math.Abs(currentFloor-floor);
                        elevatorIndex=i;
                    }
                }
                else if (elevators[i].getStatus()==status.up)
                {
                    if(currentFloor <= floor)
                    {
                        if(Math.Abs(currentFloor-floor)<minDist)
                        { 
                            minDist = Math.Abs(currentFloor-floor);
                            elevatorIndex = i; 
                        }
                    }
                    else if(currentFloor > floor)
                    {
                        int tmp = 0;
                        //找到目前向上请求中最高的楼层
                        for(int j=FloorsCount-1;j>=0;j--)
                        {
                            if (upRequest[i, j] == 1)
                            {
                                tmp = j;
                                break;
                            }
                        }
                        //计算距离
                        int dist = Math.Abs(tmp - currentFloor) + Math.Abs(tmp - floor);
                        if(dist<minDist)
                        {
                            minDist=dist;
                            elevatorIndex = i;
                        }
                    }

                }
                else if (elevators[i].getStatus()==status.down)
                {
                    int tmp = 0;
                    //找到目前向下请求中最小的层
                    for(int j=0;j<FloorsCount;j++)
                    {
                        if (downRequest[i, j] == 1)
                        {
                            tmp = j;
                            break;
                        }
                    }
                    int dist=Math.Abs(tmp - currentFloor) + Math.Abs(tmp - floor);
                    if(dist<minDist) 
                    {
                        minDist=dist;
                        elevatorIndex = i;
                    }

                }
            }
            int currenFloor = elevators[elevatorIndex].getCurrentFloor();
            if (elevators[elevatorIndex].getStatus()==status.wait) 
            {
                
                if(floor >= currenFloor)
                    upRequest[elevatorIndex, floor] = 1;
                else
                    waitUpRequest[elevatorIndex, floor] = 1;
                
                //upRequest[elevatorIndex, floor] = 1;
            }
            else if (elevators[elevatorIndex].getStatus()==status.up)
                
            {
                if (floor >= currenFloor)
                    upRequest[elevatorIndex, floor] = 1;
                else
                    waitUpRequest[elevatorIndex, floor] = 1;
            }
            else
                waitUpRequest[elevatorIndex,floor] = 1;

            return elevatorIndex;
        }

        public int handleDownRequest(int floor) 
        {
            int minDist = 21;
            int elevatorIndex = 5;

            for (int i = 0; i < ElevatorsCount; i++)
            {
                int currentFloor = elevators[i].getCurrentFloor();
                if (elevators[i].getStatus() == status.alarm)
                {
                    continue;
                }
                if (elevators[i].getStatus() == status.wait)
                {
                    if (Math.Abs(currentFloor - floor) < minDist)
                    {
                        minDist = Math.Abs(currentFloor - floor);
                        elevatorIndex = i;
                    }
                }
                else if (elevators[i].getStatus() == status.down)
                {
                    if (currentFloor >= floor)
                    {
                        if (Math.Abs(currentFloor - floor) < minDist)
                        {
                            minDist = Math.Abs(currentFloor - floor);
                            elevatorIndex = i;
                        }
                    }
                    else if (currentFloor < floor)
                    {
                        int tmp = 0;
                        //找到目前向上请求中最小的楼层
                        for (int j = 0; j <FloorsCount; j++)
                        {
                            if (upRequest[i, j] == 1)
                            {
                                tmp = j;
                                break;
                            }
                        }
                        //计算距离
                        int dist = Math.Abs(tmp - currentFloor) + Math.Abs(tmp - floor);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            elevatorIndex = i;
                        }
                    }

                }
                else if (elevators[i].getStatus() == status.up)
                {
                    int tmp = 0;
                    //找到目前向下请求中最高的层
                    for (int j = FloorsCount-1; j >= 0; j--)
                    {
                        if (downRequest[i, j] == 1)
                        {
                            tmp = j;
                            break;
                        }
                    }
                    int dist = Math.Abs(tmp - currentFloor) + Math.Abs(tmp - floor);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        elevatorIndex = i;
                    }

                }
            }
            int currenFloor = elevators[elevatorIndex].getCurrentFloor();
            if (elevators[elevatorIndex].getStatus() == status.wait)
            {
                
                if (floor <= currenFloor)
                    downRequest[elevatorIndex, floor] = 1;
                else
                    waitDownRequest[elevatorIndex, floor] = 1;
                
                //downRequest[elevatorIndex, floor] = 1;
            }
            else if (elevators[elevatorIndex].getStatus() == status.down)
            {
                if (floor <= currenFloor)
                    downRequest[elevatorIndex, floor] = 1;
                else
                    waitDownRequest[elevatorIndex, floor] = 1;
            }
            else
                waitDownRequest[elevatorIndex, floor] = 1;

            return elevatorIndex;
        }

        private int getSum(int[,]request,int elevatorIndex)
        {
            int sum = 0;
            for(int i = 0;i<FloorsCount;i++)
            {
                sum += request[elevatorIndex, i];
            }
            return sum;
        }

        void pullUpward(int elevatorIndex)
        {
            status currentStatus = elevators[elevatorIndex].getStatus();
            if (currentStatus == status.down || currentStatus == status.alarm)
            {
                return;
            }
            elevators[elevatorIndex].setStatus(status.up);

            while(getSum(upRequest,elevatorIndex)>0)
            {
                for (int i = 0; i < FloorsCount; i++)
                {
                    if (upRequest[elevatorIndex, i] == 1)
                    {

                        elevators[elevatorIndex].setTargetFloor(i);
                        while (elevators[elevatorIndex].getCurrentFloor() != elevators[elevatorIndex].getTargetFloor())
                        {
                            while (elevators[elevatorIndex].getStatus() == status.alarm) ;
                            elevators[elevatorIndex].move();
                            if (upRequest[elevatorIndex, elevators[elevatorIndex].getCurrentFloor()] == 1)
                            {
                                upRequest[elevatorIndex, elevators[elevatorIndex].getCurrentFloor()] = 0;
                                ElevatorRequestProcessed.Invoke(elevatorIndex, elevators[elevatorIndex].getCurrentFloor());
                                ElevatorExternalRequestProcessed.Invoke(elevators[elevatorIndex].getCurrentFloor(), 1);
                            }
                            ElevatorMoved.Invoke(elevatorIndex, elevators[elevatorIndex].getCurrentFloor());
                        }
                        if (elevators[elevatorIndex].getCurrentFloor() == i)
                        {
                            upRequest[elevatorIndex, i] = 0;
                            ElevatorRequestProcessed.Invoke(elevatorIndex, i);
                            ElevatorExternalRequestProcessed.Invoke(i, 1);
                        }
                    }
                }
            }
            elevators[elevatorIndex].setStatus(status.wait);
        }
       
        void pushDownward(int elevatorIndex)
        {
            status currentStatus = elevators[elevatorIndex].getStatus();
            if (currentStatus == status.up||currentStatus==status.alarm)
            {
                return;
            }
            elevators[elevatorIndex].setStatus(status.down);
            while(getSum(downRequest,elevatorIndex)>0)
            {
                for (int i = FloorsCount - 1; i >= 0; i--)
                {
                    if (downRequest[elevatorIndex, i] == 1)
                    {

                        elevators[elevatorIndex].setTargetFloor(i);
                        while (elevators[elevatorIndex].getCurrentFloor() != elevators[elevatorIndex].getTargetFloor())
                        {
                            while (elevators[elevatorIndex].getStatus() == status.alarm) ;
                            elevators[elevatorIndex].move();
                            if (downRequest[elevatorIndex, elevators[elevatorIndex].getCurrentFloor()] == 1)
                            {
                                downRequest[elevatorIndex, elevators[elevatorIndex].getCurrentFloor()] = 0;
                                ElevatorRequestProcessed.Invoke(elevatorIndex, elevators[elevatorIndex].getCurrentFloor());
                                ElevatorExternalRequestProcessed.Invoke(elevators[elevatorIndex].getCurrentFloor(), -1);
                            }
                            ElevatorMoved.Invoke(elevatorIndex, elevators[elevatorIndex].getCurrentFloor());
                        }
                        if (elevators[elevatorIndex].getCurrentFloor() == i)
                        {
                            downRequest[elevatorIndex, i] = 0;
                            ElevatorRequestProcessed.Invoke(elevatorIndex, i);
                            ElevatorExternalRequestProcessed.Invoke(i, -1);
                        }
                    }
                }
            }
            elevators[elevatorIndex].setStatus(status.wait) ;
        }

        void prepareForUp(int elevatorIndex)
        { 
            for(int i=0;i<FloorsCount;i++)
            {
                if (waitUpRequest[elevatorIndex,i] == 1)
                {
                    if (i < elevators[elevatorIndex].getCurrentFloor())
                    {
                        downRequest[elevatorIndex, i] = 1;
                        elevators[elevatorIndex].setStatus(status.down);
                        break;
                    }
                    waitUpRequest[elevatorIndex,i] = 0;
                    upRequest[elevatorIndex, i] = 1;
                    elevators[elevatorIndex].setStatus(status.up);
                }
            }
        }

        void prepareForDown(int elevatorIndex)
        {
            for(int i=FloorsCount-1;i>=0;i--)
            {
                if (waitDownRequest[elevatorIndex,i] == 1)
                {
                    if (i > elevators[elevatorIndex].getCurrentFloor())
                    {
                        upRequest[elevatorIndex, i] = 1;
                        elevators[elevatorIndex].setStatus(status.up);
                        break;
                    }
                    waitDownRequest[elevatorIndex, i] = 0;
                    downRequest[elevatorIndex, i] = 1;
                    elevators[elevatorIndex ].setStatus(status.down);
                }
            }
        }
    }

}
