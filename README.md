# Investment Simulator

A full-stack investment simulation application,
allows users to manage a virtual investment account with real-time balance updates and automated investment processing.

### Backend Setup

1. **Restore packages**
   ```bash
   dotnet restore
   ```

2. **Run the application**
   ```bash
   dotnet run
   ```

   The server will start on `http://localhost:5243` (or check console output)

### Frontend Setup

1. **Install dependencies**
   ```bash
   npm install
   ```

2. **Update API endpoints if needed**
   
   Edit `src/App.tsx` lines 25-26 to match your backend URL:
   ```typescript
   const API_BASE = 'http://localhost:5243/api/investment';
   const SIGNALR_HUB = 'http://localhost:5243/investmentHub';
   ```

3. **Start development server**
   ```bash
   npm run dev
   ```

   Frontend will be available at `http://localhost:5173`

### First Run

1. Open `http://localhost:5173` in your browser
2. Enter a username (minimum 3 English letters)
3. Choose an investment option and watch it complete in real-time!

---

