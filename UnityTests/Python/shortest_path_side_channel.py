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

class ShortestPathSideChannel(SideChannel):
    """
    This is the SideChannel for requesting the shortest path as a list of floats
    """
    resolution = []

    def __init__(self) -> None:
        channel_id = uuid.UUID("dc4b7d9a-774e-49bc-b73e-4a221070d716")
        super().__init__(channel_id)

    def on_message_received(self, msg: IncomingMessage) -> None:
        """
        IncomingMessage is a list of floats
        Reshaped to n x 3 array (array of 3D points)
        """
        path = msg.read_float32_list()
        path_length = int(len(path)/3)
        
        print("Shortest path:")
        print(np.reshape(path, (path_length,3)))
        
    def send_request(self) -> None:
        """
        Sends a request to Unity for the shortest path points
        """
        msg = OutgoingMessage()
        super().queue_message_to_send(msg)

    def build_immediate_request(self) -> bytearray:
        msg = OutgoingMessage()

        result = bytearray()
        result += self.channel_id.bytes_le
        result += struct.pack("<i", len(msg.buffer))
        result += msg.buffer
        return result
