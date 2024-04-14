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
        private Label[] elevatorStatusLabels = new Label[ElevatorsCount]; // �洢״̬��ǩ������
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
            float scale = 0.6f; // �������ű���Ϊ80%

            for (int i = 1; i <= 4; i++)
            {
                Image openImage = Image.FromFile(Path.Combine(baseDir, "���Ŷ���", $"{i}.png"));
                openDoorImages.Add(ResizeImage(openImage, scale));
                openImage.Dispose(); // �ͷ�ԭʼͼ����Դ

                Image closeImage = Image.FromFile(Path.Combine(baseDir, "���Ŷ���", $"{i}.png"));
                closeDoorImages.Add(ResizeImage(closeImage, scale));
                closeImage.Dispose(); // �ͷ�ԭʼͼ����Դ
            }
        }


        private void InitializeTimer()
        {
            animationTimer.Interval = 200; // ���ö���֡�л���ʱ����Ϊ200����
            animationTimer.Tick += new EventHandler(AnimationTimer_Tick);
            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 1000;  // ���¼������Ϊ1000���루1�룩
            updateTimer.Tick += new EventHandler(UpdateTimer_Tick);
            updateTimer.Start();
            // ��ʼ����ʱ��
            checkElevatorTimer = new System.Timers.Timer(1000); // ����ʱ����Ϊ1��
            checkElevatorTimer.Elapsed += CheckElevatorStatus;
            checkElevatorTimer.AutoReset = true;
            checkElevatorTimer.Enabled = true; // ��ʼ״̬Ϊ����
        }

        private void InitialMassageBox()
        {
            // ��ʼ����Ϣ��
            messageBox = new TextBox
            {
                Multiline = true, // ��������ı�
                ReadOnly = true,  // ����Ϊֻ��
                ScrollBars = ScrollBars.Vertical, // ��ֱ������
                Location = new System.Drawing.Point(10, this.ClientSize.Height - 200), // �趨λ��
                Size = new System.Drawing.Size(this.ClientSize.Width - 20, 150), // �趨��С
                BackColor = System.Drawing.Color.White, // ������ɫ
                Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Regular), // ��������
            };
            this.Controls.Add(messageBox); // ��ӵ�����Ŀؼ���
        }

        private void InitializeElevatorUI()
        {
            for (int elevatorIndex = 0; elevatorIndex < ElevatorsCount; elevatorIndex++)
            {
                // ����ÿ�����ݵ�GroupBox
                GroupBox groupBox = new GroupBox
                {
                    Text = $"Elevator {elevatorIndex + 1}",
                    Size = new System.Drawing.Size(150, FloorsCount * 40 + 80),
                    Location = new System.Drawing.Point(elevatorIndex * 225 + 10, 75)
                };
                this.Controls.Add(groupBox);
                elevatorGroups[elevatorIndex] = groupBox;

                //��������ͼ��
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
                    Text = "1", // �����ʼ¥��Ϊ1
                    AutoSize = true,
                    Location = new System.Drawing.Point(elevatorIndex * 225 + 20, 30),
                    Font = new Font("Arial", 16, FontStyle.Bold),
                    ForeColor = Color.Black,
                    BackColor = Color.Transparent
                };
                this.Controls.Add(floorDisplay);
                floorDisplayLabels[elevatorIndex] = floorDisplay; // �洢Label����

                // ��������״̬��ʾLabel
                Label statusLabel = new Label
                {
                    Text = "wait", // ��ʼ״̬Ϊ�ȴ�
                    AutoSize = true,
                    Location = new System.Drawing.Point(10, FloorsCount * 40 + 60),
                    Font = new Font("Arial", 8, FontStyle.Regular),
                    ForeColor = Color.Black,
                    BackColor = Color.Transparent
                };
                groupBox.Controls.Add(statusLabel);
                elevatorStatusLabels[elevatorIndex] = statusLabel; // �洢״̬Label

                // ����ʣ��������ʾLabel
                Label capacityLabel = new Label
                {
                    Text = "Capacity: 5", // �����ʼ����Ϊ10�������Ը���ʵ���������
                    AutoSize = true,
                    Location = new System.Drawing.Point(elevatorIndex * 225 + 70, 40),
                    Font = new Font("Arial", 8, FontStyle.Bold),
                    ForeColor = Color.Black,
                    BackColor = Color.Transparent
                };
                this.Controls.Add(capacityLabel);
                elevatorCapacityLabels[elevatorIndex] = capacityLabel; // �洢����Label


                // ����¥�㰴ť
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

                // ����������ť
                Button alarmButton = new Button
                {
                    Text = "Alarm",
                    Size = new System.Drawing.Size(80, 30),
                    Location = new System.Drawing.Point(20, FloorsCount * 40 + 20)
                };
                int temp = elevatorIndex;
                alarmButton.Click += (sender, e) => AlarmButton_Click(sender, e, temp);
                groupBox.Controls.Add(alarmButton);

                // �������Ű�ť
                Button openButton = new Button
                {
                    Text = "Open",
                    Size = new System.Drawing.Size(70, 30),
                    Location = new System.Drawing.Point(elevatorIndex * 225 + 10, groupBox.Bottom + 5)
               
                };
                openButton.Click += (sender, e) => manage.TriggerOpenDoor(temp); // ����¼�������
                this.Controls.Add(openButton);
                openDoorButtons[elevatorIndex] = openButton;

                // �������Ű�ť
                Button closeButton = new Button
                {
                    Text = "Close",
                    Size = new System.Drawing.Size(70, 30),
                    Location = new System.Drawing.Point(elevatorIndex * 225 + 85, groupBox.Bottom + 5)
                };
                closeButton.Click += (sender, e) => manage.TriggerCloseDoor(temp); // ����¼�������
                this.Controls.Add(closeButton);
                closeDoorButtons[elevatorIndex] = closeButton;
            }
        }

        private void InitializeFloorRequestButtons()
        {
            // ���¥������ť������
            GroupBox requestButtonsGroup = new GroupBox
            {
                Text = "Floor Requests",
                Size = new System.Drawing.Size(200, FloorsCount * 40 + 80),
                Location = new System.Drawing.Point(ElevatorsCount * 225 + 10, 75)
            };
            this.Controls.Add(requestButtonsGroup);

            for (int floor = 0; floor < FloorsCount; floor++)
            {
                // ��������ť
                if (floor < FloorsCount - 1) // ���˶���
                {
                    Button upRequestButton = new Button
                    {
                        Text = "��",
                        Size = new System.Drawing.Size(40, 30),
                        Location = new System.Drawing.Point(10, (FloorsCount - 1 - floor) * 40 + 20)
                    };
                    int temp = floor;
                    upRequestButton.Click += (sender, e) => UpRequestButton_Click(sender, e, temp);
                    requestButtonsGroup.Controls.Add(upRequestButton);
                }

                // ��������ť
                if (floor > 0) // �����ײ�
                {
                    Button downRequestButton = new Button
                    {
                        Text = "��",
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
            // �����˿͹����GroupBox
            passengersGroup = new GroupBox
            {
                Text = "Passenger Management",
                Size = new System.Drawing.Size(400, FloorsCount * 40 + 80), // ������Ҫ������С
                Location = new System.Drawing.Point(ElevatorsCount * 225 + 220, 75) // ���ݵ�������������UIԪ�ص�λ�õ���
            };
            this.Controls.Add(passengersGroup);

            // ��ӳ˿Ͱ�ť
            Button addPassengerButton = new Button
            {
                Text = "Add Passenger",
                Size = new System.Drawing.Size(180, 30),
                Location = new System.Drawing.Point(10, 20)
            };
            passengersGroup.Controls.Add(addPassengerButton);
            addPassengerButton.Click += AddPassengerButton_Click; // ��Ҫ������¼��������

            // ������ǰ¥���ǩ�������б�
            Label currentFloorLabel = new Label
            {
                Text = "Current Floor:",
                AutoSize = true,
                Location = new System.Drawing.Point(10,50) // ����λ������Ӧ���沼��
            };
            currentFloorComboBox = new ComboBox
            {
                Size = new System.Drawing.Size(180, 20),
                DropDownStyle = ComboBoxStyle.DropDownList, // ����Ϊֻ�������б�
                Location = new System.Drawing.Point(10, 80) // �����б�λ�ڱ�ǩ���·�
            };

            // ����Ŀ��¥���ǩ�������б�
            Label targetFloorLabel = new Label
            {
                Text = "Target Floor",
                AutoSize = true,
                Location = new System.Drawing.Point(10, 110) // ������currentFloorComboBox�Ķ���
            };
            targetFloorComboBox = new ComboBox
            {
                Size = new System.Drawing.Size(180, 20),
                DropDownStyle = ComboBoxStyle.DropDownList, // ����Ϊֻ�������б�
                Location = new System.Drawing.Point(10, 140) // �����б�λ�ڱ�ǩ���·�
            };

            // ��������б���
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
                Location = new System.Drawing.Point(10, 200), // ��������Ӧ����
                Size = new System.Drawing.Size(380, 700), // ������Ҫ������С
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false, // ��ֹ�û�ֱ�������
                AllowUserToOrderColumns = false,
                AllowUserToResizeColumns = false, 
                AllowUserToResizeRows = false,
                ReadOnly = true, // ����Ϊֻ��
                RowHeadersVisible = false, // ����ʾ��ͷ
                AutoGenerateColumns = false
            };

         
            passengersDataGridView.Columns.Add("Id", "ID");
            passengersDataGridView.Columns.Add("CurrentFloor", "Current");
            passengersDataGridView.Columns.Add("TargetFloor", "Target");
            passengersDataGridView.Columns.Add("AssignedElevator", "Assigned");

            passengersGroup.Controls.Add(passengersDataGridView);

        }

        // ������ӳ˿Ͱ�ť���¼��������
        private void AddPassengerButton_Click(object sender, EventArgs e)
        {
            // �˴���Ӵ����߼���������ʾһ���´���������˿���Ϣ��ֱ���ڵ�ǰ�������

            int currentFloor=(int)currentFloorComboBox.SelectedIndex;
            int targetFloor=(int)targetFloorComboBox.SelectedIndex;
            int assignedElevator = -1;

            // ����Ƿ���¥��δ��ѡ��
            if (currentFloor == -1 || targetFloor == -1)
            {
                MessageBox.Show("Please select both current and target floors.", "Selection Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return; // �˳�������������ִ����ӳ˿͵��߼�
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
                this.Invoke((MethodInvoker)delegate // ȷ����UI�߳���ִ��
                {
                    MessageBox.Show($"���� {assignedElevator + 1} �ڵľ�������,���ݳ��أ�");
                    TriggerAlarmState(assignedElevator, true);
                    var timer = new System.Windows.Forms.Timer { Interval = 5000 }; // 5���������
                    timer.Tick += (sender, args) =>
                    {
                        this.Invoke((MethodInvoker)delegate // �ٴ�ȷ����UI�߳���ִ��
                        {
                            TriggerAlarmState(assignedElevator, false);
                        });
                        timer.Stop();
                        timer.Dispose(); // ֹͣ��ʱ�����ͷ���Դ
                    };
                    timer.Start();
                });
                manage.elevators[assignedElevator].ariveTargetFloor(); // �������ǵ���Ŀ��¥��ķ���
            }
            else
            {
                this.Invoke((MethodInvoker)delegate // ȷ����UI�߳���ִ��
                {
                    // ����³˿�
                    passengers.Add(newPassenger);
                    // ����DataGridView
                    passengersDataGridView.Rows.Add(newPassenger.id + 1, currentFloor + 1, targetFloor + 1, assignedElevator + 1);
                    // ������Ϣ����ʾ������Ϣ
                    string message = $"Passenger {newPassenger.id + 1} has been assigned to Elevator {assignedElevator + 1}.";
                    messageBox.AppendText(message + Environment.NewLine); // �������Ϣ����Ϣ�򣬲�����
                });
            }
        }



        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            Dictionary<int, int> floorUpdates = new Dictionary<int, int>();

            // �����ڲ�����һ����Ҫ���µĳ˿��б����
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

            // ����ʹ��Invoke����ΪTimer Tick�¼�Ĭ����UI�߳���ִ��
            foreach (var update in floorUpdates)
            {
                UpdatePassengerCurrentFloor(update.Key, update.Value);
            }
            CheckAndRemoveArrivedPassengers();
        }


        private void CheckAndRemoveArrivedPassengers()
        {
            // ע�⣺���´������Ҫ��UI�߳�ִ�У���Ϊ��������UI���
            List<Passenger> passengersToRemove = new List<Passenger>();

            foreach (var passenger in passengers)
            {
                // ������ĵ����Ƿ��ѵ���˿͵�Ŀ��¥��
                if (passenger.currentFloor==passenger.targetFloor)
                {
                    manage.elevators[passenger.assignedElevator].ariveTargetFloor();
                    // �Ƴ���Ӧ��DataGridView��
                    foreach (DataGridViewRow row in passengersDataGridView.Rows)
                    {
                        if ((int)row.Cells["Id"].Value - 1 == passenger.id)
                        {
                            passengersDataGridView.Invoke((MethodInvoker)delegate
                            {
                                passengersDataGridView.Rows.RemoveAt(row.Index);
                            });
                            break; // �ҵ����˳�ѭ��
                        }
                    }
                    // ��ǳ˿��Ա���б���ɾ��
                    passengersToRemove.Add(passenger);
                }
            }

            // �ڳ˿��б���ɾ���ѵ���ĳ˿�
            foreach (var passenger in passengersToRemove)
            {
                passengers.Remove(passenger);
            }
        }


        private async void CheckElevatorStatus(Object source, System.Timers.ElapsedEventArgs e)
        {
            // ��Task.Run��ִ����Ҫ��UI�߳�����ɵĲ���
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
                                // �ȴ������ִ�к����߼�
                                await Task.Delay(2000);
                                openDoorButtons[passenger.assignedElevator].BackColor = Color.White;
                                var closeTcs = new TaskCompletionSource<bool>();
                                closeDoorTcs[passenger.assignedElevator] = closeTcs;
                                await closeTcs.Task;

                                // ���ź��ӳ�����
                                await Task.Delay(2000);
                                closeDoorButtons[passenger.assignedElevator].BackColor = Color.White;
                                Invoke((MethodInvoker)(() => toTargetFloorOfPassenger(passenger.id, passenger.assignedElevator, passenger.targetFloor)));
                            }, TaskScheduler.FromCurrentSynchronizationContext());
                        }
                    }
                });
            });


            // ����״̬����Ҳ������UI�߳���ִ��
            Invoke((MethodInvoker)delegate
            {
                for (int i = 0; i < ElevatorsCount; i++)
                {
                    Elevator elevator = manage.elevators[i];
                    status currentStatus = elevator.getStatus();
                    int capacity = elevator.getCapacity();
                    elevatorCapacityLabels[i].Text = $"Capacity: {capacity}";

                    // ����״̬��ǩ��ɫ������
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
                // ���в���...
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
                // ����˿Ͳ����ڵ����
                Console.WriteLine($"Passenger with ID {passengerId} not found.");
            }
        }

        private void StartAnimation(List<Image> frames, int elevatorIndex)
        {
            int baseY = 20; // ���Ļ�׼Y����
            int floorHeight = 40; // ÿ��¥�ĸ߶ȣ����أ�

            // ���㵱ǰ¥���Ӧ��Y����
            int newY = baseY + (FloorsCount - 1 - manage.elevators[elevatorIndex].getCurrentFloor()) * floorHeight;
            elevatorPictureBoxes[elevatorIndex].Location = new Point(50, newY); // ���ö���λ��
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
                currentAnimationFrame = 0; // ���ö���֡������Ϊ�´ζ���������׼��
            }
        }



        private void Manage_OpenDoorPressed(int elevatorIndex)
        {
            if (openDoorTcs.ContainsKey(elevatorIndex))
            {
                openDoorButtons[elevatorIndex].BackColor = Color.Pink;
                var tcs = openDoorTcs[elevatorIndex];
                if (!tcs.Task.IsCompleted) // ��������Ƿ������
                {
                    tcs.SetResult(true);
                    messageBox.AppendText($"Passenger has entered the elevator. Please press the close door button for Elevator {elevatorIndex + 1} to start the elevator.{Environment.NewLine}");
                    StartAnimation(openDoorImages, elevatorIndex);
                }
                // �����������Ƴ� tcs ������״̬
                openDoorTcs.Remove(elevatorIndex); // ����������ֵ����Ƴ�torIndex + 1} to start the elevator.{Environment.NewLine}");
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
                if (!tcs.Task.IsCompleted) // ��������Ƿ������
                {
                    tcs.SetResult(true);
                    messageBox.AppendText($"The door for Elevator {elevatorIndex + 1} has closed. The elevator will start moving shortly.{Environment.NewLine}");
                    StartAnimation(closeDoorImages, elevatorIndex);
                }
                // �����������Ƴ� tcs ������״̬
                closeDoorTcs.Remove(elevatorIndex); // ����������ֵ����Ƴ�
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
            // ���ȸ���passengers�б��еĳ˿͵�ǰ¥��
            var passengerToUpdate = passengers.FirstOrDefault(p => p.id == passengerID);
            if (passengerToUpdate != null)
            {
                passengerToUpdate.currentFloor = currentFloor; // ֱ�Ӹ��³˿Ͷ���ĵ�ǰ¥��
            }

            // Ȼ�����DataGridView�ж�Ӧ�е���ʾ
            // ע�⣺���������Ҫ��UI�߳�ִ��
            this.Invoke((MethodInvoker)delegate
            {
                foreach (DataGridViewRow row in passengersDataGridView.Rows)
                {
                    // ����DataGridView��Id��ֱ�Ӵ洢�˿�ID���Ҳ���Ҫ����ƫ��
                    if ((int)row.Cells["Id"].Value - 1 == passengerID)
                    {
                        // �ҵ���Ӧ�к󣬸��µ�ǰ¥���ֵ
                        row.Cells["CurrentFloor"].Value = currentFloor + 1;
                        break; // �ҵ����˳�ѭ��
                    }
                }
            });
        }



        private void UpdateElevatorPosition(int elevatorIndex, int currentFloor)
        {

            //�ȸ�������ܵ�����
            floorDisplayLabels[elevatorIndex].Text = (currentFloor+1).ToString();

            // �������ͼ�������¥�㣨��¥��0��ʱ��Y������20����
            // ����ÿ�½�һ�㣬ͼ���Y��������40����
            int baseY = 20; // ���Ļ�׼Y����
            int floorHeight = 40; // ÿ��¥�ĸ߶ȣ����أ�

            // ���㵱ǰ¥���Ӧ��Y����
            int newY = baseY + (FloorsCount - 1 - currentFloor) * floorHeight;

            // ȷ��UI������UI�߳���ִ��
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
            // ֱ���������¥������ť������
            GroupBox requestButtonsGroup = this.Controls.OfType<GroupBox>().FirstOrDefault(gb => gb.Text == "Floor Requests");

            if (requestButtonsGroup != null)
            {
                // ����GroupBox�е����а�ť
                foreach (Button button in requestButtonsGroup.Controls.OfType<Button>())
                {
                    // �жϰ�ť�����ͣ����л����У�
                    bool isUpButton = button.Text == "��";
                    bool isDownButton = button.Text == "��";

                    // ���ݷ����¥���ҵ���Ӧ�İ�ť
                    if ((direction == 1 && isUpButton) || (direction == -1 && isDownButton))
                    {
                        // ��ť��λ������ȷ��¥�㣬�������¼��㰴ťӦ�ڵ�Y����
                        int expectedY = (FloorsCount - 1 - floor) * 40 + 20;
                        // ����ҵ�¥��ƥ��İ�ť
                        if (button.Location.Y == expectedY)
                        {
                            return button;
                        }
                    }
                }
            }

            return null; // ���û���ҵ�������null
        }


        private Button FindButton(int elevatorIndex, int floor)
        {
            // ����elevatorGroups�Ǵ洢ÿ������GroupBox�ؼ����õ�����
            GroupBox groupBox = elevatorGroups[elevatorIndex];
            foreach (Control control in groupBox.Controls)
            {
                // ��鵱ǰ�ؼ��Ƿ�ΪButton
                if (control is Button button)
                {
                    // ����ť���ı�ת��Ϊ¥��ţ�������谴ť��Text���Ա�����Ϊ¥���
                    if (int.TryParse(button.Text, out int buttonFloor) && buttonFloor == floor + 1)
                    {
                        return button; // �ҵ���Ӧ¥��İ�ť
                    }
                }
            }
            return null; // ���û���ҵ�������null
        }

        private Button FindAlarmButton(int elevatorIndex)
        {
            // ���� elevatorGroups �Ǵ洢���е���GroupBox���õ�����
            GroupBox elevatorGroupBox = elevatorGroups[elevatorIndex];

            // ��������GroupBox�е����пؼ�
            foreach (Control control in elevatorGroupBox.Controls)
            {
                // ����ҵ��˱�����ť���򷵻���
                if (control is Button && control.Text == "Alarm")
                {
                    return (Button)control;
                }
            }

            // ���û���ҵ�������ť������null
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
           
            // ����¥�㰴ť����¼�
            //MessageBox.Show($"���� {elevatorIndex + 1} �ĵ� {floor + 1} �㰴ť�����");
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
            // ������������
            //MessageBox.Show($"���ϵ������� {floor + 1} �㱻����");
            Button clickButton = sender as Button;
            if(clickButton!=null)
            {
                clickButton.BackColor = Color.Blue;
            }
            manage.handleUpRequest(floor);
        }

        private void DownRequestButton_Click(object sender, EventArgs e, int floor)
        {
            // ������������
            //MessageBox.Show($"���µ������� {floor + 1} �㱻����");
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
                // ��������״̬
                manage.elevators[elevatorIndex].setStatus(status.alarm);
                alarmButton.BackColor = Color.Red;

                // ��������¥�㰴ť
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
                // ȡ������״̬
                manage.elevators[elevatorIndex].setStatus(status.wait);
                alarmButton.BackColor = Color.White;

                // ��������¥�㰴ť
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

            // �л�����״̬
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
