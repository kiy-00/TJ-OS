from FCB import FCB

class Category:
    class Node:
        def __init__(self, fcb=None, name=None, file_type=None):
            if fcb:
                self.fcb = fcb
            else:
                self.fcb = FCB(name=name, file_type=file_type)
            self.children = []  # 存储所有子节点的列表
            self.parent = None  # 父节点

        def add_child(self, child_node):
            self.children.append(child_node)
            child_node.parent = self

        def delete_child(self, child_node):
            # Remove a specific child node from children list
            if child_node in self.children:
                self.children.remove(child_node)
                child_node.parent = None

    def __init__(self, root_node=None):
        self.root = root_node

    def free_category(self, p_node):
        if p_node is None:
            return
        for child in p_node.children[:]:  # Use slicing to prevent modification issues
            self.free_category(child)
        p_node.children = []
        if p_node.parent:
            p_node.parent.delete_child(p_node)
        del p_node

    def search(self, p_node, file_name, file_type):
        if p_node is None:
            return None
        if p_node.fcb.file_name == file_name and p_node.fcb.file_type == file_type:
            return p_node
        for child in p_node.children:
            result = self.search(child, file_name, file_type)
            if result is not None:
                return result
        return None

    def create_file(self, parent_file_name, fcb):
        if self.root is None:
            return
        parent_node = self.search(self.root, parent_file_name, FCB.FOLDER)
        if parent_node is None:
            return
        new_node = self.Node(fcb=fcb)
        parent_node.add_child(new_node)

    def delete_folder(self, folder_name):
        # Locate the node that matches the file name and type
        current_node = self.search(self.root, folder_name, FCB.FOLDER)
        if current_node is None:
            print("未找到文件夹")
            return
        if current_node.parent is not None:
            current_node.parent.delete_child(current_node)
            self.free_category(current_node)
        else:
            print("当前不能删除该文件夹")

    def delete_file(self, file_name):
        current_node = self.search(self.root, file_name, FCB.TXTFILE)
        if current_node is None:
            print("未找到文本文件")
            return
        if current_node.parent is not None:
            current_node.parent.delete_child(current_node)
            current_node = None
        else:
            print("当前不能删除该文本文件")

    def check_same_name(self, p_node, name, file_type):
        if p_node is None:
            return True
        # 只检查给定节点（即父节点）的直接子节点
        for child in p_node.children:
            if child.fcb.file_name == name and child.fcb.file_type == file_type:
                return False  # 找到一个同名同类型的直接子节点，返回 False
        return True  # 在同级目录中没有找到同名同类型的文件，返回 True

