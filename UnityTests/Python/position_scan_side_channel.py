from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.side_channel import (
    SideChannel,
    IncomingMessage,
    OutgoingMessage,
)
from typing import List
import numpy as np
import struct

import uuid

class PositionScanChannel(SideChannel):
    """
    This is the SideChannel for retrieving attribution data from Unity.
    You can send requests to Unity using send_request.
    """

    def __init__(self) -> None:
        channel_id = uuid.UUID("a599964d-d747-4696-9a2d-d14cca2fa2e5")
        super().__init__(channel_id)

    def on_message_received(self, msg: IncomingMessage) -> None:
        print(msg.read_string())
        
        
    def send_request(self, key: str, value: List[float]) -> None:
        """
        Sends a request to Unity
        The arguments for a positionScan are ("positionScan", [X_POS, Y_POS, Z_POS, Optional-RANGE])
        """
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
