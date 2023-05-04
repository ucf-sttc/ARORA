# Introduction 
This project contains the Unity based code for ARORA simulator.

# Get the code and assets

clone the `arora` repo:

```sh
git clone --recurse-submodules git@github.com:ucf-sttc/ARORA.git
```

copy the assets from AWS:

```sh
aws s3 sync s3://arora-9km/ARORA_Assets/ARORA/ .
```

# Build 
1. Open project in Unity Editor 2020.3.33f1. We only tested it to be working in this version, use other version at your own risk. The initial load will take several hours as it is regenerating the library folder.

2. In the menu bar go to Window->Package Manager and ensure the following packages are installed:
  
   * Addressable 1.1.10
   * Universal RP 10.10.1
   
3. In the project panel go to Assets/Scenes and make sure all the numbered scenes have been marked as addressable. There will be a checkbox in the inspector of each scene and they can be selected as a group to speed up the process of checking

4. Build the Addressables: If you intend to create an executable this step must be completed before beginning the build. It is not necessary for testing in the editor. 
   1. In the menu bar go to Window -> Asset Management -> Addressables
   2. In the Addressables window select build from the menu bar. This process takes several hours and must be repeated if the scenes have been updated

5. Build the project: 

   1. In the me nu bar go to File -> Build Settings.
   2. Go to Player Settings -> Player -> Comfiguration - > Scripting Backend : Select Mono
   3. Click Build