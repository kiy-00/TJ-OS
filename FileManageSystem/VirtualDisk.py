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
        blocks = []
        index = 0
        while index < len(content):
            # 特别处理换行符，保证'\r\n'不被拆分
            if content[index:index + 2] == '\r\n' and self.block_size == 2 and index + 2 <= len(content):
                blocks.append(content[index:index + 2])
                index += 2
            elif index + self.block_size <= len(content):
                blocks.append(content[index:index + self.block_size])
                index += self.block_size
            else:
                # 添加最后一个可能的小于block_size的块
                blocks.append(content[index:])
                break

        if not blocks:  # 如果blocks为空（content为空或其他情况）
            return True  # 无内容可存储，可以直接返回False或进行其他逻辑处理

        if len(blocks) <= self.remain:
            # 找到文件开始存放的位置
            start = -1
            for i in range(self.block_num):
                if self.bit_map[i] == self.EMPTY:
                    self.remain -= 1
                    start = i
                    fcb.start = i
                    self.memory[i] = blocks[0]
                    break

            if start == -1:  # 如果没有找到空间
                return False

            # 从该位置往后开始存放内容
            j = 1
            i = start + 1
            while j < len(blocks) and i < self.block_num:
                if self.bit_map[i] == self.EMPTY:
                    self.remain -= 1
                    self.bit_map[start] = i  # 以链接的方式存储每位数据
                    start = i
                    self.memory[i] = blocks[j]
                    j += 1  # 处理下一个块
                i += 1

            if j == len(blocks):
                self.bit_map[start] = self.END  # 标记文件尾

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

    def delete_file_content(self, start, size):
        if start == self.EMPTY or start >= self.block_num:
            return  # If start position is invalid or file is empty, return immediately

        blocks = self.get_block_size(size)

        count = 0
        i = start
        while i < self.block_num and count < blocks:
            next_index = self.bit_map[i]  # Get next index before clearing
            self.memory[i] = ""
            self.bit_map[i] = self.EMPTY
            self.remain += 1

            if next_index == self.END:
                break  # If this was the last block, exit the loop

            i = next_index
            count += 1

    def file_update(self,old_start,old_size,new_fcb,new_content):
        self.delete_file_content(old_start,old_size)
        return self.give_space(new_fcb,new_content)