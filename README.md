# Investment Simulator

A full-stack investment simulation application built with React (TypeScript) and ASP.NET Core, featuring real-time updates via SignalR and event-driven architecture.

##  Project Overview

This application allows users to manage a virtual investment account with real-time balance updates and automated investment processing. The system demonstrates modern full-stack development practices including event-driven architecture, real-time communication, and persistent data storage.

##  Key Features

- **User Authentication** - Simple username-based login system
- **Investment Management** - Create and track multiple investment options
- **Real-Time Updates** - Live balance and investment status updates using SignalR
- **Event-Driven Architecture** - Asynchronous request processing with message queue
- **Background Processing** - Automated investment completion tracking
- **Persistent Storage** - SQLite database with Entity Framework Core
- **Responsive UI** - Clean, modern interface with live countdown timers

##  Architecture

### Event-Driven Flow

```
User Action → Queue → Background Worker → Database → SignalR → UI Update
```

1. User initiates investment
2. Request validated and queued (immediate 202 response)
3. Background worker processes request asynchronously
4. Investment saved to database
5. Real-time notification sent via SignalR
6. UI updates automatically without page refresh

### Technology Stack

**Frontend:**
- React 18 with TypeScript
- SignalR client for real-time updates
- Axios for HTTP requests
- Lucide React for icons

**Backend:**
- ASP.NET Core 8.0
- Entity Framework Core 8.0
- SignalR for WebSocket communication
- SQLite database

##  Project Structure

```
investment-simulator/
├── InvestmentsServer/              # Backend (C# / ASP.NET Core)
│   ├── Controllers/
│   │   └── InvestmentController.cs # REST API endpoints
│   ├── Services/
│   │   ├── InvestmentService.cs    # Business logic
│   │   ├── InvestmentQueue.cs      # Message queue (Channel-based)
│   │   ├── InvestmentQueueWorker.cs        # Queue consumer
│   │   └── InvestmentBackgroundService.cs  # Completion tracker
│   ├── Hubs/
│   │   └── InvestmentHub.cs        # SignalR hub
│   ├── Models/
│   │   ├── UserAccount.cs
│   │   ├── ActiveInvestment.cs
│   │   ├── InvestmentOption.cs
│   │   └── InvestRequest.cs
│   ├── Data/
│   │   └── InvestmentDbContext.cs  # EF Core context
│   └── Program.cs
│
└── investment-client/              # Frontend (React + TypeScript)
    ├── src/
    │   ├── App.tsx                 # Main component
    │   └── main.tsx
    ├── package.json
    └── vite.config.ts
```

##  Getting Started

### Prerequisites

- .NET 8.0 SDK
- Node.js 18+ and npm
- Git

### Backend Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd investment-simulator/InvestmentsServer
   ```

2. **Restore packages**
   ```bash
   dotnet restore
   ```

3. **Run the application**
   ```bash
   dotnet run
   ```

   The server will start on `http://localhost:5243` (or check console output)

   You should see:
   ```
    Database migrations applied successfully
    Backend is UP and RUNNING!
    API URL: http://localhost:5243
    SignalR Hub: http://localhost:5243/investmentHub
    Database: SQLite (investments.db)
    Event-Driven: ENABLED
   ```

### Frontend Setup

1. **Navigate to frontend directory**
   ```bash
   cd investment-client
   ```

2. **Install dependencies**
   ```bash
   npm install
   ```

3. **Update API endpoints if needed**
   
   Edit `src/App.tsx` lines 25-26 to match your backend URL:
   ```typescript
   const API_BASE = 'http://localhost:5243/api/investment';
   const SIGNALR_HUB = 'http://localhost:5243/investmentHub';
   ```

4. **Start development server**
   ```bash
   npm run dev
   ```

   Frontend will be available at `http://localhost:5173`

### First Run

1. Open `http://localhost:5173` in your browser
2. Enter a username (minimum 3 English letters)
3. Start with $500 default balance
4. Choose an investment option and watch it complete in real-time!

## 💾 Database

### Schema

**Users Table:**
- Username (Primary Key)
- Balance (decimal)
- Related ActiveInvestments (One-to-Many)

**ActiveInvestments Table:**
- Id (Primary Key, GUID)
- Username (Foreign Key)
- Name
- Amount
- ExpectedReturn
- EndTime (UTC)

### Database Features

- **Auto-Migration**: Database created automatically on first run
- **Transaction Support**: Ensures data consistency
- **Async Operations**: Non-blocking database calls
- **Factory Pattern**: Efficient DbContext management for background services

### Database File

- Location: `InvestmentsServer/investments.db`
- Type: SQLite
- Persists across server restarts

### Switching to SQL Server (Optional)

Update `Program.cs`:
```csharp
builder.Services.AddDbContextFactory<InvestmentDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);
```

Add to `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=InvestmentSimulator;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

##  Investment Options

Three investment tiers are available:

| Name | Cost | Return | Duration |
|------|------|--------|----------|
| Short-term | $10 | $20 | 10 seconds |
| Mid-term | $100 | $250 | 30 seconds |
| Long-term | $1000 | $3000 | 60 seconds |

##  API Endpoints

### GET `/api/investment/user-data?username={username}`
Returns user account data including balance and active investments.

### GET `/api/investment/options`
Returns available investment options.

### POST `/api/investment/invest`
Queues a new investment request.

**Request Body:**
```json
{
  "username": "john",
  "optionName": "Short-term"
}
```

**Response:** `202 Accepted` (immediate response, processed asynchronously)

##  SignalR Events

The application uses SignalR for real-time communication:

### Client-to-Server
- `SubscribeToUpdates(username)` - Subscribe to user-specific updates

### Server-to-Client
- `InvestmentStarted` - Investment processing completed
- `InvestmentCompleted` - Investment duration finished, return added to balance
- `InvestmentFailed` - Investment validation or processing failed
- `SubscriptionConfirmed` - Subscription successful

##  Event-Driven Architecture Implementation

### 1. Message Queue
Uses `Channel<T>` for in-memory, thread-safe queuing:
```csharp
public async Task EnqueueAsync(InvestmentRequest request)
{
    await _channel.Writer.WriteAsync(request);
}
```

### 2. Immediate Response
Controller returns `202 Accepted` without waiting for processing:
```csharp
await _queue.EnqueueAsync(request);
return Accepted(new { message = "Request queued" });
```

### 3. Background Worker
`InvestmentQueueWorker` continuously processes queued requests:
```csharp
while (!stoppingToken.IsCancellationRequested)
{
    var request = await _queue.DequeueAsync(stoppingToken);
    await ProcessInvestmentRequestAsync(request);
}
```

### 4. Real-Time Notifications
SignalR pushes updates to connected clients:
```csharp
await hubContext.Clients.Group(username)
    .SendAsync("InvestmentStarted", data);
```

##  Business Rules

- Users start with $500 balance
- Minimum username length: 3 characters (English letters only)
- Cannot invest with insufficient balance
- Cannot have multiple active investments in the same option
- Investment returns are added automatically upon completion
- All timestamps use UTC

##  Testing the Application

### Test Event-Driven Flow
1. Open browser console (F12)
2. Click an investment option
3. Observe immediate "Processing..." message
4. Within 1 second, see "Investment started!" notification
5. Balance updates automatically without page refresh

### Test Real-Time Completion
1. Start a short-term investment (10 seconds)
2. Watch the countdown timer
3. When it reaches zero, observe automatic:
   - Completion notification
   - Balance increase
   - Investment removal from active list

### Test Connection Status
- Green WiFi icon = Connected to SignalR
- Red WiFi icon = Disconnected (will auto-reconnect)

##  Monitoring

The application includes comprehensive logging:

**Backend Logs:**
- Investment queue events
- Processing status
- Completion notifications
- SignalR connections
- Database operations

**Frontend Logs:**
- SignalR connection status
- Event reception
- API responses

##  Security Notes

- No password authentication (simplified for demonstration)
- CORS enabled for localhost development
- SignalR requires AllowCredentials for WebSocket
- Production deployment would require proper authentication/authorization

##  Known Limitations

- No persistent user authentication
- Single-server deployment (queue is in-memory)
- No investment history tracking
- Fixed investment options

##  Learning Outcomes

This project demonstrates:
- Event-driven architecture principles
- Real-time web communication with SignalR
- Asynchronous programming patterns
- Entity Framework Core and database management
- RESTful API design
- Modern React development with TypeScript
- Background service implementation
- Message queue patterns

##  Future Enhancements

- User authentication with JWT
- Investment history tracking
- Dynamic investment options
- Redis-based distributed queue
- Investment success/failure rates
- Portfolio analytics
- Multi-user real-time dashboard

##  Contributing

This is a demonstration project for educational purposes.

##  License

This project is for educational and demonstration purposes.

##  Acknowledgments

Built as part of the Siemens Student Position application process.

---

**Note**: This application is a simulation and does not involve real money or financial transactions.
