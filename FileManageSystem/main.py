import os
import sys
from PyQt5.QtCore import Qt
from PyQt5.QtWidgets import QApplication, QMainWindow, QWidget, QVBoxLayout, QHBoxLayout, QPushButton, QLabel, \
    QLineEdit, QTreeWidget, QTreeWidgetItem, QTextEdit, QTableWidget, QTableWidgetItem,QHeaderView,QDialog,QComboBox,QMessageBox,QMenu,QAction
from PyQt5.QtGui import QIcon
from PyQt5.QtCore import pyqtSignal
from datetime import datetime

from Category import Category
from VirtualDisk import VirtualDisk
from FCB import FCB

class HelpDialog(QDialog):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("操作帮助")
        layout = QVBoxLayout(self)

        # 添加操作帮助内容
        help_text = QLabel("1. 您可以单击鼠标右键『新建文件夹』或『新建文本文件』\n"
                           "2. 双击打开文件或文件夹, 或通过右键进行『打开』和『删除』\n"
                           "3. 搜索框中输入文件夹或以.txt为后缀的文件名进行『搜索』\n"
                           "4. 左侧的目录树中单击『展开文件夹』或双击『打开文件』\n"
                           "5. 选择右侧的按钮进行『格式化』, 同时您也可以『新建文件夹』和『新建文件』\n"
                           "6. 受容量限制, 一个目录下最多可创建8个文件或者子目录")
        layout.addWidget(help_text)

        # 添加关闭按钮
        close_button = QPushButton("我知道了")
        close_button.clicked.connect(self.close)
        layout.addWidget(close_button)
        self.setStyleSheet("QPushButton { background-color: #8FBC8F; color: white; border-radius: 5px; padding: 5px; }"
                           "QPushButton:hover { background-color: #66CDAA; }"
                           "QPushButton:pressed { background-color: #556B2F; }")


class CreateDialog(QDialog):
    def __init__(self, parent, create_type):
        super().__init__(parent)
        self.create_type = create_type  # 'file' or 'folder'
        self.parent = parent  # Save reference to the parent MainWindow instance
        self.init_ui()
        self.resize(500,200)

    def init_ui(self):
        self.setWindowTitle("Create New " + ("Folder" if self.create_type == "folder" else "File"))
        layout = QVBoxLayout(self)

        # Label and text field for entering the name
        label = QLabel("Enter name for new " + ("folder:" if self.create_type == "folder" else "file:"))
        self.name_field = QLineEdit()

        # Buttons for create and cancel
        btn_layout = QHBoxLayout()
        create_btn = QPushButton("Create")
        cancel_btn = QPushButton("Cancel")
        btn_layout.addWidget(create_btn)
        btn_layout.addWidget(cancel_btn)

        layout.addWidget(label)
        layout.addWidget(self.name_field)
        layout.addLayout(btn_layout)

        create_btn.clicked.connect(self.create)
        cancel_btn.clicked.connect(self.reject)

    def create(self):
        name = self.name_field.text().strip()
        if name:
            if self.create_type == "folder":
                self.parent.create_folder(name)
            elif self.create_type == "file":
                self.parent.create_file(name)
            self.accept()
        else:
            QMessageBox.warning(self, "Error", "Name cannot be empty!", QMessageBox.Ok)
from PyQt5.QtWidgets import QDialog, QTextEdit, QVBoxLayout, QMessageBox, QApplication
from PyQt5.QtCore import pyqtSignal
from datetime import datetime

class NoteForm(QDialog):
    textChanged = pyqtSignal()

    def __init__(self, filename, main_form, parent=None):
        super().__init__(parent)
        self.main_form = main_form  # Reference to MainWindow instance
        self.filename = filename
        self.ischanged = False
        self.init_ui()
        self.load_content()

    def init_ui(self):
        self.setWindowTitle(self.filename)
        self.textEdit = QTextEdit()
        self.textEdit.textChanged.connect(self.on_text_changed)
        layout = QVBoxLayout()
        layout.addWidget(self.textEdit)
        self.setLayout(layout)
        self.resize(800, 500)

    def on_text_changed(self):
        self.ischanged = True

    def load_content(self):
        # Fetching FCB for the file
        fcb = self.main_form.category.search(self.main_form.category.root, self.filename, FCB.TXTFILE).fcb
        # Loading content from the virtual disk
        content = self.main_form.disk.get_file_content(fcb)
        if content:
            self.textEdit.setText(content)
        self.ischanged = False

    def closeEvent(self, event):
        if self.ischanged:
            reply = QMessageBox.question(self, 'Save Changes', "Do you want to save changes to the document?",
                                         QMessageBox.Yes | QMessageBox.No | QMessageBox.Cancel, QMessageBox.Yes)
            if reply == QMessageBox.Yes:
                self.save_content()
                event.accept()  # Allow the dialog to close
            elif reply == QMessageBox.Cancel:
                event.ignore()  # Prevent the dialog from closing
        else:
            event.accept()  # No changes were made, allow closing

    def save_content(self):
        content = self.textEdit.toPlainText()
        fcb = self.main_form.category.search(self.main_form.category.root, self.filename, FCB.TXTFILE).fcb
        old_size = fcb.size
        new_size = len(content)
        # Update file size and modification time
        fcb.size = new_size
        fcb.lastModify = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        # Attempt to update the file content on the disk
        if not self.main_form.disk.file_update(fcb.start, old_size, fcb, content):
            QMessageBox.critical(self, 'Error', 'Failed to save file on disk.')
        else:
            QMessageBox.information(self, 'Success', 'File saved successfully.')

        self.main_form.update_disk_info()

class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        self.resize(1600, 650)
        self.setWindowTitle("FileManageSystem")
        self.setObjectName("window")

        root = FCB("root", FCB.FOLDER, "", 1)
        self.root_node = Category.Node(root)
        self.current_node = self.root_node
        self.category = Category(self.root_node)
        self.disk = VirtualDisk(200, 2)
        self.current_path = ""

        #恢复文件系统
        self.read_bit_map()
        self.read_my_disk()
        self.init_ui()
    def init_ui(self):
        self.setStyleSheet("QMainWindow, QTreeWidget, QTextEdit, QTableWidget { background-color: #E0FFE0; }"
                           "QPushButton { background-color: #8FBC8F; color: white; border-radius: 10px; padding: 5px;}"
                           "QPushButton:hover { background-color: #66CDAA; }"
                           "QPushButton:pressed { background-color: #556B2F; }"
                           "QLineEdit { border-radius: 5px; }")

        # 主布局容器
        central_widget = QWidget()
        self.setCentralWidget(central_widget)
        main_layout = QHBoxLayout()
        central_widget.setLayout(main_layout)

        # 左边布局 - 文件目录树
        self.tree = QTreeWidget()
        self.tree.setHeaderLabel("Directories")
        self.setup_tree()
        main_layout.addWidget(self.tree, 1)

        # 中间布局 - 文件显示和路径
        middle_layout = QVBoxLayout()
        path_layout = QHBoxLayout()  # 水平布局用于路径显示和按钮
        self.path_label = QLabel("Current Path: ")
        self.path_display = QLineEdit()  # 只读文本框显示当前路径
        self.path_display.setReadOnly(True)
        self.up_button = QPushButton("Up")  # 返回上级目录按钮
        self.up_button.clicked.connect(self.go_up)
        path_layout.addWidget(self.path_label)
        path_layout.addWidget(self.path_display)
        path_layout.addWidget(self.up_button)

        # Set up the file table in the middle layout
        self.file_table = QTableWidget()
        self.file_table.setColumnCount(5)  # Icon, File Name, Last Modified, Type, Size
        self.file_table.setHorizontalHeaderLabels(["Icon", "File Name", "Last Modified", "Type", "Size"])
        # Setup the edit triggers to prevent editing
        self.file_table.setEditTriggers(QTableWidget.NoEditTriggers)
        # Set Icon column fixed and to a reasonable width
        self.file_table.setColumnWidth(0, 50)
        self.file_table.horizontalHeader().setSectionResizeMode(0, QHeaderView.ResizeToContents)
        # Automatically adjust the width of FileName, Type, and Size columns based on their content
        self.file_table.horizontalHeader().setSectionResizeMode(2, QHeaderView.Stretch)
        self.file_table.horizontalHeader().setSectionResizeMode(3, QHeaderView.ResizeToContents)
        self.file_table.horizontalHeader().setSectionResizeMode(4, QHeaderView.ResizeToContents)
        # Stretch the "Last Modified" column to use any remaining space
        self.file_table.horizontalHeader().setSectionResizeMode(1, QHeaderView.Stretch)
        self.file_table.horizontalHeader().setSectionsMovable(False)
        self.file_table.cellClicked.connect(self.cell_clicked)
        
        middle_layout.addLayout(path_layout)  # 将路径布局加入到中间布局
        middle_layout.addWidget(self.file_table)
        main_layout.addLayout(middle_layout, 2)

        # 右边布局 - 操作按钮和磁盘信息
        right_layout = QVBoxLayout()
        self.format_button = QPushButton("Format Disk")
        self.new_folder_button = QPushButton("New Folder")
        self.new_file_button = QPushButton("New File")
        self.format_button.clicked.connect(self.format)
        self.new_folder_button.clicked.connect(self.new_folder_button_clicked)
        self.new_file_button.clicked.connect(self.new_file_button_clicked)
        self.disk_info_label = QLabel(
            f"Disk Size: {self.disk.size} B\nBlock Size: {self.disk.block_size} B\nRemaining Space: {self.disk.remain} blocks")
        right_layout.addWidget(self.format_button)
        right_layout.addWidget(self.new_folder_button)
        right_layout.addWidget(self.new_file_button)
        right_layout.addWidget(self.disk_info_label)
        right_layout.addStretch()
        main_layout.addLayout(right_layout, 1)

        search_layout = QHBoxLayout()
        self.search_input = QLineEdit()
        self.search_type = QComboBox()
        self.search_type.addItems(["Folder", "Text File"])
        self.search_button = QPushButton("Search")
        self.search_button.clicked.connect(self.perform_search)  # 搜索函数需要根据实际情况实现

        search_layout.addWidget(self.search_input)
        search_layout.addWidget(self.search_type)
        search_layout.addWidget(self.search_button)
        right_layout.insertLayout(0, search_layout)  # 将搜索布局添加到右侧布局的顶部

    def update_disk_info(self):
        # Assuming `disk_info_label` is already added to the layout of MainWindow
        self.disk_info_label.setText(
            f"Disk Size: {self.disk.size} B\n"
            f"Block Size: {self.disk.block_size} B\n"
            f"Remaining Space: {self.disk.remain} blocks"
        )

    def setup_tree(self):
        # Dummy data for demonstration
        root_item = QTreeWidgetItem(self.tree, ["Root"])
        child_item = QTreeWidgetItem(root_item, ["Child"])
        root_item.setExpanded(True)

    def display_file_folder_info(self, name, modified_time, f_type, size):
        row_position = self.file_table.rowCount()
        self.file_table.insertRow(row_position)

        # Load the icon based on the file type
        icon_path = "res/folder.jpg" if f_type == FCB.FOLDER else "res/file.png"
        icon = QIcon(icon_path)
        pixmap = icon.pixmap(24, 24)  # Get QPixmap from QIcon

        icon_label = QLabel()
        icon_label.setPixmap(pixmap)  # Set the QPixmap into the QLabel

        type = "File" if f_type == FCB.TXTFILE else "Folder"
        # Inserting data into the row
        self.file_table.setCellWidget(row_position, 0, icon_label)
        self.file_table.setItem(row_position, 1, QTableWidgetItem(name))
        self.file_table.setItem(row_position, 2, QTableWidgetItem(modified_time))
        self.file_table.setItem(row_position, 3, QTableWidgetItem(type))
        if f_type == FCB.TXTFILE:
            self.file_table.setItem(row_position, 4, QTableWidgetItem(str(size)))

    def format(self):
        pass

    def new_file_button_clicked(self):
        dlg = CreateDialog(self, "file")
        dlg.exec_()

    def new_folder_button_clicked(self):
        dlg = CreateDialog(self, "folder")
        dlg.exec_()

    def contextMenuEvent(self, event):
        contextMenu = QMenu(self)

        newFileAct = QAction('New File', self)
        newFolderAct = QAction('New Folder', self)
        newFileAct.triggered.connect(self.new_file_button_clicked)
        newFolderAct.triggered.connect(self.new_folder_button_clicked)

        contextMenu.addAction(newFileAct)
        contextMenu.addAction(newFolderAct)
        contextMenu.exec_(event.globalPos())

    def cell_clicked(self, row, column):
        if column == 1 and self.file_table.item(row,3).text() == "File":  # Assuming file names are in the second column
            filename = self.file_table.item(row, column).text()
            # Placeholder for opening the text for editing
            # This should actually fetch the text from wherever your data is stored
            self.open_editor(filename)

    def open_editor(self, filename):
        editor = NoteForm(filename, self)
        editor.exec_()
    def create_folder(self, name):
        print(f"Creating folder named: {name}")
        # Actual folder creation logic here
        if name == self.current_node.fcb.file_name:
            name = "_" + name
        if not self.category.check_same_name(self.current_node,name, FCB.FOLDER):
            # If name is duplicated, show a warning message
            QMessageBox.warning(self, "Warning", f"A folder named {name} already exists.")
        else:
            current_time = datetime.now()
            formatted_time = current_time.strftime("%Y-%m-%d %H:%M:%S")

            self.display_file_folder_info(name,formatted_time,FCB.FOLDER,0)
            self.category.create_file(self.current_node.fcb.file_name,FCB(name,FCB.FOLDER,formatted_time,0))


    def create_file(self, name):
        print(f"Creating file named: {name}")
        # Check if the same name already exists
        if not self.category.check_same_name(self.current_node,name, FCB.TXTFILE):
            # If name is duplicated, show a warning message
            QMessageBox.warning(self, "Warning", f"A file named {name} already exists.")
        else:
            # If the name is unique, create the file
            current_time = datetime.now()
            formatted_time = current_time.strftime("%Y-%m-%d %H:%M:%S")
            # Assuming FCB initialization accepts a datetime object directly
            self.display_file_folder_info(name,formatted_time,FCB.TXTFILE,0)
            self.category.create_file(self.current_node.fcb.file_name, FCB(name, FCB.TXTFILE, formatted_time, 0))
            print(f"File {name} created successfully.")


    def go_up(self):
        # 这里应实现返回上级目录的逻辑
        print("Return to the upper directory")

    def read_bit_map(self):
        path = os.path.join(os.getcwd(), "BitMapInfo.txt")
        if os.path.exists(path):
            with open(path, 'r') as reader:
                for i in range(self.disk.block_num):
                    self.disk.bit_map[i] = int(reader.readline().strip())

    def write_bit_map(self):
        path = os.path.join(os.getcwd(), "BitMapInfo.txt")  # 获取当前工作目录并创建文件路径
        if os.path.exists(path):  # 检查文件是否存在
            os.remove(path)  # 如果存在，则删除文件

        with open(path, 'w') as writer:  # 打开文件进行写操作
            for i in range(self.disk.block_num):
                writer.write(f"{self.disk.bit_map[i]}\n")  # 将每个位图条目写入文件，并添加换行符

    def read_my_disk(self):
        path = os.path.join(os.getcwd(), "MyDiskInfo.txt")
        if os.path.exists(path):
            with open(path, 'r', encoding='utf-8') as reader:
                for i in range(self.disk.block_num):
                    line = reader.readline().strip()
                    # Decode the line
                    line = line.replace("(((", "(")  # Decode left parentheses
                    line = line.replace(")))", ")")  # Decode right parentheses
                    line = line.replace("|||", "\r\n")  # Decode newlines
                    self.disk.memory[i] = line

    def write_my_disk(self):
        path = os.path.join(os.getcwd(), "MyDiskInfo.txt")
        if os.path.exists(path):
            os.remove(path)

        with open(path, 'w', encoding='utf-8') as writer:
            for data in self.disk.memory:
                # Encode the line
                encoded_data = data.replace("(", "(((")  # Encode left parentheses
                encoded_data = encoded_data.replace(")", ")))")  # Encode right parentheses
                encoded_data = encoded_data.replace("\r\n", "|||")  # Encode newlines
                writer.write(encoded_data + '\n')


    def perform_search(self):
        # 根据输入和选择的类型进行搜索
        pass  # 实现具体的搜索逻辑


if __name__ == '__main__':
    app = QApplication(sys.argv)
    window = MainWindow()
    window.show()
    help_dialog = HelpDialog()  # 创建帮助对话框
    help_dialog.show()  # 显示帮助对话框
    sys.exit(app.exec_())