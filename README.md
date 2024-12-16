# Project Overview

This project has **2 versions** depending on the OS you're running on:

[Video Demo](https://drive.google.com/file/d/1yrKz0yM1sVpZqf6GD53oTqsULgw4tCS8/view?usp=drivesdk)

## Desktop Version

- **Description:**  
  A 2D application where you can use a slider to change the slice number of a scan. After adjusting the slice number, you can input the headset's IP address to send both the slices and patient information to the headset.

- **How to Build:**  
  1. Change the target platform to **MacOS** in Unity.
  2. Build the project as you normally would for a desktop application.
  
  **Alternatively**, you can run it directly in the Unity Editor without changing the build target.

## MR Version

- **Description:**  
  A 3D viewer that works in conjunction with the desktop application. After receiving the data sent from the desktop version, you can view and manipulate the scans in a 3D space. This version also includes a hand menu displaying patient information.

- **How to Build:**  
  1. Change the target platform to **Android** in Unity.
  2. Deploy the build to the **Meta Quest** headset.
