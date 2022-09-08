import time
import argparse
import subprocess
from pathlib import Path
from PIL import Image
from mlagents_envs.environment import UnityEnvironment
from gym import spaces
from gym_unity.envs import UnityToGymWrapper
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel
from mlagents_envs.side_channel.environment_parameters_channel import EnvironmentParametersChannel
from mlagents_envs.side_channel.float_properties_channel import FloatPropertiesChannel

def main(args : argparse.Namespace):
    
    # seeds to test: 1, 5, 10
    unitySeed = 1
    resets = 10
    steps = 10
    terrain_tile_load_distance = args.tile_range[0]

    #Rewards
    GOAL_COLLISION_REWARD = 1
    EXP_COLLISION_REWARD = .8
    OTHER_COLLISION_REWARD = -0.9
    FALL_OFF_MAP_REWARD = -0.1
    STEP_REWARD = -0.08

    engine_side_channel = EngineConfigurationChannel()
    environment_side_channel = EnvironmentParametersChannel()
    float_properties_side_channel = FloatPropertiesChannel()

    #Connect to Unity Editor environment
    unityEnvironmentStr = None
    unityEnvironmentStr = "../../../ARORA_2.10.18_headless/ARORA.x86_64"
    #unityEnvironmentStr = "../../../unity-envs/ARORA_2.10.16/ARORA"
    unity_env = UnityEnvironment(
        file_name = unityEnvironmentStr, 
        seed = unitySeed, timeout_wait=1200, log_folder="/data/work/choayun/navsim-test-1/exp",
        side_channels = [engine_side_channel, environment_side_channel, float_properties_side_channel],
        additional_args = [
            "-observationMode", "1", 
            "-terrainLoadDistance", str(terrain_tile_load_distance)
            ]
        )
    
    print('connected to ',unityEnvironmentStr)
    
    #Engine Side Channels
    engine_side_channel.set_configuration_parameters(time_scale=1, quality_level = 0)

    #Rewards
    environment_side_channel.set_float_parameter("rewardForGoalCollision", GOAL_COLLISION_REWARD)
    environment_side_channel.set_float_parameter("rewardForExplorationPointCollision", EXP_COLLISION_REWARD)
    environment_side_channel.set_float_parameter("rewardForOtherCollision", OTHER_COLLISION_REWARD)
    environment_side_channel.set_float_parameter("rewardForFallingOffMap", FALL_OFF_MAP_REWARD)
    environment_side_channel.set_float_parameter("rewardForEachStep", STEP_REWARD)

    environment_side_channel.set_float_parameter("selectedTaskIndex", 0) # pointnav


    #Create Gym Environment
    gym_env = UnityToGymWrapper(unity_env, False, False, True)
    action = [ 0.5, 1, -1]
    lines = []
    top_output = []
    nvidia_output = []

    for t in range(resets):
        startTime = int(time.time_ns()/1000000)
        observation = gym_env.reset()
        line = "Load time," + str(t) + "," + str(int(time.time_ns()/1000000)-startTime)
        print(line)
        lines.append(line)
        top_output.append(subprocess.getoutput("top -b -n1"))
        nvidia_output.append(subprocess.getoutput("nvidia-smi"))
        for s in range(steps):
            startTime = int(time.time_ns()/1000000)
            observation, reward, done, info = gym_env.step(action)
            print(observation[0])
            if done:
                break
            line = "Step time," + str(s) + "," + str(int(time.time_ns()/1000000)-startTime)
            print(str(t) + " " + line)
            lines.append(line)
            top_output.append(subprocess.getoutput("top -b -n1"))
            #nvidia_output.append(subprocess.getoutput("nvidia-smi"))
    
    gym_env.close()
    
    with open("loadTests/times_" + str(terrain_tile_load_distance) + ".csv", "w") as logfile:
        logfile.write("Type,Index,Time(ms)\n")
        for s in lines:
            logfile.write(s+"\n") 
    with open("loadTests/top_output" + str(terrain_tile_load_distance) + ".txt", "w") as logfile:
        for s in top_output:
            logfile.write(s+"\n") 
            """
    with open("loadTests/nvidia_output" + str(terrain_tile_load_distance) + ".txt", "w") as logfile:
        for s in nvidia-smi:
            logfile.write(s+"\n") 
    """
if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('tile_range', nargs ='+', action = 'store')
    args = parser.parse_args()

    main(args)
