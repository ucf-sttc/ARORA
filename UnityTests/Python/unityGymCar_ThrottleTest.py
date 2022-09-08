import time
import argparse
import math
from pathlib import Path
from PIL import ImageCms
from PIL import Image
from mlagents_envs.environment import UnityEnvironment
from gym import spaces
from gym_unity.envs import UnityToGymWrapper
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel
from mlagents_envs.side_channel.environment_parameters_channel import EnvironmentParametersChannel
from mlagents_envs.side_channel.float_properties_channel import FloatPropertiesChannel
from map_side_channel import MapSideChannel
from position_scan_side_channel import PositionScanChannel
from set_agent_position_side_channel import SetAgentPositionSideChannel
from navigable_side_channel import NavigableSideChannel
from typing import List

from mlagents_envs.rpc_utils import behavior_spec_from_proto, steps_from_proto

timerEntries = []

def main(args : argparse.Namespace):

    print(args)
    #Debug
    saveImageFlag = True
    saveOnStepIfFactorOf = 100

    #Rewards
    GOAL_REWARD = 1
    NO_VIABLE_PATH_REWARD = -1
    STEP_REWARD_MULTIPLIER = -0.08
    COLLISION_MULTIPLIER = -0.9
    SPL_DELTA_MULTIPLIER = 0.1
    
    EXP_COLLISION_REWARD = .8
    TRAFFIC_COLLISION_REWARD = -0.2  

    engine_side_channel = EngineConfigurationChannel()
    environment_side_channel = EnvironmentParametersChannel()
    map_side_channel = MapSideChannel()
    position_scan_side_channel = PositionScanChannel()
    float_properties_side_channel = FloatPropertiesChannel()
    set_agent_position_side_channel = SetAgentPositionSideChannel()
    navigable_side_channel = NavigableSideChannel()

    #Connect to Unity Editor environment
    unityEnvironmentStr = None
    #Connect to specified binary environment
    #unityEnvironmentStr = "../envs/v2_9T/Berlin_Walk_V2"

    observationX = 256
    observationY = 256

    if(observationX > 512 or observationY > 512):
        print("Visual observation size is clamped at 512 for performance reasons")
    observationX = max(1,min(observationX, 512))
    observationY = max(1,min(observationY, 512))
    if(not(math.ceil(Log2(observationX)) == math.floor(Log2(observationX))) or not(math.ceil(Log2(observationY)) == math.floor(Log2(observationY)))):
        print("Warning: Observation sizes that are non power of 2 may cause a decrease in performance")
    
    unity_env = UnityEnvironment(
        file_name = unityEnvironmentStr, seed = 1, timeout_wait=1000,
        side_channels =[engine_side_channel, environment_side_channel, position_scan_side_channel, map_side_channel, float_properties_side_channel, navigable_side_channel, set_agent_position_side_channel],
        log_folder = "D:/Work",
        additional_args = ["-observationWidth", str(observationX), "-observationHeight", str(observationY), "-showVisualObservations"]
        #additional_args = ["-observationWidth", str(observationX), "-observationHeight", str(observationY), "-showVisualObservations", "-fastForward", "0"]
        )
    
    print('connected to ',unityEnvironmentStr)
    
    #Engine Side Channels
    engine_side_channel.set_configuration_parameters(time_scale=2, quality_level = 0)

    #Rewards
    environment_side_channel.set_float_parameter("rewardForGoal", GOAL_REWARD)
    environment_side_channel.set_float_parameter("rewardForNoViablePath", NO_VIABLE_PATH_REWARD)
    environment_side_channel.set_float_parameter("rewardStepMul", STEP_REWARD_MULTIPLIER)
    environment_side_channel.set_float_parameter("rewardCollisionMul", COLLISION_MULTIPLIER)
    environment_side_channel.set_float_parameter("rewardStepDeltaMul", SPL_DELTA_MULTIPLIER)
    
    environment_side_channel.set_float_parameter("rewardForExplorationPointCollision", EXP_COLLISION_REWARD)
    environment_side_channel.set_float_parameter("rewardForTrafficCollision", TRAFFIC_COLLISION_REWARD)
    
    #Segmentation
    #Object Segmentation 0
    #Tag Segmentation 1
    #Layer Segmentation 2
    segmentationMode = args.s
    environment_side_channel.set_float_parameter("segmentationMode", segmentationMode)

    #Agent Selection
    observationModeIndex = 2
    #Select Vector Agent 0
    if(args.a=="Vector"):
        observationModeIndex = 0
    #Select Visual Agent 1
    if(args.a=="Visual"):
        observationModeIndex = 1
    #Select Vector Visual Agent 2
    if(args.a=="VectorVisual"):
        observationModeIndex = 2
    environment_side_channel.set_float_parameter("observationMode", observationModeIndex)
    
    #Episode Length
    episodeLength = args.e
    environment_side_channel.set_float_parameter("episodeLength", episodeLength)

    #Task Selection
    selectedTaskIndex = 1
    #Point Nav Task 0
    if(args.t=="PointNav"):
        selectedTaskIndex = 0
    #Simple Object Nav Task 1
    if(args.t=="SimpleObjectNav"):
        selectedTaskIndex = 1
    #Simple Vehicle Object Nav Task with goal selection 2
    if(args.t=="ObjectNav"):
        selectedTaskIndex = 2
    environment_side_channel.set_float_parameter("selectedTaskIndex", selectedTaskIndex)
    #Goal Selection
    #0 - Tocus;
    #1 - sedan1
    #2 - Car1
    #3 - Car2
    #4 - City Bus
    #5 - Sporty_Hatchback
    #Else - SEDAN
    #goalSelectionIndex = 0t
    #environment_side_channel.set_float_parameter("goalSelectionIndex", goalSelectionIndex)
    
    environment_side_channel.set_float_parameter("goalDistance", 10)
    
    agentCarPhysics = args.p
    environment_side_channel.set_float_parameter("agentCarPhysics", agentCarPhysics)
    
    environment_side_channel.set_float_parameter("relativeSteering", 1) # default is enabled (relativeSteering = 1)

    environment_side_channel.set_float_parameter("numberOfExplorationPoints", 0)
    
    #Traffic Mode
    trafficVehicles = args.v
    environment_side_channel.set_float_parameter("numberOfTrafficVehicles", trafficVehicles)

    #File to save step time benchmarks
    BASEFILENAME = "unityGymCarStatic-"+args.a+"-seg"+str(segmentationMode)+"_"+str(episodeLength)
    print(BASEFILENAME)
    timestr = time.strftime("%Y%m%d-%H%M%S")
    baseFileNameWithTime = BASEFILENAME + "-" + timestr 
    csvFilename = baseFileNameWithTime + ".csv"
    #Visual Observation Save outs
    if(saveImageFlag and (observationModeIndex == 1 or observationModeIndex == 2)):
        imgBasePath = Path(".") / Path(baseFileNameWithTime)
        imgBasePath.mkdir(parents=True, exist_ok=True)

    #Create Gym Environment
    gym_env = UnityToGymWrapper(unity_env, False, False, True)
    #message_output = unity_env._process_immediate_message(map_side_channel.build_immediate_request("binaryMap", [327, 266, 0.5]))
    #message_output = unity_env._process_immediate_message(position_scan_side_channel.build_immediate_request("positionScan", [177.9137,35.1113,28.1348,100]))
    #obs,_,_,_ = gym_env._single_step(unity_env.get_steps(gym_env.name)[0])
    

    print('=================================')
    print(gym_env.action_space)
    print(gym_env.action_space.sample())
    print(type(gym_env.action_space), gym_env.action_space.sample(),  type(gym_env.action_space.sample()))
    print(gym_env.observation_space)
    #print(obs)
    print(gym_env.metadata)
    print(gym_env.spec)
    print(gym_env.name)
    print(gym_env._env.get_steps(gym_env.name))
    print(gym_env.action_space.high, '+++++', gym_env.action_space.low)
    print('=================================')
    
    
    observation = gym_env.reset();
    print(observation)
    
    action = [ 1, 0, -1] # throttle, steering, braking
    observation, reward, done, info = gym_env.step(action)
    print(action)
    print(observation)
    
    action = [ 0, 0, -1] # throttle, steering, braking
    observation, reward, done, info = gym_env.step(action)
    print(action)
    print(observation)
    
    action = [ 1, 0, -1] # throttle, steering, braking
    observation, reward, done, info = gym_env.step(action)
    print(action)
    print(observation)
    
    action = [ 0, 0, -1] # throttle, steering, braking
    observation, reward, done, info = gym_env.step(action)
    print(action)
    print(observation)
    

        
    gym_env.close()
    
def calculateStepTime(start_time, last_time):
    current_time = time.time()
    print("--- %s seconds ---" % (current_time - start_time))
    timerEntries.append(10000/(current_time-last_time))
    print("Steps/second = {}".format(timerEntries[-1]))
    averageStepTime = 0
    for j in timerEntries :
        averageStepTime += j
    print("Average Steps/second = {}".format(averageStepTime/len(timerEntries)))
    
def storeObservationImage(basepath, observation, grayscale, observationId):
    
    #print (observation)
    if(grayscale):
        img = Image.fromarray((observation*255).astype('uint8').reshape(observation.shape[0],observation.shape[1]), 'L')
    else:
        img = Image.fromarray((observation*255).astype('uint8'), 'RGB')
    img_path = basepath / "visual_observation{}.png".format(observationId)
    img.save(img_path)
    #img.show()

def Log2(x):
    return (math.log10(x) / 
            math.log10(2));
  
    
if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('-t', default="PointNav", choices=["PointNav", "SimpleObjectNav", "ObjectNav"], help="Navigational Task that agent will train")
    parser.add_argument('-a', default="Vector", choices=["Vector", "Visual", "VectorVisual"], help="Type of agent")
    parser.add_argument('-s', default=0, type=int, choices=[0,1,2], help="Segmentation model for visual observations")
    parser.add_argument('-e', default=2500, type=int, help="Episode length integer")
    parser.add_argument('-p', default=0, type=int, help="Agent physics mode")
    parser.add_argument('-v', default=50, type=int, help="Number of autonomous vehicles")
    args = parser.parse_args()

    main(args)
