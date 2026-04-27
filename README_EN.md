# How to Compile and Reproduce This Project from the Existing Project
1. Set up Unity Editor `2022.3.62f3c1 LTS` and Visual Studio 2022
    - Visual Studio 2022 shall be downloaded from the Installs page in Unity Hub.
2. Open the project located in `./Hololens2Test` via Unity Hub.
3. Open the scene:
    - File -> Open Scene
    - Select `Assets/Scenes/Hololen2Scene.unity`
4. Reconfigure options in File -> Build Settings
    - UWP
    - ARM64
    - Remote Device (via Device Portal)
       - Enter the correct Address, Username and Password
       - It is recommended to test the Device Portal address in a browser to verify connectivity
       - Navigate to System -> Preferences -> Device Security
           - Either disable the SSL connection requirement option
           - Or download and install the certificate file named `rootcertificate.cer`
    - Click Switch Platform after configuration is completed.
5. Go to Edit -> Project Settings -> Project Validation and select Fix All
    - Some minor non-critical errors may remain unfixed, which can be ignored.

For subsequent operations, refer to the section *Deploy the Project to HoloLens 2*.

## Deploy the Project to HoloLens 2

> [!TIP]
> Make good use of Device Portal. Its web version provides abundant auxiliary functions, such as transferring files stored on the device to other computers.

> [!TIP]
> If Windows Developer Mode is not enabled on your PC, please enable it:
> - Path: `Win+I` > System > Advanced > Developer Mode

1. Ensure your HoloLens 2 and development computer are connected to the same local area network.
2. Make sure your HoloLens 2 is powered on and Device Portal is properly configured.
3. Pre-install *OpenXR Tools for Windows Mixed Reality* on HoloLens 2.
4. In Build Settings
    - Set Build and Run on to `Remote Device (via Device Portal)`
    - Then fill in the correct Address, Username and Password for Device Portal
    - The compilation and deployment process may take a long time; keep the headset powered on during the process.
5. Fix missing Visual Studio components
    - Open the .sln file generated in the build folder with Visual Studio
    - Visual Studio will prompt missing runtime components after opening the file.
6. Perform Build and Run. Refer to the following solutions if any errors occur.
    - To cooperate with the automated scripts provided in this project
    - You need to save the built project to the directory `./Hololens2Test/_build`
    - Ensure HoloLens 2 is unlocked before building; otherwise Unity will fail to connect to HoloLens 2 via Device Portal
        - If you start compilation first and unlock the headset afterwards, connection failure will most likely occur.
    - Without compilation cache, the initial compilation and deployment takes about 4 minutes.
    - With cache available, compilation and deployment take about 1.5 minutes.

## Common Solutions to Compilation Errors

> [!NOTE]
> Other possible errors and corresponding solutions are listed below. Note: Rebuild the project after fixing any error.
> - No valid MRTK Profile for build target platform.
>   - Go to Edit -> Project Settings -> Project Validation and run Fix All.
> - Failed to open DevicePortal connection to 'xxx.xxx.xxx.xxx'
>   - First check whether the Device Portal web page is accessible via a browser. If it is accessible
>   - The Device Portal credentials in Build Settings are most likely lost; just refill them.
> - Selected Visual Studio is missing required components and may not be able to build the generated project.
>   - Open the `.sln` file of the project under `./Hololens2Test/_build` in Visual Studio to diagnose missing components
>   - Install the required components as prompted.
> - Build Failed followed by a long compilation command, with error MSB3774: Cannot find SDK "WindowsMobile, Version=10.0.2xxxx.0" in the error message
>   - Method 1: Use the automated script provided by this project
>       - Run the Python script `./fix_win_mobile.py`
>   - Method 2: Manual fix (if the error persists after running the script)
>       - Open the `.sln` file with Visual Studio 2022
>       - Unload the main solution `Hololens2Test` in Solution Explorer
>       - Locate and delete the `ItemGroup` node that contains `WindowsMobile` in the project code, then reload the solution.
> - Deployment Error with a filename similar to ...x64.appx shown in the error message
>   - Select ARM64 as the target architecture in Build Settings.