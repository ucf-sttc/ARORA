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

class NavigableSideChannel(SideChannel):
    """
    This is the SideChannel for requesting or checking a navigable point in Unity.
    You can send requests to Unity using send_request.
    """
    resolution = []

    def __init__(self) -> None:
        channel_id = uuid.UUID("fbae7da3-76e8-4c37-86c9-ad647c74fd69")
        super().__init__(channel_id)

    def on_message_received(self, msg: IncomingMessage) -> None:
        """
        IncomingMessage is a list of floats
        IncomingMessage is empty if there was no point that satisfied the request,
        otherwise it will contain the requested navigable point in Unity's world space
        """
        print("Navigable side channel message received")

        point = msg.read_float32_list()
        
        if point == []:
            print('No navigable point!')
        else:
            print(point)
        print('~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~')
        
        
    def send_request(self, key: str, value: List[float]) -> None:
        """
        Sends a request to Unity
        The arguments for the request are ("navigable", [POINT]), where POINT can be one of the following:
            1. []        - requests random navigable point
            2. [x, z]    - check if there is a navigable point (x, y, z) at any height y
            3. [x, y, z] - check if (x, y, z) is a navigable point
        """
        msg = OutgoingMessage()
        msg.write_string(key)
        msg.write_float32_list(value)
        super().queue_message_to_send(msg)

    def build_immediate_request(self, key: str, value: List[float]) -> bytearray:
        msg = OutgoingMessage()
        msg.write_string(key)
        msg.write_float32_list(value)

        result = bytearray()
        result += self.channel_id.bytes_le
        result += struct.pack("<i", len(msg.buffer))
        result += msg.buffer
        return result
