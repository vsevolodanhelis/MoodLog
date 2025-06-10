# MoodLog - Quick Start Guide ⚡

## 🚀 Get Running in 5 Minutes

### Prerequisites Check
```bash
# Verify you have .NET 8.0
dotnet --version
# Should show: 8.0.x

# Verify Git
git --version
```

### Installation Commands
```bash
# 1. Clone the repository
git clone https://github.com/vsevolodanhelis/MoodLog.git
cd MoodLog

# 2. Restore packages
dotnet restore

# 3. Build solution
dotnet build

# 4. Run the application
cd src/MoodLog.Web
dotnet run
```

### Access Points
- **Application**: http://localhost:5115
- **API Docs**: http://localhost:5115/swagger
- **Sample Data**: http://localhost:5115/Seed

### First Steps
1. **Register Account**: Click "Register" on homepage
2. **Add Sample Data**: Visit `/Seed` and click "Seed Sample Data"
3. **Start Tracking**: Use the emoji mood picker on dashboard
4. **View Analytics**: Check the Analytics page for insights

### Quick Commands
```bash
# Run tests
dotnet test

# Reset database
dotnet ef database drop --force
dotnet ef database update

# Clean build
dotnet clean && dotnet build

# Stop application
Ctrl+C (in terminal)
```

### Project Structure
```
src/MoodLog.Web/        # Main web application (START HERE)
src/MoodLog.Core/       # Business logic
src/MoodLog.Application/ # Services
tests/                  # Unit tests
```

### Admin Access
- **Email**: `admin@moodlog.com`
- **Password**: Any valid password
- **Auto Role**: Admin role assigned automatically

### Troubleshooting
```bash
# Port 5115 busy?
netstat -ano | findstr :5115
taskkill /PID <process_id> /F

# Package issues?
dotnet nuget locals all --clear
dotnet restore
```

### Key Features to Test
- ✅ Mood tracking with emojis
- ✅ Timeline view of mood history
- ✅ Analytics dashboard with charts
- ✅ Settings and data export
- ✅ Admin panel (with admin account)
- ✅ REST API endpoints

### Development Workflow
```bash
# Create feature branch
git checkout -b feature/my-feature

# Make changes, then test
dotnet test
dotnet run --project src/MoodLog.Web

# Commit and push
git add .
git commit -m "Add feature description"
git push origin feature/my-feature
```

---

**Need help?** Check `SETUP_GUIDE.md` for detailed instructions!

**Repository**: https://github.com/vsevolodanhelis/MoodLog
