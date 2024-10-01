---

# Particle Accelerator Monitoring System

## Overview
This project simulates a particle accelerator monitoring system where various sensors (e.g., temperature, humidity, pressure, radiation) are deployed to monitor critical parameters. Each sensor operates in different states, such as `ON`, `OFF`, `IDLE`, `MEASURING`, etc., and transmits its data at regular intervals.

The system includes an **archiving service** that logs data to a specific directory (`Documents\ParticleAcceleratorMonitoring`) and a **logging service** that tracks events and errors throughout the application's lifecycle.

## Features
- Real-time monitoring of sensor data.
- Archiving service to save sensor data in the user's `Documents` folder.
- Logging service to track application events and errors in the user's `Documents` folder.
- Multi-threaded sensor simulation with configurable update intervals.
- Supports multiple sensor types (e.g., Temperature, Humidity, Pressure, Radiation).

## Getting Started

### Prerequisites
- **.NET SDK 8.0** or later must be installed on your machine. You can download the latest .NET SDK from [here](https://dotnet.microsoft.com/download/dotnet/8.0).
- Visual Studio (2022 or later) or any IDE that supports .NET development (e.g., Visual Studio Code, JetBrains Rider).

### Installation

1. Clone the repository to your local machine:

    ```bash
    git clone https://github.com/zanostro/ParticleAcceleratorMonitoring.git
    ```

2. Navigate into the project folder:

    ```bash
    cd ParticleAcceleratorMonitoring
    ```

3. Install the required NuGet packages by restoring the dependencies:

    ```bash
    dotnet restore
    ```

4. Open the solution in your preferred IDE (e.g., Visual Studio, JetBrains Rider).

5. Build the solution to ensure all dependencies are properly installed.

### Running the Application

1. **Start the application**:
   - Navigate to `Program.cs` in the root of the project.
   - Run the project using your IDE or via the .NET CLI:
     ```bash
     dotnet run
     ```
   - The application will initialize the sensor system, and you'll see logs indicating sensor states and data transmissions in the console.


### Runtime

1. **Monitoring Service**:
   - When the application starts, the **Monitoring Service** is initialized. This service controls the overall operation of all other components, ensuring that sensor readings are within specified safety intervals.
   - The **Monitoring Service** includes a console interface that displays important information about the system's current state.
   - Additionally, a **Ping** button is available to manually check the connection status of sensors in real time.

2. **Client Display**:
   - The application launches two client interfaces. These clients receive and display real-time sensor readings such as temperature, pressure, and humidity.
   - Clients also receive warning signals from the **Monitoring Service** and will visually display alerts when sensor readings exceed defined thresholds, helping operators monitor the system's safety.

2. **Archiving and Logging**:
   - **Archiving Service**: All sensor data will be archived in the user's `Documents\ParticleAcceleratorMonitoring` folder. This folder is created automatically if it doesn't exist.
   - **Logger**: The application uses a logging system that outputs logs to both the console and a file in the same `Documents\ParticleAcceleratorMonitoring` folder.

### Key Components

- **Program.cs**: Entry point of the application. It initializes the sensors and starts the archiving and logging services.
- **MonitoringService.cs**: Oversees the operation of all sensors and clients. It processes sensor data, identifies abnormal readings, and triggers alerts.
- **Client.cs**: Displays sensor data and emergency states. A yellow indicator is shown when a sensor's readings fall outside the allowed range. A red indicator signals an emergency when two or more sensors exceed the allowed thresholds.
- **Sensor.cs**: The base class for all sensors. It defines the sensor's behavior, including reading sensor values, broadcasting data, and managing states.
- **Archiving Service**: Responsible for saving sensor data to the user's `Documents\ParticleAcceleratorMonitoring` folder. Data is saved in plain text format.

---

### Summary

- To start the application, run the `Program.cs` file.
- Sensor data is archived in the `Documents\ParticleAcceleratorMonitoring` folder.
- The logger outputs important application events to the console and a log file in the same folder.

---
