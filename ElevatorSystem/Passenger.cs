using System;

/// <summary>
/// Summary description for Class1
/// </summary>
namespace ElevatorSystem
{
    public class Passenger
    {
        public int id { get; set; }
        public int currentFloor { get; set; }
        public int targetFloor { get; set; }
        public int assignedElevator { get; set; } // 假设电梯分配后存储电梯的编号

        public bool isHandled { get; set; } = false; // 默认为false，表示未处理

        public Passenger(int ID, int current, int target, int assigned)
        {
            id = ID;
            currentFloor = current;
            targetFloor = target;
            assignedElevator = assigned;
        }

    }

}