import os

class FCB:
    TXTFILE = 0  # 文本文件标识
    FOLDER = 1   # 文件夹标识

    def __init__(self, name, file_type, last_modify, size, start=None):
        self.file_name = name
        self.file_type = file_type
        self.last_modify = last_modify
        self.size = size
        self.start = start if start is not None else -1  # 如果未指定起始位置，则设为默认值 -1