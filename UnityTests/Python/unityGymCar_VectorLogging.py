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

timerEntries = []

def main(args : argparse.Namespace):

    print(args)
    #Debug
    saveImageFlag = False
    saveOnStepIfFactorOf = 100
    
    #Reject Visual agent
    if(args.a=="Visual"):
        print('Not compatible with agents not including vector observations')
        return

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
    unity_env = UnityEnvironment(file_name = unityEnvironmentStr, seed = 1, timeout_wait=1000, side_channels =[engine_side_channel, environment_side_channel, position_scan_side_channel, map_side_channel, float_properties_side_channel])
    
    print('connected to ',unityEnvironmentStr)
    
    #Engine Side Channels
    engine_side_channel.set_configuration_parameters(time_scale=10, quality_level = 0)

    #Rewards
    environment_side_channel.set_float_parameter("rewardForGoalCollision", GOAL_COLLISION_REWARD)
    environment_side_channel.set_float_parameter("rewardForExplorationPointCollision", EXP_COLLISION_REWARD)
    environment_side_channel.set_float_parameter("rewardForOtherCollision", OTHER_COLLISION_REWARD)
    environment_side_channel.set_float_parameter("rewardForFallingOffMap", FALL_OFF_MAP_REWARD)
    environment_side_channel.set_float_parameter("rewardForEachStep", STEP_REWARD)

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

    #Select Vector Visual Agent 2
    if(args.a=="VectorVisual"):
        observationModeIndex = 2
    environment_side_channel.set_float_parameter("observationMode", observationModeIndex)
    
    #Agent Car Physics
    agentCarPhysics = 1
    if(args.nophysics):
        agentCarPhysics = 0
    environment_side_channel.set_float_parameter("agentCarPhysics", agentCarPhysics)
    
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
    #goalSelectionIndex = 0
    #environment_side_channel.set_float_parameter("goalSelectionIndex", goalSelectionIndex)

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
    observation = gym_env.reset()
    
    print('=================================')
    print(gym_env.action_space)
    print(gym_env.action_space.sample())
    print(type(gym_env.action_space), gym_env.action_space.sample(),  type(gym_env.action_space.sample()))
    print(gym_env.observation_space)
    print(observation)
    print(gym_env.metadata)
    print(gym_env.spec)
    print(gym_env.name)
    print(gym_env._env.get_steps(gym_env.name))
    print(gym_env.action_space.high, '+++++', gym_env.action_space.low)
    print('=================================')

 
    start_time = time.time()
    last_time = start_time
    

    #stepTimeDeltas format: step#, steptimedelta
    stepTimeDeltas = []
    observations = []
    for t in range(30001):
        #action = gym_env.action_space.sample()
        # some arbitrary turns and then reverse
        if (t >= 0):
            action = [ 1, 1, 0]
        if (t > 17000):
            action = [ 1, 0, 0]
        if (t > 25000):
            action = [ 0, 1, 0]
        observation, reward, done, info = gym_env.step(action)

        current_time = time.time()
        ## log observation
        k = 0
        if(observationModeIndex == 2):
            k = 3
        textline = []
        textline.append("{}".format(t))
        for i in range(13):
            textline.append(",{}".format(observation[k][i]))
        observations.append(''.join(textline))
        ##
        stepTimeDeltas.append("{},{}".format(t, current_time-last_time))
        
        last_time = current_time
        if(saveImageFlag and t%saveOnStepIfFactorOf == 0 and (observationModeIndex == 1 or observationModeIndex == 2)):
            #calculateStepTime(start_time, last_time)
            #print (observation)
            storeObservationImage(imgBasePath, observation[0], False, t+0)
            storeObservationImage(imgBasePath, observation[1], True, t+1)
            storeObservationImage(imgBasePath, observation[2], False, t+2)
            #last_time = time.time()
        if done:
            print("\tEpisode finished after {} timesteps".format(t+1))
            gym_env.reset()
                
    with open(csvFilename, "w") as logfile:
        for s in observations:
            logfile.write(s+"\n") 
            
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
    
if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('-t', default="PointNav", choices=["PointNav", "SimpleObjectNav", "ObjectNav"], help="Navigational Task that agent will train")
    parser.add_argument('-a', default="Vector", choices=["Vector", "Visual", "VectorVisual"], help="Type of agent")
    parser.add_argument('-s', default=0, type=int, choices=[0,1,2], help="Segmentation model for visual observations")
    parser.add_argument('-e', default=2500, type=int, help="Episode length integer")
    parser.add_argument('--nophysics', action="store_true", help="Disable agent car physics simulation")
    args = parser.parse_args()

    main(args)