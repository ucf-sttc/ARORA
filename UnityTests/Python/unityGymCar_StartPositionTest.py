import time
import argparse
from pathlib import Path
from PIL import Image
from mlagents_envs.environment import UnityEnvironment
from gym import spaces
from gym_unity.envs import UnityToGymWrapper
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel
from mlagents_envs.side_channel.environment_parameters_channel import EnvironmentParametersChannel
from mlagents_envs.side_channel.float_properties_channel import FloatPropertiesChannel
from map_side_channel import MapSideChannel
from position_scan_side_channel import PositionScanChannel

def main(args : argparse.Namespace):
    
    # seeds to test: 1, 5, 10
    unitySeed = 1
    episodes = 30

    #Rewards
    GOAL_COLLISION_REWARD = 1
    EXP_COLLISION_REWARD = .8
    OTHER_COLLISION_REWARD = -0.9
    FALL_OFF_MAP_REWARD = -0.1
    STEP_REWARD = -0.08

    engine_side_channel = EngineConfigurationChannel()
    environment_side_channel = EnvironmentParametersChannel()
    map_side_channel = MapSideChannel()
    position_scan_side_channel = PositionScanChannel()
    float_properties_side_channel = FloatPropertiesChannel()

    #Connect to Unity Editor environment
    unityEnvironmentStr = None
    #Connect to specified binary environment
    #unityEnvironmentStr = "../envs/Berlin_v2/Berlin_Walk_v2.exe" 
    #unityEnvironmentStr = "C:/workspace/v2_mlagents18/Berlin_Walk_v2.exe"
    unity_env = UnityEnvironment(file_name = unityEnvironmentStr, seed = unitySeed, timeout_wait=1000, side_channels =[engine_side_channel, environment_side_channel, position_scan_side_channel, map_side_channel, float_properties_side_channel])
    
    print('connected to ',unityEnvironmentStr)
    
    #Engine Side Channels
    engine_side_channel.set_configuration_parameters(time_scale=1, quality_level = 0)

    #Rewards
    environment_side_channel.set_float_parameter("rewardForGoalCollision", GOAL_COLLISION_REWARD)
    environment_side_channel.set_float_parameter("rewardForExplorationPointCollision", EXP_COLLISION_REWARD)
    environment_side_channel.set_float_parameter("rewardForOtherCollision", OTHER_COLLISION_REWARD)
    environment_side_channel.set_float_parameter("rewardForFallingOffMap", FALL_OFF_MAP_REWARD)
    environment_side_channel.set_float_parameter("rewardForEachStep", STEP_REWARD)

    environment_side_channel.set_float_parameter("observationMode",0) # vector agent
    environment_side_channel.set_float_parameter("episodeLength", 1)
    environment_side_channel.set_float_parameter("selectedTaskIndex", 0) # pointnav

    #File to save step time benchmarks
    BASEFILENAME = "seed-{}-episodes-{}".format(unitySeed, episodes)
    print(BASEFILENAME)
    timestr = time.strftime("%Y%m%d-%H%M%S")
    baseFileNameWithTime = BASEFILENAME + "-" + timestr 
    csvFilename = baseFileNameWithTime + ".csv"

    #Create Gym Environment
    gym_env = UnityToGymWrapper(unity_env, False, False, True)
    observation = gym_env.reset()

    observations = []
    for t in range(episodes):
        print("Starting episode",t)
        observation, reward, done, info = gym_env.step([0,0,0])

        textline = []
        textline.append("{}".format(t))
        for i in range(13):
            textline.append(",{}".format(observation[0][i]))
        observations.append(''.join(textline))
        if done:
            gym_env.reset()

    with open(csvFilename, "w") as logfile:
        for s in observations:
            logfile.write(s+"\n") 
            
    gym_env.close()
    
    
if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    args = parser.parse_args()

    main(args)