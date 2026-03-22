# 🚀 Deal Processor (MT5 / .NET)

A production-style centralized Deal Processor built in **.NET**, designed to handle high-frequency concurrent trade requests from multiple clients.

The system uses a **thread-safe queue (Channel)**, supports **retry mechanisms**, and integrates with the **MetaTrader 5 Web API** or a **mock execution mode** for testing.

---

## 📌 Features

- Centralized trade processing  
- Thread-safe producer-consumer architecture  
- Multiple concurrent client simulators  
- MT5 Web API integration (Dealer API)  
- Mock mode (no MT dependency)  
- Retry mechanism for transient failures  
- Request → MT mapping (RequestId ↔ MT Ticket)  
- Graceful shutdown (no lost requests)  
- Structured logging (Serilog)

---

## 🧠 Architecture Overview
Clients (Simulators)
↓
Thread-safe Queue (Channel)
↓
Deal Processor Service (BackgroundService)
↓
MT5 Gateway (Real / Mock)
↓
Result Store + Logging

---

## ⚙️ Configuration

All configuration is in `appsettings.json`.

### MT5 Settings

```json
"Mt5Options": {
  "UseRealMt5Api": false,
  "BaseUrl": "https://your-mt5-api-url",
  "ManagerLogin": 14,
  "Password": "your-password",
  "CentralTradeLogin": 1010,
  "PollIntervalMs": 500,
  "PollTimeoutMs": 10000,
  "ConnectRetryCount": 5,
  "ConnectRetryDelayMs": 2000
}

### Simulation Settings
"SimulationOptions": {
  "ClientCount": 5,
  "RequestsPerClient": 1000,
  "ShutdownDelayMs": 10000
}

### Retry Settings
"RetryOptions": {
  "Enabled": true,
  "MaxRetryAttempts": 2,
  "DelayMs": 500
}

### Run 
dotnet run --project DealProcessor.Console


🔄 Execution Flow
Clients generate trade requests
Requests are enqueued into a thread-safe queue
Deal Processor dequeues requests
Requests are validated
Requests are executed:
Mock mode → simulated execution
Real mode → MT5 Web API
Results are stored and logged
Retry logic is applied if needed
Application waits until queue is drained before shutdown
🔌 MT5 API Integration

The following MT5 Web API endpoints are used:

/api/auth/start → initiate authentication
/api/auth/answer → complete authentication
/api/dealer/send_request → send trade request
/api/dealer/get_request_result → retrieve execution result
🧵 Threading Model
Multiple producers (client simulators)
Single or configurable consumers (processor workers)
Thread-safe queue using System.Threading.Channels
Atomic operations using Interlocked
Optional execution serialization for MT API
🔁 Retry Mechanism
Retries only transient failures (e.g. timeout, connection issues)
Max retry count configurable
Same RequestId is preserved across retries
Prevents infinite retry loops
🧪 Testing Modes
Mock Mode (Recommended)
"UseRealMt5Api": false
No MT dependency
Simulates:
Success
Market closed
No money
Real MT5 Mode
"UseRealMt5Api": true

Requires:

MT5 Web API access
Valid credentials
📊 Logging
Console output
File logs:
/logs/deal-processor-YYYYMMDD.log
🎯 Key Guarantees
Thread-safe request handling
No race conditions in queue
No lost requests (graceful shutdown)
Full request lifecycle tracking
💡 Future Improvements
Multi-worker parallel processing
Persistent storage (DB instead of in-memory)
Metrics & monitoring (Prometheus / OpenTelemetry)
API layer for external clients
👤 Author

Hamzeh Taha

⭐ If you like this project, give it a star!

---

## ✅ This version is:

- ✔ GitHub clean Markdown  
- ✔ No weird formatting  
- ✔ Professional  
- ✔ Matches evaluation criteria  
- ✔ Ready to paste  

---

If you want next level (this really impresses them):

👉 I can add badges (build status, .NET version, etc.)  
👉 or add a diagram image for GitHub  
👉 or make a “System Design” section (very strong for interviews)
