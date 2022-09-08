import warnings
warnings.filterwarnings('ignore')
warnings.simplefilter('ignore')

import sys
import math
import argparse
import random
from collections import deque
import datetime
import time
from pathlib import Path

import numpy as np

from keras.models import Sequential
from keras.layers import Dense, Conv2D, Flatten, Dropout
from keras.optimizers import Adam

from mlagents_envs.environment import UnityEnvironment
from gym_unity.envs import UnityToGymWrapper
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel
from mlagents_envs.side_channel.environment_parameters_channel import EnvironmentParametersChannel


throttle=15.0
steering=10.0

class SimpleAgent(object):
    def __init__(self, action_space, lr=0.01, batch_size=64, discount_rate=1.0, exploration_rate=1.0, exploration_decay=0.995, exploration_min=0.1):
        self.action_space=action_space
        self.memory=deque(maxlen=10000)
        self.lr=lr
        self.batch_size=batch_size
        self.discount_rate=discount_rate
        self.exploration_rate=exploration_rate
        self.exploration_decay=exploration_decay
        self.exploration_min=exploration_min

        self.model = Sequential()
        self.model.add(Dense(24, input_dim=10, activation='tanh'))
        self.model.add(Dropout(0.2))
        self.model.add(Dense(48 , activation='tanh'))
        self.model.add(Dropout(0.2))
        self.model.add(Dense(48 , activation='tanh'))
        self.model.add(Dropout(0.2))
        self.model.add(Dense(4, activation='linear'))
        self.model.compile(loss='mse', optimizer=Adam(lr=self.lr))
        
        print(self.model.summary())

    def predict (self, observation):
        return self.model.predict(observation)
        
    def act(self, observation, ex_rate):
        return np.random.randint(0,4) if np.random.rand() <= ex_rate else  np.argmax(self.model.predict(observation)[0])

    def calc_exploration_rate(self, t, t_total, t_burnin=5):
        #return max(self.exploration_min, min(self.exploration_rate, 1.0 - math.log10((t + 1) * self.exploration_decay)))
        return max(self.exploration_min, min(self.exploration_rate, 1.0 - (t-t_burnin)/(t_total-t_burnin)))

    def memorize(self, observation, action, reward, next_observation, done):
        self.memory.append((observation, action, reward, next_observation, done))

    def replay(self, batch_size):
        x_batch, y_batch = [], []
        #Old minibatch
        #minibatch = random.sample(self.memory, min(len(self.memory), batch_size))
        minibatch = self.memory
    
        for observation, action, reward, next_observation, done in minibatch:
            #print("*****", observation, action, reward, next_observation, done, "*****", sep="^^^")
            y_target = self.model.predict(np.reshape(observation, [1,*observation.shape]))
            print(reward, done, action, y_target, y_target[0][action], np.max(y_target[0]))
            y_target[0][action] = reward if done else reward + self.discount_rate * np.max(self.model.predict(np.reshape(next_observation, [1,*next_observation.shape]))[0])
            #print(observation, y_target)
            x_batch.append(observation)
            y_batch.append(y_target[0])

        x_b = np.array(x_batch)
        y_b = np.array(y_batch)
        print(x_b.shape, y_b.shape)
        
        try:
            #Old minibatch
            #self.model.fit(np.array(x_batch), np.array(y_batch), batch_size=len(x_batch), verbose=1)
            self.model.fit(np.array(x_batch), np.array(y_batch), batch_size=batch_size)
        except:
        
            for x in x_b:
                print(x,"====", x.shape)
                
            for y in y_b:
                print(y,"----", y.shape)
            print("Unexpected error:", sys.exc_info()[0])
            raise
            
        if self.exploration_rate > self.exploration_min:
            self.exploration_rate *= self.exploration_decay 

    def save(self, filename):
        self.model.save(filename)

def main(args : argparse.Namespace):

    #Debug
    saveImageFlag = True
    saveOnStepIfFactorOf = 5

    #Rewards
    GOAL_COLLISION_REWARD = 1
    EXP_COLLISION_REWARD = .8
    OTHER_COLLISION_REWARD = -0.9
    FALL_OFF_MAP_REWARD = -0.1
    STEP_REWARD = -0.08
    
    #print(args, type(args))

    #env = gym.make(args.env_id)
    #env.seed(0)
    engine_side_channel = EngineConfigurationChannel()
    environment_side_channel = EnvironmentParametersChannel()

    #Connect to Unity Editor environment
    unityEnvironmentStr = None
    #Connect to specified binary environment
    #unityEnvironmentStr = "../envs/Berlin/Berlin_ML.exe" 
    unityEnvironmentStr = "E:/work/AICOOP/AI_COOPV2-regressiontesting/UnityTests/Berlin_ML_URP/Build/Berlin_Walk_v2.exe" 
    unity_env = UnityEnvironment(file_name = unityEnvironmentStr, seed = np.random.randint(0,100000), timeout_wait=600, side_channels =[engine_side_channel, environment_side_channel])

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
    segmentationMode = 2
    environment_side_channel.set_float_parameter("segmentationMode", segmentationMode)

    #Agent Selection
    #Select Vector Agent 0
    #Select Visual Agent 1
    #Select Vector Visual Agent 2
    observationModeIndex = 2
    environment_side_channel.set_float_parameter("observationMode", observationModeIndex)
    
    #Episode Length
    episodeLength = 1600
    environment_side_channel.set_float_parameter("episodeLength", episodeLength)

    #Point Nav Task 0
    #Simple Object Nav Task 1
    #Simple Vehicle Object Nav Task with goal selection 2
    selectedTaskIndex = 1
    environment_side_channel.set_float_parameter("selectedTaskIndex", selectedTaskIndex)
    #0 - Tocus;
    #1 - sedan1
    #2 - Car1
    #3 - Car2
    #4 - City Bus
    #5 - Sporty_Hatchback
    #Else - SEDAN
    #goalSelectionIndex = 0
    #environment_side_channel.set_float_parameter("goalSelectionIndex", goalSelectionIndex)

    gym_env = UnityToGymWrapper(unity_env, False, False, True)
    
    print("DQN_VECTOR_VISUAL_AGENT")
    print("*****", gym_env.__dict__, "*****")
    print(type(gym_env.__dict__['name']))
    print(gym_env.__dict__['_env'], gym_env.__dict__['name'], "Action Space:", gym_env.action_space, "Observation Space:", gym_env.observation_space, "Reward Range:", gym_env.reward_range)
    print("Observation Space Ranges")
    print("Observations", len(gym_env.observation_space), type(gym_env.observation_space))
    for ob in gym_env.observation_space:
        print(ob.shape)
        #print(ob.high, '+++++', ob.low)
        #print(type(ob.high), '+++++', type(ob.low))
        print(ob.high.shape, '+++++', ob.low.shape)
        print(type(ob))
        print('========')
    print("Action Space Sample", gym_env.action_space.sample())

    agent = SimpleAgent(gym_env.action_space, lr=0.08, exploration_rate=1.0, exploration_decay=0.96)


    episode_count = 1000000
    scores = deque(maxlen=10)

    start_time = time.time()
    last_time = start_time
    cur_step = 0
    last_episode_end_step=0
    step_average_interval=250
    
    #Agent's action state
    forward_state = False
    backward_state = False
    left_state = False
    right_state = False
    
    #Agent action state number of steps
    forward_state_count_max = 20
    backward_state_count_max = 15
    left_state_count_max = 15
    right_state_count_max = 15
    
    forward_state_count = forward_state_count_max
    backward_state_count = backward_state_count_max
    left_state_count = left_state_count_max
    right_state_count = right_state_count_max
    
    BASEFILENAME = "DQN_vector_visual_car_agent-walk"
    timestr = time.strftime("%Y%m%d-%H%M%S")
    baseFileNameWithTime = BASEFILENAME + "-" + timestr 
    csvFilename = baseFileNameWithTime + ".csv"

    #stepTimeDeltas format: step#, steptimedelta, episode_count, agent_act_time, agent_memorize_time
    stepTimeDeltas = []
    valueable_episode_count = 0
    goal_count = 0
    exp_count = 0
    for i in range(episode_count):
        done = False
        ob = gym_env.reset()[3]
        score = 0
        valueable_episode_cur_count = 0
        goal_cur_count = 0
        exp_cur_count = 0
        
        
        for dummmy_i in range(0,10):
            print("============================================================")
        print("\tCurrent progress in training has reached {} valueable episodes, {} goals and {} exploration points".format(valueable_episode_count, goal_count, exp_count)) 
            
        #Until the Episode receives a done signal
        while not done:
            #print("Observation Shape Prior", ob.shape)
            #action = [rotated left, rotate right, forward, backward]
            reshaped_ob = np.reshape(ob, [1,*ob.shape])
            #print("Observation Shape Prior Reshape", reshaped_ob.shape)
            
            true_action = []
            
            calc_exp_rate = agent.calc_exploration_rate(valueable_episode_count, 20)
            #If our agent action state is not set then get an action that has the highest expected value
            if not forward_state and not backward_state and not left_state and not right_state:
                a = datetime.datetime.now()
                action = agent.act(reshaped_ob, calc_exp_rate)
                b = datetime.datetime.now()
                agent_act_time = b-a
                
                #action=0
                #print(action, end =" ")
                #Forward
                if action==0:
                    forward_state = True
                    forward_state_count = forward_state_count_max
                #Left
                elif action==1:
                    left_state = True
                    left_state_count = left_state_count_max
                #Right
                elif action==2:
                    right_state = True
                    right_state_count = right_state_count_max
                #Backward
                elif action==3:
                    backward_state = True
                    backward_state_count = backward_state_count_max
                else:
                    forward_state = True
                    forward_state_count = forward_state_count_max
                    
            #If an agent action state is active then get an action, decrement one step and reset state if state has finished
            action_class = ""
            if forward_state:
                true_action = [throttle, 0]
                forward_state_count = forward_state_count - 1
                action_class = "Forward"
                if forward_state_count == 0:
                    forward_state = False
            elif left_state:
                true_action = [throttle, -steering]
                left_state_count = left_state_count - 1
                action_class = "Left"
                if left_state_count == 0:
                    left_state = False
            elif right_state:
                true_action = [throttle, steering]
                right_state_count = right_state_count - 1
                action_class = "Right"
                if right_state_count == 0:
                    right_state = False
            elif backward_state:
                true_action = [-throttle, 0]
                backward_state_count = backward_state_count - 1
                action_class = "Backward"
                if backward_state_count == 0:
                    backward_state = False
            else:
                true_action = [throttle, 0]
                forward_state_count = forward_state_count - 1
                action_class = "Forward"
                if forward_state_count == 0:
                    forward_state = False
                
            #Send action to environment
            #print("Episode:", i, "Observation", ob, "Agent Action", action)
            next_ob, reward, done, _ = gym_env.step(true_action)
            next_ob = next_ob[3]
            #next_ob = np.reshape(next_ob, [1,*next_ob.shape])

            #Only add current step to DQN memory buffer if this was a somewhat valueable step
            reward_class = ""
            if reward == np.float32(GOAL_COLLISION_REWARD) or reward == np.float32(GOAL_COLLISION_REWARD)+np.float32(STEP_REWARD):
                reward_class = "Goal"
                goal_cur_count = goal_cur_count + 1
            elif reward == np.float32(EXP_COLLISION_REWARD)or reward == np.float32(EXP_COLLISION_REWARD)+np.float32(STEP_REWARD):
                reward_class = "Exploration"
                exp_cur_count = exp_cur_count + 1
            elif reward == np.float32(OTHER_COLLISION_REWARD)or reward == np.float32(OTHER_COLLISION_REWARD)+np.float32(STEP_REWARD):
                reward_class = "Other"
            elif reward == np.float32(FALL_OFF_MAP_REWARD) or reward == np.float32(FALL_OFF_MAP_REWARD)+np.float32(STEP_REWARD):
                reward_class = "Fall"
            elif reward == np.float32(STEP_REWARD):
                reward_class = "Step"
            else:
                reward_class = "Unknown"
            
            a = datetime.datetime.now()
            #print("Memorize ob, next_ob shape", ob.shape, next_ob.shape)
            if(reward_class == "Goal" or reward_class == "Exploration" or reward_class == "Unknown"):
                agent.memorize(ob, action, reward, next_ob, done)
                print("Memorized Valuable")
                if(reward_class == "Goal"):
                    valueable_episode_cur_count = valueable_episode_cur_count + 5
                else:
                    valueable_episode_cur_count = valueable_episode_cur_count + 1
            elif(reward_class == "Other"):
                if np.random.rand() < 0.7: 
                    agent.memorize(ob, action, reward, next_ob, done) 
                    print("Memorized Other")
            elif(reward_class == "Fall" or reward_class == "Step"):
                if np.random.rand() < 0.02: 
                    agent.memorize(ob, action, reward, next_ob, done) 
                    print("Memorized Step")
            b = datetime.datetime.now()
            agent_memorize_time = b-a
            
            ob = next_ob 
            score += reward
            
            current_time = time.time()
            stepTimeDeltas.append("{},{},{},{},{},{}".format(cur_step, current_time-last_time, i, agent_act_time, agent_memorize_time, ""))
            last_time = time.time()
            
            cur_step += 1
            
            print(action_class, reward, reward_class, agent.exploration_rate, calc_exp_rate)
        if valueable_episode_cur_count > 5: valueable_episode_count = valueable_episode_count + 1
        #print()
        scores.append(score)
        mean_score = np.mean(scores)
        
        print()
        print("\tReplaying Memory")
        a = datetime.datetime.now()
        agent.replay(agent.batch_size)
        b = datetime.datetime.now()
        agent_replay_time = b-a
        
        current_time = time.time()
        stepTimeDeltas.append("{},{},{},{},{},{}".format("", current_time-last_time, i, "", "", agent_replay_time))
        last_time = time.time()
        for dummmy_i in range(0,10):
            print("============================================================")
        print("\tEpisode {} finished at timestep {}".format(i, cur_step+1))
        print("\tAnd took {} timesteps ".format(cur_step-last_episode_end_step))
        print("\tThis Episode reached {} goals and {} exploration points".format(goal_cur_count, exp_cur_count))
        goal_count = goal_count + goal_cur_count
        exp_count = exp_count + exp_cur_count
        last_episode_end_step=cur_step
        
        if i % 10 == 0:
            print('[Episode {}] - Mean survival score over last 10 episodes was {}'.format(i, mean_score))
            agent.save("Model"+str(i))

    with open(csvFilename, "w") as logfile:
        for s in stepTimeDeltas:
            logfile.write(s+"\n")
            
    gym_env.close()

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('null', nargs='?', default='null', help='Nothing for now')
    args = parser.parse_args()
    main(args)
