using System;
using System.Threading;

/// <summary>
/// Summary description for Class1
/// </summary>
namespace ElevatorSystem
{
    public enum status
    {
        wait, up, down, alarm
    };

    public class Elevator
	{

		status currentStatus;

		int currentFloor;
		int targetFloor;
		int capacity;


		public Elevator(int current=0,int target=0)
		{ 
			currentFloor = current;
			targetFloor = target;
			capacity = 5;
			currentStatus = status.wait;
		}


		public int getCurrentFloor() { return currentFloor; }
		public int getTargetFloor() { return targetFloor;}
		public int getCapacity() { return capacity;}

		public void takePassenger() { capacity--; }

		public void ariveTargetFloor() { capacity++; }

		public void setTargetFloor(int target) { targetFloor = target; }
		public void setStatus(status sta) {  currentStatus = sta; }

		public status getStatus() { return currentStatus; }

		public void up()
		{ 
			currentStatus = status.up;
			if (currentFloor < 20)
			{
				currentFloor++;
				Thread.Sleep(500);
			}
		}
		public void down()
		{
			currentStatus = status.down;
			if (currentFloor > 0)
			{
				currentFloor--;
				Thread.Sleep(500);
			}
		}

		public void move()
		{
			if (currentFloor < targetFloor)
			{
				up();
			}
			if (currentFloor > targetFloor)
			{
				down();
			}
		}

		public bool check()
		{
			if (capacity < 0)
			{
				currentStatus = status.alarm;
				return false;

			}
			return true;
		}
	}
}
