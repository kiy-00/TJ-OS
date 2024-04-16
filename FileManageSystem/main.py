import os
import sys
from PyQt5.QtCore import Qt
from PyQt5.QtWidgets import QMainWindow, QWidget, QHBoxLayout, QPushButton, QLabel, \
    QLineEdit, QTreeWidget, QTreeWidgetItem, QTableWidget, QTableWidgetItem,QHeaderView,QDialog,QComboBox,\
    QMenu,QAction
from PyQt5.QtGui import QIcon,QPixmap
from PyQt5 import QtGui

from Category import Category
from VirtualDisk import VirtualDisk
from FCB import FCB

class HelpDialog(QDialog):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("操作帮助")
        layout = QVBoxLayout(self)
        self.setWindowIcon(QIcon('res/icon.jpg'))  # Adjust the path as necessary

        # 添加操作帮助内容
        help_text = QLabel("1. 您可以单击鼠标右键『新建文件夹』或『新建文本文件』\n"
                           "2. 双击打开文件或文件夹, 或通过右键进行『打开』和『删除』\n"
                           "3. 搜索框中输入文件夹或以.txt为后缀的文件名进行『搜索』\n"
                           "4. 左侧的目录树中单击『展开文件夹』或双击『打开文件』\n"
                           "5. 选择右侧的按钮进行『格式化』, 同时您也可以『新建文件夹』和『新建文件』\n"
                           "6. 搜索时只在当前节点下搜索")
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
        self.setWindowIcon(QIcon('res/icon.jpg'))  # Adjust the path as necessary

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
        self.setWindowIcon(QIcon('res/icon.jpg'))  # Adjust the path as necessary

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

    from PyQt5.QtWidgets import QMessageBox
    from datetime import datetime

    def save_content(self):
        content = self.textEdit.toPlainText()
        fcb = self.main_form.category.search(self.main_form.current_node, self.filename, FCB.TXTFILE).fcb
        old_size = fcb.size
        new_size = len(content)
        current_time = datetime.now().strftime("%Y-%m-%d %H:%M:%S")

        # Update file size and modification time
        fcb.size = new_size
        fcb.last_modify = current_time

        # Attempt to update the file content on the disk
        if not self.main_form.disk.file_update(fcb.start, old_size, fcb, content):
            QMessageBox.critical(self, 'Error', 'Failed to save file on disk.')
        else:
            QMessageBox.information(self, 'Success', 'File saved successfully.')

        # Update the modification time for all parent nodes
        node = self.main_form.category.search(self.main_form.current_node, self.filename, FCB.TXTFILE)
        while node.parent:
            node.parent.fcb.last_modify = current_time
            node = node.parent

        self.main_form.update_disk_info()
        # Assume a method to update the UI to reflect changes
        self.main_form.display_file_folder_info(fcb.file_name, fcb.last_modify, fcb.file_type, fcb.size)
        #self.main_form.file_form_init(self.main_form.current_node)  # Refresh the view to show updated times


class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        self.resize(1600, 650)
        self.setWindowTitle("FileManageSystem")
        self.setObjectName("window")
        self.setWindowIcon(QIcon('res/icon.jpg'))  # Adjust the path as necessary

        root = FCB("root", FCB.FOLDER, "", 0)
        self.root_node = Category.Node(root)
        self.current_node = self.root_node
        self.category = Category(self.root_node)
        self.disk = VirtualDisk(200, 2)
        self.current_path = "root"

        self.init_ui()
        #恢复文件系统
        self.read_bit_map()
        self.read_my_disk()
        self.read_category()

        self.setup_tree()
        self.print_tree(self.current_node,4)
        self.update_disk_info()

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
        main_layout.addWidget(self.tree, 1)

        # 中间布局 - 文件显示和路径
        middle_layout = QVBoxLayout()
        path_layout = QHBoxLayout()  # 水平布局用于路径显示和按钮
        self.path_label = QLabel("Current Path: ")
        self.path_display = QLineEdit()  # 只读文本框显示当前路径
        self.path_display.setText(self.current_path)
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
        self.file_table.setContextMenuPolicy(Qt.CustomContextMenu)
        self.file_table.customContextMenuRequested.connect(self.show_context_menu)
        self.file_table.cellClicked.connect(self.cell_clicked)

        middle_layout.addLayout(path_layout)  # 将路径布局加入到中间布局
        middle_layout.addWidget(self.file_table)
        main_layout.addLayout(middle_layout, 2)

        # Right layout - Operation buttons, disk information, and sorting options
        right_layout = QVBoxLayout()

        # Adding buttons for disk operations
        self.format_button = QPushButton("Format Disk")
        self.new_folder_button = QPushButton("New Folder")
        self.new_file_button = QPushButton("New File")
        self.format_button.clicked.connect(self.format)
        self.new_folder_button.clicked.connect(self.new_folder_button_clicked)
        self.new_file_button.clicked.connect(self.new_file_button_clicked)

        self.disk_info_label = QLabel(
            f"Disk Size: {self.disk.size} B\nBlock Size: {self.disk.block_size} B\nRemaining Space: {self.disk.remain} blocks")

        # Add widgets to the layout
        right_layout.addWidget(self.format_button)
        right_layout.addWidget(self.new_folder_button)
        right_layout.addWidget(self.new_file_button)
        right_layout.addWidget(self.disk_info_label)

        # Sorting controls
        sort_layout = QHBoxLayout()
        self.sort_label = QLabel("Sort by:")
        self.sort_combo = QComboBox()
        self.sort_combo.addItems(["Name", "Modification Date", "Type"])  # Options for sorting
        self.sort_button = QPushButton("Apply Sort")
        self.sort_button.clicked.connect(self.apply_sort)  # Connect to a method that handles sorting

        sort_layout.addWidget(self.sort_label)
        sort_layout.addWidget(self.sort_combo)
        sort_layout.addWidget(self.sort_button)

        # Add the sorting controls to the right layout
        right_layout.addLayout(sort_layout)

        # Search layout at the top
        search_layout = QHBoxLayout()
        self.search_input = QLineEdit()
        self.search_type = QComboBox()
        self.search_type.addItems(["Folder", "File"])
        self.search_button = QPushButton("Search")
        self.search_button.clicked.connect(self.perform_search)

        search_layout.addWidget(self.search_input)
        search_layout.addWidget(self.search_type)
        search_layout.addWidget(self.search_button)
        right_layout.insertLayout(0, search_layout)

        image_label = QLabel()
        image_pixmap = QPixmap("res/cat.jpg")
        # Calculate 30% of the original size
        scaled_pixmap = image_pixmap.scaled(int(image_pixmap.width() * 0.4), int(image_pixmap.height() * 0.4),
                                            Qt.KeepAspectRatio, Qt.SmoothTransformation)
        # Set the scaled pixmap to the label
        image_label.setPixmap(scaled_pixmap)

        # Adding the image to the layout
        right_layout.addWidget(image_label)
        # Ensure the layout stretches to fill the space
        right_layout.addStretch()
        main_layout.addLayout(right_layout, 1)

    def closeEvent(self, event):
        self.update_log()  # 在窗口关闭前更新日志
        event.accept()  # 接受关闭事件，确保窗口会被关闭


    def update_disk_info(self):
        # Assuming `disk_info_label` is already added to the layout of MainWindow
        self.disk_info_label.setText(
            f"Disk Size: {self.disk.size} B\n"
            f"Block Size: {self.disk.block_size} B\n"
            f"Remaining Space: {self.disk.remain} blocks"
        )

    def setup_tree(self):
        # 清除现有的树结构
        self.tree.clear()
        # 创建一个递归函数来填充树视图
        def add_items(parent_item, node):
            # 根据当前节点的信息创建一个新的树项目
            if node.fcb.file_type == FCB.FOLDER:
                item = QTreeWidgetItem(parent_item, [node.fcb.file_name + " (Folder)"])
            else:
                item = QTreeWidgetItem(parent_item, [node.fcb.file_name + " (File)"])

            # 递归地为每个子节点添加树项目
            for child in node.children:
                add_items(item, child)

        # 检查根节点是否存在
        if self.category.root is not None:
            # 创建根节点对应的树项目
            root_item = QTreeWidgetItem(self.tree, [self.category.root.fcb.file_name + " (Root)"])
            self.tree.addTopLevelItem(root_item)

            # 为根节点的每个子节点添加树项目
            for child in self.category.root.children:
                add_items(root_item, child)

            # 展开根节点，以便默认显示所有子节点
            root_item.setExpanded(True)
        else:
            print("No root node is defined in the category.")

    def display_file_folder_info(self, name, modified_time, f_type, size):
        icon_path = "res/folder.jpg" if f_type == FCB.FOLDER else "res/file.png"
        icon = QIcon(icon_path)
        pixmap = icon.pixmap(24, 24)
        file_type_label = "Folder" if f_type == FCB.FOLDER else "File"

        found = False
        for row in range(self.file_table.rowCount()):
            row_name = self.file_table.item(row, 1).text()
            row_type = self.file_table.item(row, 3).text()
            if row_name == name and row_type == file_type_label:
                found = True
                self.file_table.cellWidget(row, 0).setPixmap(pixmap)
                self.file_table.item(row, 2).setText(modified_time)
                if f_type == FCB.TXTFILE:
                    self.file_table.item(row, 4).setText(str(size))
                print(f"Updated: {name}, Time: {modified_time}, Row: {row}")
                break

        if not found:
            row_position = self.file_table.rowCount()
            self.file_table.insertRow(row_position)
            icon_label = QLabel()
            icon_label.setPixmap(pixmap)
            self.file_table.setCellWidget(row_position, 0, icon_label)
            self.file_table.setItem(row_position, 1, QTableWidgetItem(name))
            self.file_table.setItem(row_position, 2, QTableWidgetItem(modified_time))
            self.file_table.setItem(row_position, 3, QTableWidgetItem(file_type_label))
            if f_type == FCB.TXTFILE:
                self.file_table.setItem(row_position, 4, QTableWidgetItem(str(size)))
            print(f"Added: {name}, Time: {modified_time}")

        self.file_table.viewport().update()

    def format(self):
        # Pop-up confirmation dialog
        result = QMessageBox.question(self, "Confirm Disk Format",
                                      "Are you sure you want to format the disk? All data will be lost.",
                                      QMessageBox.Yes | QMessageBox.No | QMessageBox.Cancel, QMessageBox.Yes)

        if result == QMessageBox.Yes:
            # Free only the children of the root node
            if self.category.root and hasattr(self.category.root, 'children'):
                while self.category.root.children:
                    child = self.category.root.children[0]
                    self.category.free_category(child)  # Free each child

            # Clear the disk contents
            for i in range(self.disk.block_num):
                self.disk.memory[i] = ""
                self.disk.bit_map[i] = self.disk.EMPTY
            self.disk.remain = self.disk.block_num

            # Update the user interface
            self.file_table.clearContents()  # Clear the file table contents
            self.file_table.setRowCount(0)
            # Clear the directory tree and rebuild it with only the root node
            self.tree.clear()  # Clear the directory tree
            if self.category.root:
                root_item = QTreeWidgetItem(self.tree, [self.category.root.fcb.file_name + " (Root)"])
                self.tree.addTopLevelItem(root_item)  # Re-add the root node to the tree

            self.path_display.clear()  # Clear the path display

            # Update disk remaining space display (if displayed)
            self.update_disk_info()
            # Update log file or other state displays
            self.update_log()

            QMessageBox.information(self, "Disk Formatted",
                                    "The disk content and structure have been completely cleared, except for the root node.")

    def new_file_button_clicked(self):
        dlg = CreateDialog(self, "file")
        dlg.exec_()

    def new_folder_button_clicked(self):
        dlg = CreateDialog(self, "folder")
        dlg.exec_()

    def cell_clicked(self, row, column):
        if column == 1 and self.file_table.item(row,3).text() == "File":  # Assuming file names are in the second column
            filename = self.file_table.item(row, column).text()
            self.open_editor(filename)
        if column == 1 and self.file_table.item(row,3).text() == "Folder":
            filename = self.file_table.item(row, column).text()
            self.open_folder(filename)
    def show_context_menu(self, position):
        menu = QMenu()
        index = self.file_table.indexAt(position)
        if index.isValid() and index.column() == 1:  # 确保在第二列点击
            item_type = FCB.TXTFILE if self.file_table.item(index.row(), 3).text() == "File" else FCB.FOLDER
            filename = self.file_table.item(index.row(), 1).text()
            open_action = QAction("Open", self)
            open_action.triggered.connect(lambda: self.open_editor(filename) if item_type == "File" else self.open_folder(filename))
            menu.addAction(open_action)
            delete_action = QAction("Delete", self)
            delete_action.triggered.connect(lambda: self.delete(filename,item_type,self.current_node))
            menu.addAction(delete_action)
        else:
            # 全局菜单项
            newFileAct = QAction('New File', self)
            newFolderAct = QAction('New Folder', self)
            newFileAct.triggered.connect(self.new_file_button_clicked)
            newFolderAct.triggered.connect(self.new_folder_button_clicked)
            menu.addAction(newFileAct)
            menu.addAction(newFolderAct)
        menu.exec_(self.file_table.viewport().mapToGlobal(position))

    def open_folder(self, filename):
        print(f"Opening folder: {filename}")
        node = self.category.search(self.current_node, filename, FCB.FOLDER)
        print(f"Current Node:{self.current_node.fcb.file_name}")
        for child in self.current_node.children:
            print(f"child of current node:{child.fcb.file_name}")
        if node:
            self.current_node = node
            # 更新当前路径，手动构建路径字符串
            self.current_path = self.current_path + '/' + filename if self.current_path else filename
            self.path_display.setText(self.current_path)  # 更新地址栏显示的路径
            self.file_form_init(self.current_node)  # 重新加载文件表格以显示新文件夹的内容
        else:
            print("Folder not found!")

    def open_editor(self, filename):
        editor = NoteForm(filename, self)
        editor.exec_()

    def file_form_init(self, node):
        self.file_table.setRowCount(0)
        if hasattr(node, 'children') and node.children:
            for child in node.children:
                # Prepare data for each child node
                file_name = child.fcb.file_name
                modified_time = child.fcb.last_modify  # Assuming the date is properly formatted
                f_type = child.fcb.file_type
                size = child.fcb.size

                self.display_file_folder_info(file_name, modified_time, f_type, size)

    def delete(self, filename, item_type, current_node):
        node = self.category.search_in_current_directory(current_node, filename, item_type)
        if node is None:
            print(f"未找到指定的{'文件' if item_type == FCB.TXTFILE else '文件夹'}: {filename}")
            return

        if item_type == FCB.TXTFILE:
            # 删除文件内容
            self.disk.delete_file_content(node.fcb.start, node.fcb.size)
            # 从目录结构中删除文件节点
            if node.parent:
                node.parent.delete_child(node)
        else:
            # 递归删除所有子节点
            while node.children:  # 使用 while 循环确保所有子节点都被处理
                child = node.children[0]  # 总是取第一个子节点
                self.delete(child.fcb.file_name, child.fcb.file_type, node)  # 递归删除

            # 删除当前节点
            if node.parent:
                node.parent.delete_child(node)

        # 只在最顶层调用更新视图，避免重复更新
        if current_node == self.current_node:
            self.file_form_init(self.current_node)
            self.setup_tree()
            self.update_disk_info()

    def create_folder(self, name):
        print(f"Creating folder named: {name}")
        if not self.category.check_same_name(self.current_node,name, FCB.FOLDER):
            # If name is duplicated, show a warning message
            QMessageBox.warning(self, "Warning", f"A folder named {name} already exists.")
        else:
            current_time = datetime.now()
            formatted_time = current_time.strftime("%Y-%m-%d %H:%M:%S")

            self.display_file_folder_info(name,formatted_time,FCB.FOLDER,0)
            self.category.create_file(self.current_node,FCB(name,FCB.FOLDER,formatted_time,0))
        self.setup_tree()


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
            self.category.create_file(self.current_node, FCB(name, FCB.TXTFILE, formatted_time, 0))
            print(f"File {name} created successfully.")
        self.setup_tree()

    def go_up(self):
        # 检查当前节点是否有父节点
        if self.current_node.parent is not None:
            # 更新当前节点到父节点
            self.current_node = self.current_node.parent
            # 重新加载文件表格以显示父文件夹的内容
            self.file_form_init(self.current_node)
            # 更新当前路径为父路径
            # 将路径切割成部分，然后移除最后一个部分，即当前文件夹的名字
            self.current_path = '/'.join(self.current_path.split('/')[:-1])
            # 更新地址栏显示的路径
            self.path_display.setText(self.current_path)
            print("Moved up to:", self.current_path)
        else:
            print("Already at the root directory, cannot move up.")

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
                # 首先读取磁盘的剩余容量信息
                remain_line = reader.readline().strip()
                if remain_line.startswith("Remaining Blocks:"):
                    self.disk.remain = int(remain_line.split(":")[1].strip())

                for i in range(self.disk.block_num):
                    line = reader.readline().strip()
                    if not line:  # 保护代码，避免读取空行
                        continue
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
            # 首先写入磁盘的剩余容量
            writer.write(f"Remaining Blocks: {self.disk.remain}\n")
            for data in self.disk.memory:
                # Encode the line
                encoded_data = data.replace("(", "(((")  # Encode left parentheses
                encoded_data = encoded_data.replace(")", ")))")  # Encode right parentheses
                encoded_data = encoded_data.replace("\r\n", "|||")  # Encode newlines
                writer.write(encoded_data + '\n')

    def read_category(self):
        with open("CategoryInfo.txt", 'r') as file:
            lines = file.readlines()
            root_node = None
            parent_stack = []
            current_node_info = {}

            for line in lines:
                line = line.strip()
                if "Node Start" in line:
                    current_node_info = {}
                elif "Node End" in line:
                    fcb = FCB(current_node_info['File Name'],
                              int(current_node_info['File Type']),
                              current_node_info['Last Modified'],
                              int(current_node_info['File Size']),
                              int(current_node_info['Start Position']))
                    new_node = Category.Node(fcb)
                    if parent_stack:
                        parent_stack[-1].add_child(new_node)
                    else:
                        root_node = new_node  # 标记根节点
                    parent_stack.append(new_node)  # 添加当前节点到栈，用作后续子节点的父节点
                elif "Parent End" in line and parent_stack:
                    parent_stack.pop()  # 当一个节点的所有子节点都被处理完毕，从栈中移除该节点
                else:
                    if line:
                        parts = line.split(": ", 1)
                        if len(parts) == 2:
                            key, value = parts
                            current_node_info[key.strip()] = value.strip()

        if not root_node:
            default_fcb =  FCB("root", FCB.FOLDER, "", 0)
            root_node = Category.Node(default_fcb)

        self.category.root = root_node
        self.root_node = root_node
        self.current_node = root_node
        self.file_form_init(self.category.root)

    def write_category(self):
        with open("CategoryInfo.txt", 'w') as file:
            def write_node(node, parent_name=""):
                file.write("Node Start\n")
                file.write(f"Parent Name: {parent_name}\n")
                file.write(f"File Name: {node.fcb.file_name}\n")
                file.write(f"File Type: {node.fcb.file_type}\n")
                file.write(f"Last Modified: {node.fcb.last_modify}\n")
                file.write(f"File Size: {node.fcb.size}\n")
                file.write(f"Start Position: {node.fcb.start}\n")
                file.write("Node End\n")
                for child in node.children:
                    write_node(child, node.fcb.file_name)
                file.write("Parent End\n")  # 标记父节点的结束

            if self.category.root:
                write_node(self.category.root)

    def print_tree(self,node, indent=0):
        if node is None:
            return
        print(' ' * indent + f"Name: {node.fcb.file_name}, Type: {node.fcb.file_type}, Size: {node.fcb.size}")
        for child in node.children:
            self.print_tree(child, indent + 4)

    # 使用这个函数来打印

    def update_log(self):
        self.write_bit_map()
        self.write_my_disk()
        self.write_category()

    def apply_sort(self):
        sort_option = self.sort_combo.currentText()
        # Collect all data from the table
        data = []
        for row in range(self.file_table.rowCount()):
            file_name = self.file_table.item(row, 1).text()
            modified_time = self.file_table.item(row, 2).text()
            item_type = self.file_table.item(row, 3).text()
            file_type = FCB.TXTFILE if item_type == "File" else FCB.FOLDER
            size = self.file_table.item(row, 4).text() if self.file_table.item(row, 4) else 0
            data.append((file_name, modified_time, file_type, size))

        # Determine the sorting key based on the sort option
        if sort_option == "Name":
            data.sort(key=lambda x: x[0])  # Sort by file name
        elif sort_option == "Modification Date":
            data.sort(key=lambda x: x[1])  # Sort by modification date
        elif sort_option == "Type":
            data.sort(key=lambda x: x[2])  # Sort by file type

        # Clear the table and repopulate it with sorted data
        self.file_table.setRowCount(0)
        for file_name, modified_time, file_type, size in data:
            self.display_file_folder_info(file_name, modified_time, file_type, size)

    def perform_search(self):
        # Get the search text and selected type from the user interface
        search_text = self.search_input.text()
        search_type = self.search_type.currentText()

        # Determine the file type based on the dropdown selection
        file_type = FCB.FOLDER if search_type == "Folder" else FCB.TXTFILE

        # Perform the search starting from the current node
        if self.current_node.fcb.file_name == search_text and self.current_node.fcb.file_type == file_type :
            result_node = self.current_node
        else:
            result_node = self.category.search(self.current_node, search_text, file_type)

        if result_node:
            if result_node.parent:
                self.file_table.setRowCount(0)
                self.current_node = result_node.parent
                self.current_path = self.build_path(self.current_node)  # Assuming a function to build the path
                self.path_display.setText(self.current_path)  # Update the path display
                self.file_form_init(self.current_node)
                found_row = None
                for row in range(self.file_table.rowCount()):
                    item = self.file_table.item(row, 1)  # Assuming file names are in the second column
                    item_type = self.file_table.item(row, 3).text()  # Assuming file types are in the fourth column

                    if item and item.text() == search_text and item_type == search_type:
                        found_row = row
                        break

                if found_row is not None:
                    # Highlight the row where the item was found
                    self.highlight_row(found_row, "lightpink")
            else:
                QMessageBox.information(self,"Search Results","The search node is the root node!")

        else:
            # If no result, show a message in the table or status bar
            QMessageBox.information(self, "Search Results", "No matching files or folders found.")

    def highlight_row(self, row, color):
        """ Apply background color to the entire row in the table. """
        for column in range(self.file_table.columnCount()):
            item = self.file_table.item(row, column)
            if not item:  # Create the item if it does not exist
                item = QTableWidgetItem()
                self.file_table.setItem(row, column, item)
            item.setBackground(QtGui.QColor(color))

    def build_path(self, node):
        """ Recursively build the file path from the current node to the root. """
        path = []
        while node:
            path.append(node.fcb.file_name)
            node = node.parent
        return '/'.join(reversed(path))


if __name__ == '__main__':
    app = QApplication(sys.argv)
    window = MainWindow()
    window.show()
    help_dialog = HelpDialog()  # 创建帮助对话框
    help_dialog.show()  # 显示帮助对话框
    sys.exit(app.exec_())