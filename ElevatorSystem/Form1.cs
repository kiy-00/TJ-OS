using System.Buffers.Text;
using System.Timers;

namespace ElevatorSystem
{
    public partial class Form1 : Form
    {
        private const int ElevatorsCount = 5;
        private const int FloorsCount = 20;
        private GroupBox[] elevatorGroups = new GroupBox[ElevatorsCount];
        private PictureBox[] elevatorPictureBoxes=new PictureBox[ElevatorsCount];
        private Label[] floorDisplayLabels = new Label[ElevatorsCount];
        private Label[] elevatorStatusLabels = new Label[ElevatorsCount]; // 存储状态标签的数组
        private Label[] elevatorCapacityLabels = new Label[ElevatorsCount];
        private Button[] openDoorButtons = new Button[ElevatorsCount];
        private Button[] closeDoorButtons = new Button[ElevatorsCount];
        private TextBox messageBox;
        private GroupBox passengersGroup;
        private ComboBox currentFloorComboBox;
        private ComboBox targetFloorComboBox;
        private DataGridView passengersDataGridView;
        private int nextPassengerId = 0;
        private System.Timers.Timer checkElevatorTimer;
        private CancellationTokenSource cancellationTokenSource;

        private Dictionary<int, TaskCompletionSource<bool>> openDoorTcs = new Dictionary<int, TaskCompletionSource<bool>>();
        private Dictionary<int, TaskCompletionSource<bool>> closeDoorTcs = new Dictionary<int, TaskCompletionSource<bool>>();

        private System.Windows.Forms.Timer animationTimer = new System.Windows.Forms.Timer();
        private List<Image> openDoorImages = new List<Image>();
        private List<Image> closeDoorImages = new List<Image>();
        private int currentAnimationFrame = 0;

        ElevatorController manage;
        private List<Passenger> passengers = new List<Passenger>();
        private readonly object passengersLock = new object();

        private System.Windows.Forms.Timer updateTimer;

        public Form1()
        {
            InitializeComponent();
            InitializeElevatorUI();
            InitialMassageBox();
            InitializeFloorRequestButtons();
            InitializePassengerManagementUI();
            manage = new ElevatorController();
            manage.ElevatorMoved += Manage_ElevatorMoved;
            manage.ElevatorRequestProcessed += Manage_ElevatorRequestProcessed;
            manage.ElevatorExternalRequestProcessed += Manage_ExternalRequestProcessed;
            manage.OpenDoorPressed += Manage_OpenDoorPressed;
            manage.CloseDoorPressed += Manage_CloseDoorPressed;

            LoadImages();
            InitializeTimer();
        }

        private Image ResizeImage(Image image, float scale)
        {
            int newWidth = (int)(image.Width * scale);
            int newHeight = (int)(image.Height * scale);
            Bitmap newImage = new Bitmap(newWidth, newHeight);
            using (Graphics gfx = Graphics.FromImage(newImage))
            {
                gfx.DrawImage(image, new Rectangle(0, 0, newWidth, newHeight));
            }
            return newImage;
        }


        private void LoadImages()
        {
            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res");
            float scale = 0.6f; // 设置缩放比例为80%

            for (int i = 1; i <= 4; i++)
            {
                Image openImage = Image.FromFile(Path.Combine(baseDir, "开门动画", $"{i}.png"));
                openDoorImages.Add(ResizeImage(openImage, scale));
                openImage.Dispose(); // 释放原始图像资源

                Image closeImage = Image.FromFile(Path.Combine(baseDir, "关门动画", $"{i}.png"));
                closeDoorImages.Add(ResizeImage(closeImage, scale));
                closeImage.Dispose(); // 释放原始图像资源
            }
        }


        private void InitializeTimer()
        {
            animationTimer.Interval = 200; // 设置动画帧切换的时间间隔为200毫秒
            animationTimer.Tick += new EventHandler(AnimationTimer_Tick);
            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 1000;  // 更新间隔设置为1000毫秒（1秒）
            updateTimer.Tick += new EventHandler(UpdateTimer_Tick);
            updateTimer.Start();
            // 初始化定时器
            checkElevatorTimer = new System.Timers.Timer(1000); // 设置时间间隔为1秒
            checkElevatorTimer.Elapsed += CheckElevatorStatus;
            checkElevatorTimer.AutoReset = true;
            checkElevatorTimer.Enabled = true; // 初始状态为禁用
        }

        private void InitialMassageBox()
        {
            // 初始化消息框
            messageBox = new TextBox
            {
                Multiline = true, // 允许多行文本
                ReadOnly = true,  // 设置为只读
                ScrollBars = ScrollBars.Vertical, // 垂直滚动条
                Location = new System.Drawing.Point(10, this.ClientSize.Height - 200), // 设定位置
                Size = new System.Drawing.Size(this.ClientSize.Width - 20, 150), // 设定大小
                BackColor = System.Drawing.Color.White, // 背景颜色
                Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Regular), // 字体设置
            };
            this.Controls.Add(messageBox); // 添加到窗体的控件集
        }

        private void InitializeElevatorUI()
        {
            for (int elevatorIndex = 0; elevatorIndex < ElevatorsCount; elevatorIndex++)
            {
                // 创建每部电梯的GroupBox
                GroupBox groupBox = new GroupBox
                {
                    Text = $"Elevator {elevatorIndex + 1}",
                    Size = new System.Drawing.Size(150, FloorsCount * 40 + 80),
                    Location = new System.Drawing.Point(elevatorIndex * 225 + 10, 75)
                };
                this.Controls.Add(groupBox);
                elevatorGroups[elevatorIndex] = groupBox;

                //创建电梯图标
                string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res", $"Elevator.png");
                PictureBox elevatorPictureBox = new PictureBox
                {
                    Size = new System.Drawing.Size(45, 45),
                    Location = new System.Drawing.Point(50, 20 + (FloorsCount - 1) * 40),
                    Image=Image.FromFile(imagePath),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                };
                groupBox.Controls.Add(elevatorPictureBox);
                elevatorPictureBoxes[elevatorIndex] = elevatorPictureBox;

                Label floorDisplay = new Label
                {
                    Text = "1", // 假设初始楼层为1
                    AutoSize = true,
                    Location = new System.Drawing.Point(elevatorIndex * 225 + 20, 30),
                    Font = new Font("Arial", 16, FontStyle.Bold),
                    ForeColor = Color.Black,
                    BackColor = Color.Transparent
                };
                this.Controls.Add(floorDisplay);
                floorDisplayLabels[elevatorIndex] = floorDisplay; // 存储Label引用

                // 创建电梯状态显示Label
                Label statusLabel = new Label
                {
                    Text = "wait", // 初始状态为等待
                    AutoSize = true,
                    Location = new System.Drawing.Point(10, FloorsCount * 40 + 60),
                    Font = new Font("Arial", 8, FontStyle.Regular),
                    ForeColor = Color.Black,
                    BackColor = Color.Transparent
                };
                groupBox.Controls.Add(statusLabel);
                elevatorStatusLabels[elevatorIndex] = statusLabel; // 存储状态Label

                // 创建剩余容量显示Label
                Label capacityLabel = new Label
                {
                    Text = "Capacity: 5", // 假设初始容量为10，您可以根据实际情况调整
                    AutoSize = true,
                    Location = new System.Drawing.Point(elevatorIndex * 225 + 70, 40),
                    Font = new Font("Arial", 8, FontStyle.Bold),
                    ForeColor = Color.Black,
                    BackColor = Color.Transparent
                };
                this.Controls.Add(capacityLabel);
                elevatorCapacityLabels[elevatorIndex] = capacityLabel; // 存储容量Label


                // 创建楼层按钮
                for (int floor = FloorsCount - 1; floor >= 0; floor--)
                {
                    Button floorButton = new Button
                    {
                        Text = (floor + 1).ToString(),
                        Size = new System.Drawing.Size(40, 30),
                        Location = new System.Drawing.Point(10, (FloorsCount - 1 - floor) * 40 + 20)
                    };
                    int tempElevatorIndex = elevatorIndex;
                    int tempFloor = floor;
                    floorButton.Click += (sender, e) => FloorButton_Click(sender, e, tempElevatorIndex, tempFloor);
                    groupBox.Controls.Add(floorButton);
                }

                // 创建报警按钮
                Button alarmButton = new Button
                {
                    Text = "Alarm",
                    Size = new System.Drawing.Size(80, 30),
                    Location = new System.Drawing.Point(20, FloorsCount * 40 + 20)
                };
                int temp = elevatorIndex;
                alarmButton.Click += (sender, e) => AlarmButton_Click(sender, e, temp);
                groupBox.Controls.Add(alarmButton);

                // 创建开门按钮
                Button openButton = new Button
                {
                    Text = "Open",
                    Size = new System.Drawing.Size(70, 30),
                    Location = new System.Drawing.Point(elevatorIndex * 225 + 10, groupBox.Bottom + 5)
               
                };
                openButton.Click += (sender, e) => manage.TriggerOpenDoor(temp); // 添加事件处理器
                this.Controls.Add(openButton);
                openDoorButtons[elevatorIndex] = openButton;

                // 创建关门按钮
                Button closeButton = new Button
                {
                    Text = "Close",
                    Size = new System.Drawing.Size(70, 30),
                    Location = new System.Drawing.Point(elevatorIndex * 225 + 85, groupBox.Bottom + 5)
                };
                closeButton.Click += (sender, e) => manage.TriggerCloseDoor(temp); // 添加事件处理器
                this.Controls.Add(closeButton);
                closeDoorButtons[elevatorIndex] = closeButton;
            }
        }

        private void InitializeFloorRequestButtons()
        {
            // 添加楼层请求按钮的容器
            GroupBox requestButtonsGroup = new GroupBox
            {
                Text = "Floor Requests",
                Size = new System.Drawing.Size(200, FloorsCount * 40 + 80),
                Location = new System.Drawing.Point(ElevatorsCount * 225 + 10, 75)
            };
            this.Controls.Add(requestButtonsGroup);

            for (int floor = 0; floor < FloorsCount; floor++)
            {
                // 向上请求按钮
                if (floor < FloorsCount - 1) // 除了顶层
                {
                    Button upRequestButton = new Button
                    {
                        Text = "↑",
                        Size = new System.Drawing.Size(40, 30),
                        Location = new System.Drawing.Point(10, (FloorsCount - 1 - floor) * 40 + 20)
                    };
                    int temp = floor;
                    upRequestButton.Click += (sender, e) => UpRequestButton_Click(sender, e, temp);
                    requestButtonsGroup.Controls.Add(upRequestButton);
                }

                // 向下请求按钮
                if (floor > 0) // 除了首层
                {
                    Button downRequestButton = new Button
                    {
                        Text = "↓",
                        Size = new System.Drawing.Size(40, 30),
                        Location = new System.Drawing.Point(60, (FloorsCount - 1 - floor) * 40 + 20)
                    };
                    int temp = floor;
                    downRequestButton.Click += (sender, e) => DownRequestButton_Click(sender, e, temp);
                    requestButtonsGroup.Controls.Add(downRequestButton);
                }
            }
        }

        private void InitializePassengerManagementUI()
        {
            // 创建乘客管理的GroupBox
            passengersGroup = new GroupBox
            {
                Text = "Passenger Management",
                Size = new System.Drawing.Size(400, FloorsCount * 40 + 80), // 根据需要调整大小
                Location = new System.Drawing.Point(ElevatorsCount * 225 + 220, 75) // 根据电梯数量和其他UI元素的位置调整
            };
            this.Controls.Add(passengersGroup);

            // 添加乘客按钮
            Button addPassengerButton = new Button
            {
                Text = "Add Passenger",
                Size = new System.Drawing.Size(180, 30),
                Location = new System.Drawing.Point(10, 20)
            };
            passengersGroup.Controls.Add(addPassengerButton);
            addPassengerButton.Click += AddPassengerButton_Click; // 需要定义此事件处理程序

            // 创建当前楼层标签和下拉列表
            Label currentFloorLabel = new Label
            {
                Text = "Current Floor:",
                AutoSize = true,
                Location = new System.Drawing.Point(10,50) // 调整位置以适应界面布局
            };
            currentFloorComboBox = new ComboBox
            {
                Size = new System.Drawing.Size(180, 20),
                DropDownStyle = ComboBoxStyle.DropDownList, // 设置为只读下拉列表
                Location = new System.Drawing.Point(10, 80) // 下拉列表位于标签正下方
            };

            // 创建目标楼层标签和下拉列表
            Label targetFloorLabel = new Label
            {
                Text = "Target Floor",
                AutoSize = true,
                Location = new System.Drawing.Point(10, 110) // 保持与currentFloorComboBox的对齐
            };
            targetFloorComboBox = new ComboBox
            {
                Size = new System.Drawing.Size(180, 20),
                DropDownStyle = ComboBoxStyle.DropDownList, // 设置为只读下拉列表
                Location = new System.Drawing.Point(10, 140) // 下拉列表位于标签正下方
            };

            // 填充下拉列表项
            for (int i = 1; i <= FloorsCount; i++)
            {
                currentFloorComboBox.Items.Add(i);
                targetFloorComboBox.Items.Add(i);
            }

            passengersGroup.Controls.Add(currentFloorLabel);
            passengersGroup.Controls.Add(currentFloorComboBox);
            passengersGroup.Controls.Add(targetFloorLabel);
            passengersGroup.Controls.Add(targetFloorComboBox);

            passengersDataGridView = new DataGridView
            {
                Location = new System.Drawing.Point(10, 200), // 调整以适应布局
                Size = new System.Drawing.Size(380, 700), // 根据需要调整大小
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false, // 禁止用户直接添加行
                AllowUserToOrderColumns = false,
                AllowUserToResizeColumns = false, 
                AllowUserToResizeRows = false,
                ReadOnly = true, // 设置为只读
                RowHeadersVisible = false, // 不显示行头
                AutoGenerateColumns = false
            };

         
            passengersDataGridView.Columns.Add("Id", "ID");
            passengersDataGridView.Columns.Add("CurrentFloor", "Current");
            passengersDataGridView.Columns.Add("TargetFloor", "Target");
            passengersDataGridView.Columns.Add("AssignedElevator", "Assigned");

            passengersGroup.Controls.Add(passengersDataGridView);

        }

        // 定义添加乘客按钮的事件处理程序
        private void AddPassengerButton_Click(object sender, EventArgs e)
        {
            // 此处添加处理逻辑，例如显示一个新窗口来输入乘客信息或直接在当前界面添加

            int currentFloor=(int)currentFloorComboBox.SelectedIndex;
            int targetFloor=(int)targetFloorComboBox.SelectedIndex;
            int assignedElevator = -1;

            // 检查是否有楼层未被选择
            if (currentFloor == -1 || targetFloor == -1)
            {
                MessageBox.Show("Please select both current and target floors.", "Selection Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return; // 退出方法，不继续执行添加乘客的逻辑
            }

            if (currentFloor < targetFloor)
            {
                assignedElevator = manage.handleUpRequest(currentFloor);
                Button requestButton = FindExternalButton(currentFloor, 1);
                if(requestButton!=null)
                {
                    requestButton.BackColor = Color.Blue;
                }
            }
            else
            {
                assignedElevator = manage.handleDownRequest(currentFloor);
                Button requestButton = FindExternalButton(currentFloor, -1);
                if (requestButton != null)
                {
                    requestButton.BackColor = Color.Blue;
                }
            }

            var newPassenger = new Passenger(nextPassengerId++, currentFloor, targetFloor, assignedElevator);

            manage.elevators[assignedElevator].takePassenger();
            if (manage.elevators[assignedElevator].check() == false)
            {
                this.Invoke((MethodInvoker)delegate // 确保在UI线程上执行
                {
                    MessageBox.Show($"电梯 {assignedElevator + 1} 内的警报触发,电梯超载！");
                    TriggerAlarmState(assignedElevator, true);
                    var timer = new System.Windows.Forms.Timer { Interval = 5000 }; // 5秒后解除报警
                    timer.Tick += (sender, args) =>
                    {
                        this.Invoke((MethodInvoker)delegate // 再次确保在UI线程上执行
                        {
                            TriggerAlarmState(assignedElevator, false);
                        });
                        timer.Stop();
                        timer.Dispose(); // 停止计时器并释放资源
                    };
                    timer.Start();
                });
                manage.elevators[assignedElevator].ariveTargetFloor(); // 假设这是到达目标楼层的方法
            }
            else
            {
                this.Invoke((MethodInvoker)delegate // 确保在UI线程上执行
                {
                    // 添加新乘客
                    passengers.Add(newPassenger);
                    // 更新DataGridView
                    passengersDataGridView.Rows.Add(newPassenger.id + 1, currentFloor + 1, targetFloor + 1, assignedElevator + 1);
                    // 更新消息框显示分配信息
                    string message = $"Passenger {newPassenger.id + 1} has been assigned to Elevator {assignedElevator + 1}.";
                    messageBox.AppendText(message + Environment.NewLine); // 添加新消息到消息框，并换行
                });
            }
        }



        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            Dictionary<int, int> floorUpdates = new Dictionary<int, int>();

            // 在锁内部创建一个需要更新的乘客列表快照
            lock (passengersLock)
            {
                var passengersToUpdate = passengers.Where(p => p.isHandled).ToList();
                Console.WriteLine(passengersToUpdate.Count.ToString());

                foreach (var passenger in passengersToUpdate)
                {
                    int currentFloor = manage.elevators[passenger.assignedElevator].getCurrentFloor();
                    floorUpdates[passenger.id] = currentFloor;
                }
            }

            // 无需使用Invoke，因为Timer Tick事件默认在UI线程上执行
            foreach (var update in floorUpdates)
            {
                UpdatePassengerCurrentFloor(update.Key, update.Value);
            }
            CheckAndRemoveArrivedPassengers();
        }


        private void CheckAndRemoveArrivedPassengers()
        {
            // 注意：以下代码段需要在UI线程执行，因为它操作了UI组件
            List<Passenger> passengersToRemove = new List<Passenger>();

            foreach (var passenger in passengers)
            {
                // 检查分配的电梯是否已到达乘客的目标楼层
                if (passenger.currentFloor==passenger.targetFloor)
                {
                    manage.elevators[passenger.assignedElevator].ariveTargetFloor();
                    // 移除对应的DataGridView行
                    foreach (DataGridViewRow row in passengersDataGridView.Rows)
                    {
                        if ((int)row.Cells["Id"].Value - 1 == passenger.id)
                        {
                            passengersDataGridView.Invoke((MethodInvoker)delegate
                            {
                                passengersDataGridView.Rows.RemoveAt(row.Index);
                            });
                            break; // 找到后退出循环
                        }
                    }
                    // 标记乘客以便从列表中删除
                    passengersToRemove.Add(passenger);
                }
            }

            // 在乘客列表中删除已到达的乘客
            foreach (var passenger in passengersToRemove)
            {
                passengers.Remove(passenger);
            }
        }


        private async void CheckElevatorStatus(Object source, System.Timers.ElapsedEventArgs e)
        {
            // 在Task.Run内执行需要在UI线程上完成的操作
            await Task.Run(() =>
            {
                Invoke((MethodInvoker)delegate
                {
                    foreach (var passenger in passengers)
                    {
                        if (!passenger.isHandled && manage.elevators[passenger.assignedElevator].getCurrentFloor() == passenger.currentFloor)
                        {
                            passenger.isHandled = true;
                            messageBox.AppendText($"Please press the open door button for Elevator {passenger.assignedElevator + 1} to let the passenger in.{Environment.NewLine}");


                            var openTcs = new TaskCompletionSource<bool>();
                            openDoorTcs[passenger.assignedElevator] = openTcs;
                            openTcs.Task.ContinueWith(async t =>
                            {
                                // 等待两秒后，执行后续逻辑
                                await Task.Delay(2000);
                                openDoorButtons[passenger.assignedElevator].BackColor = Color.White;
                                var closeTcs = new TaskCompletionSource<bool>();
                                closeDoorTcs[passenger.assignedElevator] = closeTcs;
                                await closeTcs.Task;

                                // 关门后延迟两秒
                                await Task.Delay(2000);
                                closeDoorButtons[passenger.assignedElevator].BackColor = Color.White;
                                Invoke((MethodInvoker)(() => toTargetFloorOfPassenger(passenger.id, passenger.assignedElevator, passenger.targetFloor)));
                            }, TaskScheduler.FromCurrentSynchronizationContext());
                        }
                    }
                });
            });


            // 其他状态更新也必须在UI线程上执行
            Invoke((MethodInvoker)delegate
            {
                for (int i = 0; i < ElevatorsCount; i++)
                {
                    Elevator elevator = manage.elevators[i];
                    status currentStatus = elevator.getStatus();
                    int capacity = elevator.getCapacity();
                    elevatorCapacityLabels[i].Text = $"Capacity: {capacity}";

                    // 更新状态标签颜色及文字
                    UpdateElevatorStatusLabels(currentStatus, i);
                }
            });
        }

        private void UpdateElevatorStatusLabels(status currentStatus, int elevatorIndex)
        {
            string statusText = currentStatus.ToString();
            elevatorStatusLabels[elevatorIndex].Text = statusText;
            switch (currentStatus)
            {
                case status.up:
                    elevatorStatusLabels[elevatorIndex].ForeColor = Color.Green;
                    break;
                case status.down:
                    elevatorStatusLabels[elevatorIndex].ForeColor = Color.Blue;
                    break;
                case status.wait:
                    elevatorStatusLabels[elevatorIndex].ForeColor = Color.Black;
                    break;
                case status.alarm:
                    elevatorStatusLabels[elevatorIndex].ForeColor = Color.Red;
                    break;
            }
        }


        void toTargetFloorOfPassenger(int passengerId, int assignedElevator, int targetFloor)
        {
            var passenger = passengers.FirstOrDefault(p => p.id == passengerId);
            if (passenger != null)
            {
                // 进行操作...
                if (passenger.currentFloor < targetFloor)
                {
                    manage.upRequest[assignedElevator, targetFloor] = 1;
                    Button floorButton = FindButton(assignedElevator, targetFloor);
                    if (floorButton != null) { floorButton.BackColor = Color.Green; }
                }
                else
                {
                    manage.downRequest[assignedElevator, targetFloor] = 1;
                    Button floorButton = FindButton(assignedElevator, targetFloor);
                    if (floorButton != null) { floorButton.BackColor = Color.Green; }
                }
            }
            else
            {
                // 处理乘客不存在的情况
                Console.WriteLine($"Passenger with ID {passengerId} not found.");
            }
        }

        private void StartAnimation(List<Image> frames, int elevatorIndex)
        {
            int baseY = 20; // 最顶层的基准Y坐标
            int floorHeight = 40; // 每层楼的高度（像素）

            // 计算当前楼层对应的Y坐标
            int newY = baseY + (FloorsCount - 1 - manage.elevators[elevatorIndex].getCurrentFloor()) * floorHeight;
            elevatorPictureBoxes[elevatorIndex].Location = new Point(50, newY); // 设置动画位置
            animationTimer.Tag = new { Frames = frames, Index = elevatorIndex };
            currentAnimationFrame = 0;
            animationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            dynamic data = animationTimer.Tag;
            var frames = data.Frames as List<Image>;
            var elevatorIndex = data.Index;

            if (currentAnimationFrame < frames.Count)
            {
                this.Invoke((MethodInvoker)delegate {
                    elevatorPictureBoxes[elevatorIndex].Image = frames[currentAnimationFrame++];
                });
            }
            else
            {
                animationTimer.Stop();
                this.Invoke((MethodInvoker)delegate {
                    string elevatorImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res", "Elevator.png");
                    elevatorPictureBoxes[elevatorIndex].Image = Image.FromFile(elevatorImagePath);
                });
                currentAnimationFrame = 0; // 重置动画帧索引，为下次动画播放做准备
            }
        }



        private void Manage_OpenDoorPressed(int elevatorIndex)
        {
            if (openDoorTcs.ContainsKey(elevatorIndex))
            {
                openDoorButtons[elevatorIndex].BackColor = Color.Pink;
                var tcs = openDoorTcs[elevatorIndex];
                if (!tcs.Task.IsCompleted) // 检查任务是否已完成
                {
                    tcs.SetResult(true);
                    messageBox.AppendText($"Passenger has entered the elevator. Please press the close door button for Elevator {elevatorIndex + 1} to start the elevator.{Environment.NewLine}");
                    StartAnimation(openDoorImages, elevatorIndex);
                }
                // 考虑在这里移除 tcs 或重置状态
                openDoorTcs.Remove(elevatorIndex); // 完成任务后从字典中移除torIndex + 1} to start the elevator.{Environment.NewLine}");
            }
            else
            {
                StartAnimation(openDoorImages, elevatorIndex);
            }
        }

        private void Manage_CloseDoorPressed(int elevatorIndex)
        {
            if (closeDoorTcs.ContainsKey(elevatorIndex))
            {
                closeDoorButtons[elevatorIndex].BackColor = Color.Yellow;
                var tcs = closeDoorTcs[elevatorIndex];
                if (!tcs.Task.IsCompleted) // 检查任务是否已完成
                {
                    tcs.SetResult(true);
                    messageBox.AppendText($"The door for Elevator {elevatorIndex + 1} has closed. The elevator will start moving shortly.{Environment.NewLine}");
                    StartAnimation(closeDoorImages, elevatorIndex);
                }
                // 考虑在这里移除 tcs 或重置状态
                closeDoorTcs.Remove(elevatorIndex); // 完成任务后从字典中移除
            }
            else
            {
                StartAnimation(closeDoorImages, elevatorIndex);
            }
        }


        private void Manage_ElevatorMoved(int elevatorIndex,int currentFloor)
        {
            this.Invoke(new Action(() => UpdateElevatorPosition(elevatorIndex, currentFloor)));
        }

        private void UpdatePassengerCurrentFloor(int passengerID, int currentFloor)
        {
            // 首先更新passengers列表中的乘客当前楼层
            var passengerToUpdate = passengers.FirstOrDefault(p => p.id == passengerID);
            if (passengerToUpdate != null)
            {
                passengerToUpdate.currentFloor = currentFloor; // 直接更新乘客对象的当前楼层
            }

            // 然后更新DataGridView中对应行的显示
            // 注意：这个操作需要在UI线程执行
            this.Invoke((MethodInvoker)delegate
            {
                foreach (DataGridViewRow row in passengersDataGridView.Rows)
                {
                    // 假设DataGridView的Id列直接存储乘客ID，且不需要调整偏差
                    if ((int)row.Cells["Id"].Value - 1 == passengerID)
                    {
                        // 找到对应行后，更新当前楼层的值
                        row.Cells["CurrentFloor"].Value = currentFloor + 1;
                        break; // 找到后退出循环
                    }
                }
            });
        }



        private void UpdateElevatorPosition(int elevatorIndex, int currentFloor)
        {

            //先更新数码管的数字
            floorDisplayLabels[elevatorIndex].Text = (currentFloor+1).ToString();

            // 假设电梯图标在最顶层楼层（即楼层0）时的Y坐标是20像素
            // 并且每下降一层，图标的Y坐标增加40像素
            int baseY = 20; // 最顶层的基准Y坐标
            int floorHeight = 40; // 每层楼的高度（像素）

            // 计算当前楼层对应的Y坐标
            int newY = baseY + (FloorsCount - 1 - currentFloor) * floorHeight;

            // 确保UI更新在UI线程上执行
            if (elevatorPictureBoxes[elevatorIndex].InvokeRequired)
            {
                elevatorPictureBoxes[elevatorIndex].Invoke(new Action(() =>
                {
                    elevatorPictureBoxes[elevatorIndex].Location = new Point(elevatorPictureBoxes[elevatorIndex].Location.X, newY);
                }));
            }
            else
            {
                elevatorPictureBoxes[elevatorIndex].Location = new Point(elevatorPictureBoxes[elevatorIndex].Location.X, newY);
            }
        }

        private Button FindExternalButton(int floor, int direction)
        {
            // 直接引用添加楼层请求按钮的容器
            GroupBox requestButtonsGroup = this.Controls.OfType<GroupBox>().FirstOrDefault(gb => gb.Text == "Floor Requests");

            if (requestButtonsGroup != null)
            {
                // 遍历GroupBox中的所有按钮
                foreach (Button button in requestButtonsGroup.Controls.OfType<Button>())
                {
                    // 判断按钮的类型（上行或下行）
                    bool isUpButton = button.Text == "↑";
                    bool isDownButton = button.Text == "↓";

                    // 根据方向和楼层找到对应的按钮
                    if ((direction == 1 && isUpButton) || (direction == -1 && isDownButton))
                    {
                        // 按钮的位置用于确定楼层，这里重新计算按钮应在的Y坐标
                        int expectedY = (FloorsCount - 1 - floor) * 40 + 20;
                        // 如果找到楼层匹配的按钮
                        if (button.Location.Y == expectedY)
                        {
                            return button;
                        }
                    }
                }
            }

            return null; // 如果没有找到，返回null
        }


        private Button FindButton(int elevatorIndex, int floor)
        {
            // 假设elevatorGroups是存储每部电梯GroupBox控件引用的数组
            GroupBox groupBox = elevatorGroups[elevatorIndex];
            foreach (Control control in groupBox.Controls)
            {
                // 检查当前控件是否为Button
                if (control is Button button)
                {
                    // 将按钮的文本转换为楼层号，这里假设按钮的Text属性被设置为楼层号
                    if (int.TryParse(button.Text, out int buttonFloor) && buttonFloor == floor + 1)
                    {
                        return button; // 找到对应楼层的按钮
                    }
                }
            }
            return null; // 如果没有找到，返回null
        }

        private Button FindAlarmButton(int elevatorIndex)
        {
            // 假设 elevatorGroups 是存储所有电梯GroupBox引用的数组
            GroupBox elevatorGroupBox = elevatorGroups[elevatorIndex];

            // 遍历电梯GroupBox中的所有控件
            foreach (Control control in elevatorGroupBox.Controls)
            {
                // 如果找到了报警按钮，则返回它
                if (control is Button && control.Text == "Alarm")
                {
                    return (Button)control;
                }
            }

            // 如果没有找到报警按钮，返回null
            return null;
        }


        private void Manage_ExternalRequestProcessed(int floor,int direction)
        {
            this.Invoke((Action)(()=>
            {
                    
                Button requestButton= FindExternalButton(floor, direction);
                if (requestButton != null) 
                {
                        requestButton.BackColor= Color.White;
                }

             }));
        }

        private void Manage_ElevatorRequestProcessed(int elevatorIndex, int currentFloor)
        {
            this.Invoke((Action)(() =>
            {
                Button floorButton = FindButton(elevatorIndex, currentFloor);
                if (floorButton != null)
                {
                    floorButton.BackColor = Color.White;
                    floorButton.Enabled = true;
                }

            }));
        }


        private void FloorButton_Click(object sender, EventArgs e, int elevatorIndex, int floor)
        {

            Button clickButton = sender as Button;
           
            // 处理楼层按钮点击事件
            //MessageBox.Show($"电梯 {elevatorIndex + 1} 的第 {floor + 1} 层按钮被点击");
            if (manage.elevators[elevatorIndex].getStatus()==status.up)
            {
                if (floor >= manage.elevators[elevatorIndex].getCurrentFloor()) 
                {
                    manage.upRequest[elevatorIndex, floor] = 1;
                    if (clickButton != null)
                    {
                        clickButton.BackColor = Color.Green;
                    }

                }
            }
            else if (manage.elevators[elevatorIndex].getStatus()==status.down) 
            {
                if (floor <= manage.elevators[elevatorIndex].getCurrentFloor())
                {
                    manage.downRequest[elevatorIndex, floor] = 1;
                    if (clickButton != null)
                    {
                        clickButton.BackColor = Color.Green;
                    }

                }
            }
            else
            {
                if (floor >= manage.elevators[elevatorIndex].getCurrentFloor())
                {
                    manage.upRequest[elevatorIndex, floor] = 1;
                    if (clickButton != null)
                    {
                        clickButton.BackColor = Color.Green;
                    }

                }
                else 
                {
                    manage.downRequest[elevatorIndex,floor] = 1;
                    if (clickButton != null)
                    {
                        clickButton.BackColor = Color.Green;
                    }

                }
            }
        }
        private void UpRequestButton_Click(object sender, EventArgs e, int floor)
        {
            // 处理向上请求
            //MessageBox.Show($"向上的请求在 {floor + 1} 层被激活");
            Button clickButton = sender as Button;
            if(clickButton!=null)
            {
                clickButton.BackColor = Color.Blue;
            }
            manage.handleUpRequest(floor);
        }

        private void DownRequestButton_Click(object sender, EventArgs e, int floor)
        {
            // 处理向下请求
            //MessageBox.Show($"向下的请求在 {floor + 1} 层被激活");
            Button clickButton = sender as Button;
            if (clickButton != null)
            {
                clickButton.BackColor = Color.Blue;
            }
            manage.handleDownRequest(floor);
        }

        private void TriggerAlarmState(int elevatorIndex, bool isActive)
        {
            Button alarmButton = FindAlarmButton(elevatorIndex);
            GroupBox groupBox = elevatorGroups[elevatorIndex];

            if (isActive)
            {
                // 启动报警状态
                manage.elevators[elevatorIndex].setStatus(status.alarm);
                alarmButton.BackColor = Color.Red;

                // 禁用所有楼层按钮
                foreach (Control control in groupBox.Controls)
                {
                    if (control is Button button && button != alarmButton)
                    {
                        button.Enabled = false;
                    }
                }
                messageBox.AppendText($"Alarm activated due to overload. All floor buttons are disabled for Elevator {elevatorIndex + 1}.{Environment.NewLine}");
            }
            else
            {
                // 取消报警状态
                manage.elevators[elevatorIndex].setStatus(status.wait);
                alarmButton.BackColor = Color.White;

                // 启用所有楼层按钮
                foreach (Control control in groupBox.Controls)
                {
                    if (control is Button button && button != alarmButton)
                    {
                        button.Enabled = true;
                    }
                }
                messageBox.AppendText($"Alarm deactivated. All floor buttons are enabled for Elevator {elevatorIndex + 1}.{Environment.NewLine}");
            }
        }


        private void AlarmButton_Click(object sender, EventArgs e, int elevatorIndex)
        {
            Button alarmButton = sender as Button;
            GroupBox groupBox = elevatorGroups[elevatorIndex];

            // 切换电梯状态
            if (manage.elevators[elevatorIndex].getStatus() != status.alarm)
            {
                TriggerAlarmState(elevatorIndex, true);
            }
            else if (manage.elevators[elevatorIndex].getStatus() == status.alarm)
            {
                TriggerAlarmState(elevatorIndex, false);
            }
        }


    }

}
