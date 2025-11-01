# Car.Simulator
An IoT simulation project that mimics a connected car sending telemetry data (RPM, speed, fuel level, and engine temperature) to the ThingSpeak cloud platform, and analyzes the data using a separate analytics application.

# Project Structure
Car.Simulator.sln
├── Car.Shared/        # Shared model (CarModel)
├── Car.Simulator/     # Sends simulated telemetry data to ThingSpeak
└── Car.Analytics/     # Fetches and analyzes data from ThingSpeak

# Features
Simulates real-time car telemetry: RPM, Speed, Fuel Level, Engine Temperature
Sends data to ThingSpeak using HTTP Client
Collects and analyzes data from ThingSpeak API
Calculates average speed and average RPM
Handles network and user interruptions safely
Uses environment variables for API keys (no hardcoded secrets)

# Requirements
.NET 8.0 SDK or newer
ThingSpeak account with a channel containing at least 4 fields
Set the following environment variables:
- THINGSPEAK_WRITE_KEY
- THINGSPEAK_CHANNEL_ID
- THINGSPEAK_READ_KEY

# How to Run
## Run the Simulator:
  This console app sends car data to ThingSpeak every 15 seconds.
  set THINGSPEAK_WRITE_KEY=your_write_key_here
  dotnet run --project Car.Simulator/Car.Simulator.csproj
## Sample Output:
  Car Simulator is running... Press Ctrl + C to stop.
  Data sent! RPM=1430, Speed=52.4, Fuel=98.7, Temp=91.1
## Run the Analytics App:
  This app retrieves and analyzes stored data from ThingSpeak.
  set THINGSPEAK_CHANNEL_ID=your_channel_id
  set THINGSPEAK_READ_KEY=your_read_key_here
  dotnet run --project Car.Analytics/Car.Analytics.csproj
## Sample Output:
Choose data range:
1) Last 24 hours
2) Last 100 points
Select: 1
Data points analyzed: Speed=50, RPM=50
Average speed: 57.3 km/h
Average RPM: 2450

# How It Works
# Data Flow:
Car.Simulator  →  ThingSpeak Cloud  →  Car.Analytics
- Car.Simulator continuously generates and sends telemetry data to ThingSpeak every 15 seconds.
- ThingSpeak stores the data in fields (field1 = RPM, field2 = Speed, etc.).
- Car.Analytics fetches and analyzes the stored data using ThingSpeak’s REST    API.

  # Security Measures
- API keys leakage:	   Use of environment variables instead of hardcoding
- Network failure:	   Exception handling with clear console messages
- Excessive requests:	 15-second delay between API calls
- User interruption:   Graceful shutdown with Ctrl + C handling

  # Results and Testing
 - Verified successful telemetry transmission to ThingSpeak (data visible in     channel graphs)
 - Analytics app correctly fetched and processed 100+ data points
 - Console outputs confirmed stable performance and accurate calculations
  
 
  






  
  

  

  
  

