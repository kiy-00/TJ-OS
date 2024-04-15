class VirtualDisk:
    EMPTY = -1  # 假设 EMPTY 常量代表未使用的块
    END = -2
    def __init__(self, size, block_size):
        self.size = size
        self.block_size = block_size
        self.block_num = size // block_size
        self.remain = self.block_num

        self.memory = [""] * self.block_num  # 初始化内存数组为空字符串
        self.bit_map = [self.EMPTY] * self.block_num  # 初始化位图为全部可用

        # Python 中不需要显式初始化每个元素，上面已经使用列表推导完成了初始化

    def get_block_size(self, size):
        return size // self.block_size + (1 if size % self.block_size != 0 else 0)

    def give_space(self, fcb, content):
        blocks = self.get_block_size(fcb.size)

        if blocks <= self.remain:
            # 找到文件开始存放的位置
            start = 0
            for i in range(self.block_num):
                if self.bit_map[i] == self.EMPTY:
                    self.remain -= 1
                    start = i
                    fcb.start = i
                    self.memory[i] = content[:self.block_size]
                    break

            # 从该位置往后开始存放内容
            j = 1
            i = start + 1
            while j < blocks and i < self.block_num:
                if self.bit_map[i] == self.EMPTY:
                    self.remain -= 1

                    self.bit_map[start] = i  # 以链接的方式存储每位数据
                    start = i

                    if blocks != 1:
                        if j != blocks - 1:
                            self.memory[i] = content[j * self.block_size:(j + 1) * self.block_size]
                        else:
                            self.bit_map[i] = self.END  # 文件尾
                            if fcb.size % self.block_size != 0:
                                self.memory[i] = content[j * self.block_size:]
                            else:
                                self.memory[i] = content[j * self.block_size:(j + 1) * self.block_size]
                    else:
                        self.memory[i] = content

                    j += 1  # 找到一个位置
                i += 1
            return True
        else:
            return False

    def get_file_content(self,fcb):
        if fcb.start == self.EMPTY:
            return ""
        else:
            content = ""
            start = fcb.start
            blocks = self.get_block_size(fcb.size)

            count = 0
            i=start
            while i<self.block_num and count < blocks:
                content += self.memory[i]
                i=self.bit_map[i]
                count+=1

        return content

    def delete_file_content(self,start,size):

        blocks = self.get_block_size(size)

        count = 0
        i = start
        while i < self.block_num and count < blocks:
            self.memory[i] = ""
            self.remain += 1
            next = self.bit_map[i]
            self.bit_map[i] = self.EMPTY
            i = next
            count += 1


    def file_update(self,old_start,old_size,new_fcb,new_content):
        self.delete_file_content(old_start,old_size)
        return self.give_space(new_fcb,new_content)