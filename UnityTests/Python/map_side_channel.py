from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.side_channel import (
    SideChannel,
    IncomingMessage,
    OutgoingMessage,
)
from typing import List
from PIL import Image
import numpy as np
import struct

import uuid

class MapSideChannel(SideChannel):
    """
    This is the SideChannel for retrieving map data from Unity.
    You can send map requests to Unity using send_request.
    """
    resolution = []

    def __init__(self) -> None:
        channel_id = uuid.UUID("24b099f1-b184-407c-af72-f3d439950bdb")
        super().__init__(channel_id)

    def on_message_received(self, msg: IncomingMessage) -> None:
        print("Map side channel message received")
        if self.resolution is None:
            print('no resolution set')
            return

        raw_bytes = msg.get_raw_bytes()
        unpacked_array = np.unpackbits(raw_bytes)[0:self.resolution[0]*self.resolution[1]]
        
        # mode as grayscale 'L' and convert to binary '1' because for some reason using only '1' doesn't work (possible bug)
        #img = Image.frombuffer('L', (self.resolution[0],self.resolution[1]), np.array(msg.get_raw_bytes())).convert('1')
        
        # here we save image when msg.get_raw_bytes() return byte array of 0 and 1 integer values
        #img = Image.frombuffer('L', (self.resolution[0],self.resolution[1]), unpacked_array*255)
        img = Image.fromarray(np.flipud((unpacked_array*255).astype('uint8').reshape(self.resolution[1],self.resolution[0])), 'L')
        img.save("img.png")
        
        #np.savetxt("arrayfile", unpacked_array, fmt='%1d', delimiter='', newline='')
        
    def send_request(self, key: str, value: List[float]) -> None:
        """
        Sends a request to Unity
        The arguments for a mapRequest are ("binaryMap", [RESOLUTION_X, RESOLUTION_Y, THRESHOLD])
        Or ("binaryMapZoom", [ROW, COL])
        """
        if key == 'binaryMap':
            self.resolution = value
            if len(value) == 0:
                self.resolution = [3284, 2666] # full map at meter scale
        elif key == 'binaryMapZoom':
            self.resolution = [100, 100] # resolution at cm scale for 1 square meter tile
        msg = OutgoingMessage()
        msg.write_string(key)
        msg.write_float32_list(value)
        super().queue_message_to_send(msg)

    def build_immediate_request(self, key: str, value: List[float]) -> bytearray:
        self.resolution = value
        msg = OutgoingMessage()
        msg.write_string(key)
        msg.write_float32_list(value)

        result = bytearray()
        result += self.channel_id.bytes_le
        result += struct.pack("<i", len(msg.buffer))
        result += msg.buffer
        return result
        
